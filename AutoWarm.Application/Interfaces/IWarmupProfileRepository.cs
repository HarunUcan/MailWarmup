using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoWarm.Domain.Entities;

namespace AutoWarm.Application.Interfaces;

public interface IWarmupProfileRepository
{
    Task<IReadOnlyCollection<WarmupProfile>> GetByMailAccountAsync(Guid mailAccountId, CancellationToken cancellationToken = default);
    Task<WarmupProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(WarmupProfile profile, CancellationToken cancellationToken = default);
    Task UpdateAsync(WarmupProfile profile, CancellationToken cancellationToken = default);
    Task DeleteAsync(WarmupProfile profile, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<WarmupProfile>> GetActiveProfilesAsync(CancellationToken cancellationToken = default);
}
