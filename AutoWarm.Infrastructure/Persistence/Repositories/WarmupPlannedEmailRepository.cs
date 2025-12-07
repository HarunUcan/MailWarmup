using System;
using System.Threading;
using System.Threading.Tasks;
using AutoWarm.Application.Interfaces;
using AutoWarm.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AutoWarm.Infrastructure.Persistence.Repositories;

public class WarmupPlannedEmailRepository : IWarmupPlannedEmailRepository
{
    private readonly AppDbContext _context;

    public WarmupPlannedEmailRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task AddAsync(WarmupPlannedEmail plannedEmail, CancellationToken cancellationToken = default)
    {
        return _context.WarmupPlannedEmails.AddAsync(plannedEmail, cancellationToken).AsTask();
    }

    public Task<WarmupPlannedEmail?> GetByInternetMessageIdAsync(string internetMessageId, CancellationToken cancellationToken = default)
    {
        return _context.WarmupPlannedEmails.FirstOrDefaultAsync(p => p.InternetMessageId == internetMessageId, cancellationToken);
    }

    public Task<WarmupPlannedEmail?> GetPendingForTargetAsync(Guid targetMailAccountId, string targetAddress, CancellationToken cancellationToken = default)
    {
        return _context.WarmupPlannedEmails
            .Where(p => p.AppliedAt == null &&
                        (p.TargetMailAccountId == targetMailAccountId || (p.TargetMailAccountId == null && p.TargetAddress == targetAddress)))
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task MarkAppliedAsync(Guid plannedEmailId, DateTime appliedAt, CancellationToken cancellationToken = default)
    {
        var planned = await _context.WarmupPlannedEmails.FirstOrDefaultAsync(p => p.Id == plannedEmailId, cancellationToken);
        if (planned is null)
        {
            return;
        }

        planned.AppliedAt = appliedAt;
        _context.WarmupPlannedEmails.Update(planned);
    }
}
