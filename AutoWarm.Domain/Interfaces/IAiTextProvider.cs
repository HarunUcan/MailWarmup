using System.Threading;
using System.Threading.Tasks;
using AutoWarm.Domain.Models;

namespace AutoWarm.Domain.Interfaces;

public interface IAiTextProvider
{
    Task<AiEmailOptimizeResponse> OptimizeEmailAsync(
        AiEmailOptimizeRequest request,
        CancellationToken cancellationToken = default);

    Task<AiGenerateEmailResponse> GenerateWarmupEmailAsync(
        AiGenerateEmailRequest request,
        CancellationToken cancellationToken = default);

    Task<AiGenerateEmailResponse> GenerateReplyAsync(
        AiGenerateReplyRequest request,
        CancellationToken cancellationToken = default);
}
