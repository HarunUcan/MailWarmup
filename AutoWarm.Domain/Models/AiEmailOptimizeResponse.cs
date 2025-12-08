namespace AutoWarm.Domain.Models;

public class AiEmailOptimizeResponse
{
    public string OptimizedSubject { get; set; } = string.Empty;
    public string OptimizedBody { get; set; } = string.Empty;
    public string ExplanationSummary { get; set; } = string.Empty;
}
