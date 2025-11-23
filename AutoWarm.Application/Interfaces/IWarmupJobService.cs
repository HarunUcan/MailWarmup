using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoWarm.Application.DTOs;
using AutoWarm.Application.DTOs.Logs;

namespace AutoWarm.Application.Interfaces;

public interface IWarmupJobService
{
    Task<DashboardSummaryDto> GetDashboardSummaryAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<WarmupLogDto>> GetLogsAsync(Guid userId, WarmupLogFilter filter, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<ReputationScoreDto>> GetReputationScoresAsync(Guid userId, CancellationToken cancellationToken = default);
}
