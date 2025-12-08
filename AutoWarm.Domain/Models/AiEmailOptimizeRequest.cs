namespace AutoWarm.Domain.Models;

public class AiEmailOptimizeRequest
{
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string Mode { get; set; } = "SpamSafe";
    public string? Language { get; set; } = "tr";
}
