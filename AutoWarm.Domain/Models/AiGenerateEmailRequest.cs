namespace AutoWarm.Domain.Models;

public class AiGenerateEmailRequest
{
    public string Language { get; set; } = "tr";
    public string Tone { get; set; } = "Professional"; // Casual, Professional, Friendly
    public string Topic { get; set; } = "General"; // Business, Technology, Greeting
}
