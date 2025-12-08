using System.Threading;
using System.Threading.Tasks;
using AutoWarm.Application.Interfaces;
using AutoWarm.Domain.Models;

namespace AutoWarm.Application.Services;

public class EmailOptimizationService : IEmailOptimizationService
{
    private readonly IAiTextProviderFactory _providerFactory;

    public EmailOptimizationService(IAiTextProviderFactory providerFactory)
    {
        _providerFactory = providerFactory;
    }

    public Task<AiEmailOptimizeResponse> OptimizeAsync(AiEmailOptimizeRequest request, CancellationToken cancellationToken = default)
    {
        var provider = _providerFactory.Create();
        return provider.OptimizeEmailAsync(request, cancellationToken);
    }
}
