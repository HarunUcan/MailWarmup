using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoWarm.Application.DTOs;
using AutoWarm.Application.DTOs.Logs;
using AutoWarm.Application.Interfaces;

namespace AutoWarm.Application.Services;

public class WarmupJobService : IWarmupJobService
{
    private readonly IMailAccountRepository _mailAccounts;
    private readonly IWarmupJobRepository _jobs;
    private readonly IWarmupEmailLogRepository _logs;

    public WarmupJobService(
        IMailAccountRepository mailAccounts,
        IWarmupJobRepository jobs,
        IWarmupEmailLogRepository logs)
    {
        _mailAccounts = mailAccounts;
        _jobs = jobs;
        _logs = logs;
    }

    public async Task<DashboardSummaryDto> GetDashboardSummaryAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var accounts = await _mailAccounts.GetForUserAsync(userId, cancellationToken);
        if (accounts.Count == 0)
        {
            return new DashboardSummaryDto(0, 0, 0, 0);
        }

        var activeAccounts = accounts.Count(a => a.Status == Domain.Enums.MailAccountStatus.Connected);
        var accountIds = accounts.Select(a => a.Id).ToArray();
        var pendingJobs = await _jobs.CountPendingForAccountsAsync(accountIds, cancellationToken);

        // This is a placeholder; in a real system these would be aggregated from logs or analytics tables.
        var todayLocal = DateTime.Now.Date;
        var utcStart = TimeZoneInfo.ConvertTimeToUtc(todayLocal);
        var utcEnd = TimeZoneInfo.ConvertTimeToUtc(todayLocal.AddDays(1).AddTicks(-1));
        var logs = await _logs.QueryForAccountsAsync(accountIds, utcStart, utcEnd, cancellationToken);
        var sent = logs.Count(l => l.Direction == Domain.Enums.EmailDirection.Sent);
        var replies = logs.Count(l => l.Direction == Domain.Enums.EmailDirection.Replied);

        return new DashboardSummaryDto(activeAccounts, sent, replies, pendingJobs);
    }

    public async Task<IReadOnlyCollection<WarmupLogDto>> GetLogsAsync(Guid userId, WarmupLogFilter filter, CancellationToken cancellationToken = default)
    {
        Guid? mailAccountId = filter.MailAccountId;
        if (mailAccountId.HasValue)
        {
            var account = await _mailAccounts.GetByIdAsync(mailAccountId.Value, cancellationToken);
            if (account is null || account.UserId != userId)
            {
                throw new InvalidOperationException("Mail account not found.");
            }
        }

        DateTime? fromUtc = null;
        DateTime? toUtc = null;

        if (filter.From.HasValue)
        {
            var local = DateTime.SpecifyKind(filter.From.Value, DateTimeKind.Local);
            fromUtc = TimeZoneInfo.ConvertTimeToUtc(local);
        }

        if (filter.To.HasValue)
        {
            // include the full day by taking end-of-day local time
            var localEnd = DateTime.SpecifyKind(filter.To.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Local);
            toUtc = TimeZoneInfo.ConvertTimeToUtc(localEnd);
        }

        var accounts = await _mailAccounts.GetForUserAsync(userId, cancellationToken);
        var allowedAccountIds = accounts.Select(a => a.Id).ToArray();
        if (mailAccountId.HasValue && !allowedAccountIds.Contains(mailAccountId.Value))
        {
            return Array.Empty<WarmupLogDto>();
        }

        var targetIds = mailAccountId.HasValue ? new[] { mailAccountId.Value } : allowedAccountIds;
        var logs = await _logs.QueryForAccountsAsync(targetIds, fromUtc, toUtc, cancellationToken);
        return logs
            .Select(l => new WarmupLogDto(
                l.Id,
                l.MailAccountId,
                l.MessageId,
                l.Direction,
                l.Subject,
                l.ToAddress,
                l.FromAddress,
                l.SentAt,
                l.DeliveredAt,
                l.OpenedAt,
                l.MarkedAsImportant,
                l.IsSpam))
            .ToArray();
    }
}
