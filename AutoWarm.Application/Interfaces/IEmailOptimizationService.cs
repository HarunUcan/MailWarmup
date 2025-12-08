using System.Threading;
using System.Threading.Tasks;
using AutoWarm.Domain.Models;

namespace AutoWarm.Application.Interfaces;

public interface IEmailOptimizationService
{
    Task<AiEmailOptimizeResponse> OptimizeAsync(
        AiEmailOptimizeRequest request,
        CancellationToken cancellationToken = default);
}
