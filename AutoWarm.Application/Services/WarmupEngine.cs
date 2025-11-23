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
    private static readonly Random _random = new();
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
        var networkProfiles = await _profiles.GetActiveProfilesAsync(cancellationToken);
        var networkAccounts = networkProfiles
            .Select(p => p.MailAccount)
            .Where(a => a is not null && a.Status == MailAccountStatus.Connected)
            .Cast<MailAccount>()
            .ToList();

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
                string toAddress = account.EmailAddress;
                string subject = "Warmup ping";

                // Resolve a target account for send or reply.
                var targetLog = await GetLatestInboundAsync(account, cancellationToken);
                if (job.Type == WarmupJobType.ReplyEmail && targetLog is not null)
                {
                    toAddress = targetLog.FromAddress;
                    subject = $"Re: {targetLog.Subject}";
                }
                else
                {
                    var peer = SelectPeer(networkAccounts, account.Id);
                    if (peer is not null)
                    {
                        toAddress = peer.EmailAddress;
                    }
                }

                switch (job.Type)
                {
                    case WarmupJobType.SendEmail:
                        messageId = await provider.SendEmailAsync(
                            account,
                            toAddress,
                            subject,
                            $"Hello from AutoWarm warmup. Target: {toAddress}",
                            cancellationToken);

                        // Schedule a reply from the peer after a short delay to simulate conversation.
                        var peerForReply = networkAccounts.FirstOrDefault(a => string.Equals(a.EmailAddress, toAddress, StringComparison.OrdinalIgnoreCase));
                        if (peerForReply is not null && peerForReply.Id != account.Id)
                        {
                            var delayMinutes = _random.Next(5, 16); // 5-15 minutes
                            var replyJob = new WarmupJob
                            {
                                Id = Guid.NewGuid(),
                                MailAccountId = peerForReply.Id,
                                ScheduledAt = DateTime.UtcNow.AddMinutes(delayMinutes),
                                Type = WarmupJobType.ReplyEmail,
                                Status = WarmupJobStatus.Pending
                            };
                            await _jobs.AddRangeAsync(new[] { replyJob }, cancellationToken);
                        }
                        break;
                    case WarmupJobType.ReplyEmail:
                        messageId = await provider.SendEmailAsync(
                            account,
                            toAddress,
                            subject,
                            $"Replying in warmup mesh to {toAddress}.",
                            cancellationToken);
                        break;
                    case WarmupJobType.MarkImportant:
                        await provider.MarkAsImportantAsync(account, job.Id.ToString(), cancellationToken);
                        break;
                    case WarmupJobType.MoveToInbox:
                        await provider.MoveToInboxAsync(account, job.Id.ToString(), cancellationToken);
                        break;
                }

                job.Status = WarmupJobStatus.Success;
                var direction = job.Type == WarmupJobType.ReplyEmail ? EmailDirection.Replied : EmailDirection.Sent;
                await _logs.AddAsync(new WarmupEmailLog
                {
                    Id = Guid.NewGuid(),
                    MailAccountId = account.Id,
                    MessageId = messageId,
                    Direction = direction,
                    Subject = subject,
                    ToAddress = toAddress,
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

    private static MailAccount? SelectPeer(IReadOnlyCollection<MailAccount> network, Guid sourceAccountId)
    {
        var peers = network.Where(a => a.Id != sourceAccountId).ToList();
        if (peers.Count == 0)
        {
            return null;
        }

        lock (_random)
        {
            var index = _random.Next(peers.Count);
            return peers[index];
        }
    }

    private async Task<WarmupEmailLog?> GetLatestInboundAsync(MailAccount account, CancellationToken cancellationToken)
    {
        var logs = await _logs.QueryForAccountsAsync(new[] { account.Id }, null, null, cancellationToken);
        return logs
            .Where(l =>
                l.ToAddress.Equals(account.EmailAddress, StringComparison.OrdinalIgnoreCase) &&
                !l.FromAddress.Equals(account.EmailAddress, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(l => l.SentAt ?? DateTime.MinValue)
            .FirstOrDefault();
    }
}
