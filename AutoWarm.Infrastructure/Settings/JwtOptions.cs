namespace AutoWarm.Infrastructure.Settings;

public class JwtOptions
{
    public const string SectionName = "Jwt";
    public string Issuer { get; set; } = "AutoWarm";
    public string Audience { get; set; } = "AutoWarmAudience";
    public string SigningKey { get; set; } = string.Empty;
    public int AccessTokenMinutes { get; set; } = 60;
    public int RefreshTokenMinutes { get; set; } = 60 * 24 * 7;
}
