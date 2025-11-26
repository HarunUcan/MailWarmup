using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoWarm.Domain.Entities;
using AutoWarm.Domain.Enums;
using AutoWarm.Domain.Interfaces;
using MailKit.Net.Imap;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace AutoWarm.Infrastructure.Services;

public class SmtpImapMailProvider : IMailProvider
{
    public async Task<bool> ValidateCredentialsAsync(MailAccount account, CancellationToken cancellationToken = default)
    {
        if (account.SmtpImapDetails is null)
        {
            return false;
        }

        try
        {
            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(account.SmtpImapDetails.SmtpHost, account.SmtpImapDetails.SmtpPort, account.SmtpImapDetails.SmtpUseSsl, cancellationToken);
            await smtp.AuthenticateAsync(account.SmtpImapDetails.SmtpUsername, account.SmtpImapDetails.SmtpPassword, cancellationToken);
            await smtp.DisconnectAsync(true, cancellationToken);

            using var imap = new ImapClient();
            await imap.ConnectAsync(account.SmtpImapDetails.ImapHost, account.SmtpImapDetails.ImapPort, account.SmtpImapDetails.ImapUseSsl, cancellationToken);
            await imap.AuthenticateAsync(account.SmtpImapDetails.ImapUsername, account.SmtpImapDetails.ImapPassword, cancellationToken);
            await imap.DisconnectAsync(true, cancellationToken);
        }
        catch
        {
            return false;
        }

        return true;
    }

    public async Task<string> SendEmailAsync(MailAccount account, string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        if (account.SmtpImapDetails is null)
        {
            throw new InvalidOperationException("SMTP/IMAP details missing.");
        }

        var warmupId = $"AutoWarm-{Guid.NewGuid():N}";
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(account.DisplayName, account.EmailAddress));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body = new TextPart("plain") { Text = body };
        message.Headers.Add("X-AutoWarm-Id", warmupId);

        using var client = new SmtpClient();
        await client.ConnectAsync(account.SmtpImapDetails.SmtpHost, account.SmtpImapDetails.SmtpPort, account.SmtpImapDetails.SmtpUseSsl, cancellationToken);
        await client.AuthenticateAsync(account.SmtpImapDetails.SmtpUsername, account.SmtpImapDetails.SmtpPassword, cancellationToken);
        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);

        return message.MessageId ?? Guid.NewGuid().ToString("N");
    }

    public async Task<IReadOnlyCollection<WarmupEmailLog>> FetchRecentEmailsAsync(MailAccount account, CancellationToken cancellationToken = default)
    {
        // For the starter template we keep this simple; implement full IMAP fetch later.
        return Array.Empty<WarmupEmailLog>();
    }

    public Task MarkAsImportantAsync(MailAccount account, string messageId, CancellationToken cancellationToken = default)
    {
        // IMAP flagging could be added here
        return Task.CompletedTask;
    }

    public Task MoveToInboxAsync(MailAccount account, string messageId, CancellationToken cancellationToken = default)
    {
        // IMAP move logic would be implemented here
        return Task.CompletedTask;
    }
}
