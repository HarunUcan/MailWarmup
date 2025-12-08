using System;
using System.Threading;
using System.Threading.Tasks;
using AutoWarm.Domain.Interfaces;
using AutoWarm.Domain.Models;

namespace AutoWarm.Infrastructure.Services.Ai;

public class OpenAiTextProvider : IAiTextProvider
{
    public Task<AiEmailOptimizeResponse> OptimizeEmailAsync(AiEmailOptimizeRequest request, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("OpenAI sağlayıcısı henüz uygulanmadı.");
    }
}
