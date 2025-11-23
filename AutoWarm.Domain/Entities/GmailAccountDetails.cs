using System;

namespace AutoWarm.Domain.Entities;

public class GmailAccountDetails
{
    public Guid MailAccountId { get; set; }
    public string EmailAddress { get; set; } = string.Empty;
    public string GoogleUserId { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime TokenExpiresAt { get; set; }
    public string Scopes { get; set; } = string.Empty;

    public MailAccount? MailAccount { get; set; }
}
