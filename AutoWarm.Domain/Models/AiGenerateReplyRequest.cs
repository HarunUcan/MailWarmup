namespace AutoWarm.Domain.Models;

public class AiGenerateReplyRequest
{
    public string OriginalSubject { get; set; } = string.Empty;
    public string OriginalBody { get; set; } = string.Empty;
    public string OriginalSender { get; set; } = string.Empty;
    public string Language { get; set; } = "tr";
    public string Tone { get; set; } = "Professional";
}
