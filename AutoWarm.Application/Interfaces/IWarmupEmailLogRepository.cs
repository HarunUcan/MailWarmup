using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoWarm.Domain.Entities;

namespace AutoWarm.Application.Interfaces;

public interface IWarmupEmailLogRepository
{
    Task AddAsync(WarmupEmailLog log, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<WarmupEmailLog>> QueryAsync(Guid? mailAccountId, DateTime? from, DateTime? to, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<WarmupEmailLog>> QueryForAccountsAsync(IEnumerable<Guid> mailAccountIds, DateTime? from, DateTime? to, CancellationToken cancellationToken = default);
}
