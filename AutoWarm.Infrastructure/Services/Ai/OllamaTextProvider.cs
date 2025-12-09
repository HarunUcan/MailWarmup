using System;
using System.Threading;
using System.Threading.Tasks;
using AutoWarm.Domain.Interfaces;
using AutoWarm.Domain.Models;

namespace AutoWarm.Infrastructure.Services.Ai;

public class OllamaTextProvider : IAiTextProvider
{
    public Task<AiEmailOptimizeResponse> OptimizeEmailAsync(AiEmailOptimizeRequest request, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Ollama sağlayıcısı henüz uygulanmadı.");
    }

    public Task<AiGenerateEmailResponse> GenerateWarmupEmailAsync(AiGenerateEmailRequest request, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<AiGenerateEmailResponse> GenerateReplyAsync(AiGenerateReplyRequest request, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
