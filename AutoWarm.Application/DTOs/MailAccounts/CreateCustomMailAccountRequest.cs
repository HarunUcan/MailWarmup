namespace AutoWarm.Application.DTOs.MailAccounts;

public record CreateCustomMailAccountRequest(
    string DisplayName,
    string EmailAddress,
    string SmtpHost,
    int SmtpPort,
    bool SmtpUseSsl,
    string SmtpUsername,
    string SmtpPassword,
    string ImapHost,
    int ImapPort,
    bool ImapUseSsl,
    string ImapUsername,
    string ImapPassword);
