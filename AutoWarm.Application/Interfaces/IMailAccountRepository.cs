using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoWarm.Domain.Entities;

namespace AutoWarm.Application.Interfaces;

public interface IMailAccountRepository
{
    Task<IReadOnlyCollection<MailAccount>> GetForUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<MailAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(MailAccount account, CancellationToken cancellationToken = default);
    Task UpdateAsync(MailAccount account, CancellationToken cancellationToken = default);
    Task DeleteAsync(MailAccount account, CancellationToken cancellationToken = default);
}
