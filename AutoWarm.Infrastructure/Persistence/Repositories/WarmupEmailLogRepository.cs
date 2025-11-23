using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoWarm.Application.Interfaces;
using AutoWarm.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AutoWarm.Infrastructure.Persistence.Repositories;

public class WarmupEmailLogRepository : IWarmupEmailLogRepository
{
    private readonly AppDbContext _context;

    public WarmupEmailLogRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(WarmupEmailLog log, CancellationToken cancellationToken = default)
    {
        await _context.WarmupEmailLogs.AddAsync(log, cancellationToken);
    }

    public async Task<IReadOnlyCollection<WarmupEmailLog>> QueryAsync(Guid? mailAccountId, DateTime? from, DateTime? to, CancellationToken cancellationToken = default)
    {
        var query = _context.WarmupEmailLogs.AsQueryable();

        if (mailAccountId.HasValue)
        {
            query = query.Where(l => l.MailAccountId == mailAccountId.Value);
        }

        if (from.HasValue)
        {
            query = query.Where(l => l.SentAt >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(l => l.SentAt <= to.Value);
        }

        var logs = await query.OrderByDescending(l => l.SentAt).ToListAsync(cancellationToken);
        return logs;
    }

    public async Task<IReadOnlyCollection<WarmupEmailLog>> QueryForAccountsAsync(IEnumerable<Guid> mailAccountIds, DateTime? from, DateTime? to, CancellationToken cancellationToken = default)
    {
        var idList = mailAccountIds.ToList();
        if (idList.Count == 0)
        {
            return Array.Empty<WarmupEmailLog>();
        }

        var query = _context.WarmupEmailLogs.Where(l => idList.Contains(l.MailAccountId));

        if (from.HasValue)
        {
            query = query.Where(l => l.SentAt >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(l => l.SentAt <= to.Value);
        }

        var logs = await query.OrderByDescending(l => l.SentAt).ToListAsync(cancellationToken);
        return logs;
    }
}
