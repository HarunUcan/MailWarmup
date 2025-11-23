namespace AutoWarm.Infrastructure.Settings;

public class GmailOAuthOptions
{
    public const string SectionName = "Gmail";
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string RedirectUri { get; set; } = string.Empty;
    public string Scopes { get; set; } = "https://www.googleapis.com/auth/gmail.modify";
    public string FrontendRedirectUri { get; set; } = string.Empty;
}
