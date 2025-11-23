using System;

namespace AutoWarm.Domain.Entities;

public class SmtpImapAccountDetails
{
    public Guid MailAccountId { get; set; }
    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; }
    public bool SmtpUseSsl { get; set; }
    public string SmtpUsername { get; set; } = string.Empty;
    public string SmtpPassword { get; set; } = string.Empty;
    public string ImapHost { get; set; } = string.Empty;
    public int ImapPort { get; set; }
    public bool ImapUseSsl { get; set; }
    public string ImapUsername { get; set; } = string.Empty;
    public string ImapPassword { get; set; } = string.Empty;

    public MailAccount? MailAccount { get; set; }
}
