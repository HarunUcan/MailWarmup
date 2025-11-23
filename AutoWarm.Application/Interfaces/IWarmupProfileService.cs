using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoWarm.Application.DTOs.WarmupProfiles;

namespace AutoWarm.Application.Interfaces;

public interface IWarmupProfileService
{
    Task<IReadOnlyCollection<WarmupProfileDto>> GetByMailAccountAsync(Guid userId, Guid mailAccountId, CancellationToken cancellationToken = default);
    Task<WarmupProfileDto> CreateAsync(Guid userId, CreateWarmupProfileRequest request, CancellationToken cancellationToken = default);
    Task<WarmupProfileDto> UpdateAsync(Guid userId, Guid id, UpdateWarmupProfileRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid userId, Guid id, CancellationToken cancellationToken = default);
}
