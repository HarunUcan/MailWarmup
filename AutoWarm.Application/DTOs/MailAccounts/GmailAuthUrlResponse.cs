namespace AutoWarm.Application.DTOs.MailAccounts;

public record GmailAuthUrlResponse(string AuthorizationUrl, string State);
