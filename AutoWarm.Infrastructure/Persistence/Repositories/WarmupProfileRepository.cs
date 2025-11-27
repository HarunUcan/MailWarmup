using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoWarm.Application.Interfaces;
using AutoWarm.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AutoWarm.Infrastructure.Persistence.Repositories;

public class WarmupProfileRepository : IWarmupProfileRepository
{
    private readonly AppDbContext _context;

    public WarmupProfileRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyCollection<WarmupProfile>> GetByMailAccountAsync(Guid mailAccountId, CancellationToken cancellationToken = default)
    {
        var profiles = await _context.WarmupProfiles
            .AsNoTracking()
            .Where(p => p.MailAccountId == mailAccountId)
            .ToListAsync(cancellationToken);
        return profiles;
    }

    public Task<WarmupProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _context.WarmupProfiles.SingleOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task AddAsync(WarmupProfile profile, CancellationToken cancellationToken = default)
    {
        await _context.WarmupProfiles.AddAsync(profile, cancellationToken);
    }

    public Task UpdateAsync(WarmupProfile profile, CancellationToken cancellationToken = default)
    {
        _context.WarmupProfiles.Update(profile);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(WarmupProfile profile, CancellationToken cancellationToken = default)
    {
        _context.WarmupProfiles.Remove(profile);
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyCollection<WarmupProfile>> GetActiveProfilesAsync(CancellationToken cancellationToken = default)
    {
        var localToday = DateTime.Now.Date;
        var utcDayStart = TimeZoneInfo.ConvertTimeToUtc(localToday);
        var profiles = await _context.WarmupProfiles
            .Include(p => p.MailAccount!)
                .ThenInclude(a => a.GmailDetails)
            .Include(p => p.MailAccount!)
                .ThenInclude(a => a.SmtpImapDetails)
            .Where(p => p.IsEnabled && p.StartDate <= utcDayStart && (p.MaxDurationDays == 0 || p.CurrentDay < p.MaxDurationDays))
            .ToListAsync(cancellationToken);
        return profiles;
    }
}
