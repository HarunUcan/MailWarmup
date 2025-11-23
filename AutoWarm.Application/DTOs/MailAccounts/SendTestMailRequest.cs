namespace AutoWarm.Application.DTOs.MailAccounts;

public record SendTestMailRequest(string To, string Subject, string Body);
