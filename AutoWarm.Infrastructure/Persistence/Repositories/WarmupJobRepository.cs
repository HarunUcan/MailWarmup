using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoWarm.Application.Interfaces;
using AutoWarm.Domain.Entities;
using AutoWarm.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AutoWarm.Infrastructure.Persistence.Repositories;

public class WarmupJobRepository : IWarmupJobRepository
{
    private readonly AppDbContext _context;

    public WarmupJobRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddRangeAsync(IEnumerable<WarmupJob> jobs, CancellationToken cancellationToken = default)
    {
        await _context.WarmupJobs.AddRangeAsync(jobs, cancellationToken);
    }

    public async Task<IReadOnlyCollection<WarmupJob>> GetPendingJobsAsync(DateTime now, CancellationToken cancellationToken = default)
    {
        var jobs = await _context.WarmupJobs
            .Where(j => j.Status == WarmupJobStatus.Pending && j.ScheduledAt <= now)
            .OrderBy(j => j.ScheduledAt)
            .ToListAsync(cancellationToken);
        return jobs;
    }

    public Task UpdateAsync(WarmupJob job, CancellationToken cancellationToken = default)
    {
        _context.WarmupJobs.Update(job);
        return Task.CompletedTask;
    }

    public Task<int> CountPendingAsync(CancellationToken cancellationToken = default)
    {
        return _context.WarmupJobs.CountAsync(j => j.Status == WarmupJobStatus.Pending, cancellationToken);
    }

    public Task<int> CountPendingForAccountsAsync(IEnumerable<Guid> mailAccountIds, CancellationToken cancellationToken = default)
    {
        var idList = mailAccountIds.ToList();
        if (idList.Count == 0)
        {
            return Task.FromResult(0);
        }

        return _context.WarmupJobs.CountAsync(
            j => j.Status == WarmupJobStatus.Pending && idList.Contains(j.MailAccountId),
            cancellationToken);
    }

    public Task<bool> HasJobsInRangeAsync(Guid mailAccountId, DateTime utcStart, DateTime utcEnd, CancellationToken cancellationToken = default)
    {
        return _context.WarmupJobs.AnyAsync(
            j => j.MailAccountId == mailAccountId &&
                 j.ScheduledAt >= utcStart &&
                 j.ScheduledAt <= utcEnd,
            cancellationToken);
    }

    public async Task<IReadOnlyCollection<WarmupJob>> GetByMailAccountAsync(Guid mailAccountId, CancellationToken cancellationToken = default)
    {
        var jobs = await _context.WarmupJobs
            .Where(j => j.MailAccountId == mailAccountId)
            .OrderByDescending(j => j.ScheduledAt)
            .ToListAsync(cancellationToken);
        return jobs;
    }
}
