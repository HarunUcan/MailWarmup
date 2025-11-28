using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoWarm.Application.DTOs;
using AutoWarm.Application.DTOs.Logs;
using AutoWarm.Application.Interfaces;
using AutoWarm.Domain.Enums;

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

    public async Task<IReadOnlyCollection<ReputationScoreDto>> GetReputationScoresAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var accounts = await _mailAccounts.GetForUserAsync(userId, cancellationToken);
        if (accounts.Count == 0)
        {
            return Array.Empty<ReputationScoreDto>();
        }

        var accountIds = accounts.Select(a => a.Id).ToList();
        var toUtc = DateTime.UtcNow.Date.AddDays(1).AddTicks(-1);
        var fromUtc = toUtc.AddDays(-6).Date;

        var logs = await _logs.QueryForAccountsAsync(accountIds, fromUtc, toUtc, cancellationToken);
        var jobs = await _jobs.GetByAccountsInRangeAsync(accountIds, fromUtc, toUtc, cancellationToken);

        var scores = new List<ReputationScoreDto>();
        foreach (var account in accounts)
        {
            var accountLogs = logs.Where(l => l.MailAccountId == account.Id).ToList();
            var accountJobs = jobs.Where(j => j.MailAccountId == account.Id).ToList();
            var trend = new List<double>();

            for (var i = 0; i < 7; i++)
            {
                var dayStart = fromUtc.AddDays(i);
                var dayEnd = dayStart.AddDays(1);
                var dayLogs = accountLogs.Where(l => l.SentAt.HasValue && l.SentAt.Value >= dayStart && l.SentAt.Value < dayEnd).ToList();
                var dayJobs = accountJobs.Where(j => j.ScheduledAt >= dayStart && j.ScheduledAt < dayEnd).ToList();
                trend.Add(CalculateScore(dayLogs, dayJobs));
            }

            var score = CalculateScore(accountLogs, accountJobs);
            var label = score >= 85 ? "Sağlıklı" : score >= 70 ? "Normal" : score >= 50 ? "Riskli" : "Kritik";

            scores.Add(new ReputationScoreDto(account.Id, account.EmailAddress, Math.Round(score, 1), label, trend));
        }

        return scores;
    }

    private static double CalculateScore(IEnumerable<Domain.Entities.WarmupEmailLog> logs, IEnumerable<Domain.Entities.WarmupJob> jobs)
    {
        var logList = logs.ToList();
        var jobList = jobs.ToList();

        var sent = logList.Count(l => l.Direction == EmailDirection.Sent);
        var replies = logList.Count(l => l.Direction == EmailDirection.Replied);
        var spam = logList.Count(l => l.IsSpam);
        var failed = jobList.Count(j => j.Status == Domain.Enums.WarmupJobStatus.Failed);

        var spamRate = sent > 0 ? (double)spam / sent : 0;
        var bounceRate = (sent + failed) > 0 ? (double)failed / (sent + failed) : 0;
        var replyRate = sent > 0 ? (double)replies / sent : 0;

        // Stability based on daily send counts
        var dailyGroups = logList
            .Where(l => l.SentAt.HasValue)
            .GroupBy(l => l.SentAt!.Value.Date)
            .Select(g => g.Count())
            .ToList();

        double variability = 0;
        if (dailyGroups.Count > 1)
        {
            var avg = dailyGroups.Average();
            if (avg > 0)
            {
                var variance = dailyGroups.Select(c => Math.Pow(c - avg, 2)).Average();
                var stdDev = Math.Sqrt(variance);
                variability = Math.Min(1.0, stdDev / avg);
            }
        }

        var negativeSignals = Math.Min(1.0, variability + Math.Max(0, 0.3 - replyRate));
        var rawScore = 100 - (spamRate * 40 + bounceRate * 25 + negativeSignals * 20);
        return Math.Clamp(rawScore, 0, 100);
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
                l.MarkedAsStarred,
                l.IsSpam))
            .ToArray();
    }
}
