using System;
using System.Threading;
using System.Threading.Tasks;
using AutoWarm.Domain.Entities;

namespace AutoWarm.Application.Interfaces;

public interface IWarmupPlannedEmailRepository
{
    Task AddAsync(WarmupPlannedEmail plannedEmail, CancellationToken cancellationToken = default);
    Task<WarmupPlannedEmail?> GetByInternetMessageIdAsync(string internetMessageId, CancellationToken cancellationToken = default);
    Task<WarmupPlannedEmail?> GetPendingForTargetAsync(Guid targetMailAccountId, string targetAddress, CancellationToken cancellationToken = default);
    Task MarkAppliedAsync(Guid plannedEmailId, DateTime appliedAt, CancellationToken cancellationToken = default);
}
