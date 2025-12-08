using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AutoWarm.Domain.Interfaces;
using AutoWarm.Domain.Models;
using AutoWarm.Infrastructure.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AutoWarm.Infrastructure.Services.Ai;

public class GeminiTextProvider : IAiTextProvider
{
    private readonly HttpClient _httpClient;
    private readonly AiProviderOptions _options;
    private readonly ILogger<GeminiTextProvider> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public GeminiTextProvider(HttpClient httpClient, IOptions<AiProviderOptions> options, ILogger<GeminiTextProvider> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<AiEmailOptimizeResponse> OptimizeEmailAsync(AiEmailOptimizeRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.GeminiApiKey))
        {
            throw new InvalidOperationException("Gemini API anahtarı yapılandırılmamış.");
        }

        var prompt = BuildPrompt(request);
        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    role = "user",
                    parts = new[] { new { text = prompt } }
                }
            },
            generationConfig = new
            {
                temperature = 0.35,
                topP = 0.9,
                responseMimeType = "application/json"
            }
        };

        var endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/{_options.GeminiModelName}:generateContent?key={_options.GeminiApiKey}";
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = JsonContent.Create(requestBody)
        };

        try
        {
            var httpResponse = await _httpClient.SendAsync(httpRequest, cancellationToken);
            if (!httpResponse.IsSuccessStatusCode)
            {
                var errorContent = await httpResponse.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Gemini  API çağrısı {Status} döndü. İçerik: {Error}", (int)httpResponse.StatusCode, errorContent);

                if (httpResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    throw new InvalidOperationException($"Gemini modeli bulunamadı veya erişilemiyor: {_options.GeminiModelName}. Model adını ve API erişimini kontrol edin. İçerik: {errorContent}");
                }

                throw new InvalidOperationException($"Gemini isteği başarısız: {(int)httpResponse.StatusCode} {httpResponse.ReasonPhrase}. İçerik: {errorContent}");
            }

            await using var contentStream = await httpResponse.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(contentStream, cancellationToken: cancellationToken);

            var text = ExtractText(document);
            var parsed = TryParseResponse(text, request);
            if (parsed != null)
            {
                return parsed;
            }

            _logger.LogWarning("Gemini yanıtı beklendiği formatta değil. İçerik: {Text}", text);
            return BuildFallbackResponse(request, "AI yanıtı beklendiği formatta dönmedi.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gemini optimizeEmail çağrısı başarısız.");
            throw;
        }
    }

    private static string BuildPrompt(AiEmailOptimizeRequest request)
    {
        var language = string.IsNullOrWhiteSpace(request.Language) ? "tr" : request.Language;
        var mode = string.IsNullOrWhiteSpace(request.Mode) ? "SpamSafe" : request.Mode;

        var sb = new StringBuilder();
        sb.AppendLine("Görev: Aşağıdaki e-postanın spam klasörüne düşme ihtimalini azalt.");
        sb.AppendLine("Kullanıcının tonunu mümkün olduğunca koru.");
        sb.AppendLine("Spamle ilişkilendirilen agresif kelimeleri, ALL CAPS ve çok ünlem içeren kısımları yumuşat.");
        sb.AppendLine("Link sayısını abartma, aşırı satış kokan cümlelerden kaçın.");
        sb.AppendLine("Çıkış dili (subject + body + explanation) tamamen: " + language + ". Farklı bir dil gelirse çevir ve tutarlı kal.");
        sb.AppendLine("Subject ve Body için de hedef dil " + language + " kullan.");
        sb.AppendLine();
        sb.AppendLine("Mode kuralları:");
        sb.AppendLine("- SpamSafe: içeriği çok bozmadan sadece riskli yerleri düzelt.");
        sb.AppendLine("- SpamSafeAndProfessional: spam riskini azaltırken daha profesyonel bir üslup kullan.");
        sb.AppendLine($"Seçili mode: {mode}");
        sb.AppendLine();
        sb.AppendLine("Yanıtı sadece JSON olarak döndür. Anahtarlar:");
        sb.AppendLine("{");
        sb.AppendLine("  \"optimizedSubject\": \"string\",");
        sb.AppendLine("  \"optimizedBody\": \"string\",");
        sb.AppendLine("  \"explanationSummary\": \"string\"");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("Orijinal konu:");
        sb.AppendLine(request.Subject ?? string.Empty);
        sb.AppendLine();
        sb.AppendLine("Orijinal gövde:");
        sb.AppendLine(request.Body ?? string.Empty);

        return sb.ToString();
    }

    private static string ExtractText(JsonDocument document)
    {
        if (!document.RootElement.TryGetProperty("candidates", out var candidates) ||
            candidates.ValueKind != JsonValueKind.Array ||
            candidates.GetArrayLength() == 0)
        {
            return string.Empty;
        }

        var first = candidates[0];
        if (first.TryGetProperty("content", out var content) &&
            content.TryGetProperty("parts", out var parts) &&
            parts.ValueKind == JsonValueKind.Array &&
            parts.GetArrayLength() > 0)
        {
            var part = parts[0];
            if (part.ValueKind == JsonValueKind.Object && part.TryGetProperty("json", out var jsonElement))
            {
                return jsonElement.GetRawText();
            }

            if (part.ValueKind == JsonValueKind.Object && part.TryGetProperty("text", out var textElement) && textElement.ValueKind == JsonValueKind.String)
            {
                return textElement.GetString() ?? string.Empty;
            }

            if (part.ValueKind == JsonValueKind.String)
            {
                return part.GetString() ?? string.Empty;
            }
        }

        return string.Empty;
    }

    private static AiEmailOptimizeResponse? TryParseResponse(string? text, AiEmailOptimizeRequest request)
    {
        var cleaned = CleanupJson(text);
        if (string.IsNullOrWhiteSpace(cleaned))
        {
            return null;
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<AiEmailOptimizeResponse>(cleaned, JsonOptions);
            if (parsed == null)
            {
                return null;
            }

            parsed.OptimizedSubject = string.IsNullOrWhiteSpace(parsed.OptimizedSubject)
                ? request.Subject
                : parsed.OptimizedSubject;
            parsed.OptimizedBody = string.IsNullOrWhiteSpace(parsed.OptimizedBody)
                ? request.Body
                : parsed.OptimizedBody;
            if (string.IsNullOrWhiteSpace(parsed.ExplanationSummary))
            {
                parsed.ExplanationSummary = "AI yanıtı açıklama sağlamadı.";
            }

            return parsed;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string CleanupJson(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var trimmed = text.Trim();
        if (trimmed.StartsWith("```", StringComparison.Ordinal))
        {
            trimmed = trimmed.Replace("```json", string.Empty, StringComparison.OrdinalIgnoreCase)
                             .Replace("```", string.Empty, StringComparison.OrdinalIgnoreCase)
                             .Trim();
        }

        var firstBrace = trimmed.IndexOf('{');
        var lastBrace = trimmed.LastIndexOf('}');
        if (firstBrace >= 0 && lastBrace >= firstBrace)
        {
            return trimmed[firstBrace..(lastBrace + 1)];
        }

        return trimmed;
    }

    private static AiEmailOptimizeResponse BuildFallbackResponse(AiEmailOptimizeRequest request, string explanation)
    {
        return new AiEmailOptimizeResponse
        {
            OptimizedSubject = string.IsNullOrWhiteSpace(request.Subject) ? string.Empty : request.Subject,
            OptimizedBody = string.IsNullOrWhiteSpace(request.Body) ? string.Empty : request.Body,
            ExplanationSummary = explanation
        };
    }
}
