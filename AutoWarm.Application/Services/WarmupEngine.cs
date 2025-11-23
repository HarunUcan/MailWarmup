using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoWarm.Application.Interfaces;
using AutoWarm.Domain.Entities;
using AutoWarm.Domain.Enums;

namespace AutoWarm.Application.Services;

public class WarmupEngine : IWarmupEngine
{
    private readonly IWarmupProfileRepository _profiles;
    private readonly IWarmupJobRepository _jobs;
    private readonly IMailAccountRepository _accounts;
    private readonly IMailProviderFactory _providerFactory;
    private readonly IWarmupEmailLogRepository _logs;
    private readonly IWarmupStrategy _strategy;
    private readonly IUnitOfWork _unitOfWork;

    public WarmupEngine(
        IWarmupProfileRepository profiles,
        IWarmupJobRepository jobs,
        IMailAccountRepository accounts,
        IMailProviderFactory providerFactory,
        IWarmupEmailLogRepository logs,
        IWarmupStrategy strategy,
        IUnitOfWork unitOfWork)
    {
        _profiles = profiles;
        _jobs = jobs;
        _accounts = accounts;
        _providerFactory = providerFactory;
        _logs = logs;
        _strategy = strategy;
        _unitOfWork = unitOfWork;
    }

    public async Task GenerateDailyJobsAsync(CancellationToken cancellationToken = default)
    {
        var profiles = await _profiles.GetActiveProfilesAsync(cancellationToken);
        var todayLocal = DateTime.Now.Date;
        foreach (var profile in profiles)
        {
            // Avoid regenerating jobs for the same day (e.g., on application restarts).
            var utcDayStart = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(todayLocal, DateTimeKind.Local));
            var utcDayEnd = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(todayLocal.AddDays(1).AddTicks(-1), DateTimeKind.Local));
            var alreadyScheduled = await _jobs.HasJobsInRangeAsync(profile.MailAccountId, utcDayStart, utcDayEnd, cancellationToken);
            if (alreadyScheduled)
            {
                continue;
            }

            var jobs = _strategy.GenerateDailyJobs(profile, todayLocal);
            if (jobs.Count > 0)
            {
                await _jobs.AddRangeAsync(jobs, cancellationToken);
                profile.CurrentDay += 1;
                await _profiles.UpdateAsync(profile, cancellationToken);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task ExecutePendingJobsAsync(CancellationToken cancellationToken = default)
    {
        var pendingJobs = await _jobs.GetPendingJobsAsync(DateTime.UtcNow, cancellationToken);
        foreach (var job in pendingJobs)
        {
            var account = await _accounts.GetByIdAsync(job.MailAccountId, cancellationToken);
            if (account is null)
            {
                job.Status = WarmupJobStatus.Failed;
                job.ErrorMessage = "Mail account not found.";
                job.ExecutedAt = DateTime.UtcNow;
                await _jobs.UpdateAsync(job, cancellationToken);
                continue;
            }

            var provider = _providerFactory.Resolve(account);
            try
            {
                string messageId = job.Id.ToString();
                switch (job.Type)
                {
                    case WarmupJobType.SendEmail:
                        messageId = await provider.SendEmailAsync(account, account.EmailAddress, "Warmup ping", "Hello from AutoWarm warmup.", cancellationToken);
                        break;
                    case WarmupJobType.ReplyEmail:
                        messageId = await provider.SendEmailAsync(account, account.EmailAddress, "Warmup reply", "Replying to warmup thread.", cancellationToken);
                        break;
                    case WarmupJobType.MarkImportant:
                        await provider.MarkAsImportantAsync(account, job.Id.ToString(), cancellationToken);
                        break;
                    case WarmupJobType.MoveToInbox:
                        await provider.MoveToInboxAsync(account, job.Id.ToString(), cancellationToken);
                        break;
                }

                job.Status = WarmupJobStatus.Success;
                await _logs.AddAsync(new WarmupEmailLog
                {
                    Id = Guid.NewGuid(),
                    MailAccountId = account.Id,
                    MessageId = messageId,
                    Direction = EmailDirection.Sent,
                    Subject = "Warmup activity",
                    ToAddress = account.EmailAddress,
                    FromAddress = account.EmailAddress,
                    SentAt = DateTime.UtcNow,
                    DeliveredAt = DateTime.UtcNow
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                job.Status = WarmupJobStatus.Failed;
                job.ErrorMessage = ex.Message;
            }

            job.ExecutedAt = DateTime.UtcNow;
            await _jobs.UpdateAsync(job, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
