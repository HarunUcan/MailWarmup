using System;
using AutoWarm.Application.Interfaces;
using AutoWarm.Domain.Enums;
using AutoWarm.Domain.Interfaces;
using AutoWarm.Infrastructure.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AutoWarm.Infrastructure.Services.Ai;

public class AiTextProviderFactory : IAiTextProviderFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly AiProviderOptions _options;

    public AiTextProviderFactory(IServiceProvider serviceProvider, IOptions<AiProviderOptions> options)
    {
        _serviceProvider = serviceProvider;
        _options = options.Value;
    }

    public IAiTextProvider Create()
    {
        return _options.ProviderType switch
        {
            AiProviderType.Gemini => _serviceProvider.GetRequiredService<GeminiTextProvider>(),
            AiProviderType.OpenAi => _serviceProvider.GetRequiredService<OpenAiTextProvider>(),
            AiProviderType.Ollama => _serviceProvider.GetRequiredService<OllamaTextProvider>(),
            _ => throw new InvalidOperationException("Unsupported AI provider")
        };
    }
}
