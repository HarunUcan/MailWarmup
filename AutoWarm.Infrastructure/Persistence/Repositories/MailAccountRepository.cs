using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoWarm.Application.Interfaces;
using AutoWarm.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AutoWarm.Infrastructure.Persistence.Repositories;

public class MailAccountRepository : IMailAccountRepository
{
    private readonly AppDbContext _context;

    public MailAccountRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyCollection<MailAccount>> GetForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var accounts = await _context.MailAccounts
            .AsNoTracking()
            .Where(a => a.UserId == userId)
            .ToListAsync(cancellationToken);
        return accounts;
    }

    public Task<MailAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _context.MailAccounts
            .Include(a => a.GmailDetails)
            .Include(a => a.SmtpImapDetails)
            .Include(a => a.WarmupProfile)
            .SingleOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task AddAsync(MailAccount account, CancellationToken cancellationToken = default)
    {
        await _context.MailAccounts.AddAsync(account, cancellationToken);
    }

    public Task UpdateAsync(MailAccount account, CancellationToken cancellationToken = default)
    {
        _context.MailAccounts.Update(account);
        return Task.CompletedTask;
    }
}
