using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoWarm.Domain.Entities;
using AutoWarm.Domain.Enums;

namespace AutoWarm.Application.Interfaces;

public interface IWarmupJobRepository
{
    Task AddRangeAsync(IEnumerable<WarmupJob> jobs, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<WarmupJob>> GetPendingJobsAsync(DateTime now, CancellationToken cancellationToken = default);
    Task UpdateAsync(WarmupJob job, CancellationToken cancellationToken = default);
    Task<int> CountPendingAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<WarmupJob>> GetByMailAccountAsync(Guid mailAccountId, CancellationToken cancellationToken = default);
}
