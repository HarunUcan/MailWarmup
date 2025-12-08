using AutoWarm.Domain.Enums;

namespace AutoWarm.Infrastructure.Settings;

public class AiProviderOptions
{
    public const string SectionName = "AiProvider";

    public AiProviderType ProviderType { get; set; } = AiProviderType.Gemini;

    public string GeminiApiKey { get; set; } = string.Empty;
    public string GeminiModelName { get; set; } = "gemini-1.5-flash-latest";

    public string OpenAiApiKey { get; set; } = string.Empty;
    public string OpenAiModelName { get; set; } = "gpt-4.1-mini";

    public string OllamaBaseUrl { get; set; } = "http://localhost:11434";
    public string OllamaModelName { get; set; } = "llama3";
}
