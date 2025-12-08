using System.Threading;
using System.Threading.Tasks;
using AutoWarm.Domain.Models;

namespace AutoWarm.Domain.Interfaces;

public interface IAiTextProvider
{
    Task<AiEmailOptimizeResponse> OptimizeEmailAsync(
        AiEmailOptimizeRequest request,
        CancellationToken cancellationToken = default);
}
