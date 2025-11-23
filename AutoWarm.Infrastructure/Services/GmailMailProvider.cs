using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AutoWarm.Domain.Entities;
using AutoWarm.Domain.Enums;
using AutoWarm.Domain.Interfaces;
using AutoWarm.Infrastructure.Settings;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using FileDataStore = Google.Apis.Util.Store.FileDataStore;

namespace AutoWarm.Infrastructure.Services;

public class GmailMailProvider : IMailProvider
{
    private readonly GmailOAuthOptions _options;
    private readonly ILogger<GmailMailProvider> _logger;

    public GmailMailProvider(IOptions<GmailOAuthOptions> options, ILogger<GmailMailProvider> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<bool> ValidateCredentialsAsync(MailAccount account, CancellationToken cancellationToken = default)
    {
        try
        {
            _ = await CreateServiceAsync(account, cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Gmail validation failed for account {Account}", account.EmailAddress);
            return false;
        }
    }

    public async Task<string> SendEmailAsync(MailAccount account, string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        var service = await CreateServiceAsync(account, cancellationToken);

        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(account.EmailAddress));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body = new TextPart("plain") { Text = body };

        var raw = Base64UrlEncode(Encoding.UTF8.GetBytes(message.ToString()));
        var gmailMessage = new Message { Raw = raw };
        var result = await service.Users.Messages.Send(gmailMessage, "me").ExecuteAsync(cancellationToken);
        return result.Id;
    }

    public async Task<IReadOnlyCollection<WarmupEmailLog>> FetchRecentEmailsAsync(MailAccount account, CancellationToken cancellationToken = default)
    {
        var service = await CreateServiceAsync(account, cancellationToken);
        var listRequest = service.Users.Messages.List("me");
        listRequest.LabelIds = "INBOX";
        listRequest.MaxResults = 10;
        listRequest.IncludeSpamTrash = true;

        var list = await listRequest.ExecuteAsync(cancellationToken);
        if (list.Messages == null || list.Messages.Count == 0)
        {
            return Array.Empty<WarmupEmailLog>();
        }

        var logs = new List<WarmupEmailLog>();
        foreach (var m in list.Messages)
        {
            var msg = await service.Users.Messages.Get("me", m.Id).ExecuteAsync(cancellationToken);
            var headers = msg.Payload?.Headers ?? new List<MessagePartHeader>();
            var subject = headers.FirstOrDefault(h => h.Name.Equals("Subject", StringComparison.OrdinalIgnoreCase))?.Value ?? "(no subject)";
            var from = headers.FirstOrDefault(h => h.Name.Equals("From", StringComparison.OrdinalIgnoreCase))?.Value ?? string.Empty;
            var to = headers.FirstOrDefault(h => h.Name.Equals("To", StringComparison.OrdinalIgnoreCase))?.Value ?? account.EmailAddress;
            var internalDate = msg.InternalDate.HasValue
                ? DateTimeOffset.FromUnixTimeMilliseconds((long)msg.InternalDate.Value).UtcDateTime
                : (DateTime?)null;

            logs.Add(new WarmupEmailLog
            {
                Id = Guid.NewGuid(),
                MailAccountId = account.Id,
                MessageId = msg.Id,
                Direction = EmailDirection.Received,
                Subject = subject,
                ToAddress = to,
                FromAddress = from,
                SentAt = internalDate,
                DeliveredAt = internalDate,
                MarkedAsImportant = msg.LabelIds?.Contains("IMPORTANT") == true,
                IsSpam = msg.LabelIds?.Contains("SPAM") == true
            });
        }

        return logs;
    }

    public async Task MarkAsImportantAsync(MailAccount account, string messageId, CancellationToken cancellationToken = default)
    {
        var service = await CreateServiceAsync(account, cancellationToken);
        var modify = new ModifyMessageRequest
        {
            AddLabelIds = new[] { "IMPORTANT" }
        };
        await service.Users.Messages.Modify(modify, "me", messageId).ExecuteAsync(cancellationToken);
    }

    public async Task MoveToInboxAsync(MailAccount account, string messageId, CancellationToken cancellationToken = default)
    {
        var service = await CreateServiceAsync(account, cancellationToken);
        var modify = new ModifyMessageRequest
        {
            AddLabelIds = new[] { "INBOX" },
            RemoveLabelIds = new[] { "SPAM" }
        };
        await service.Users.Messages.Modify(modify, "me", messageId).ExecuteAsync(cancellationToken);
    }

    private async Task<GmailService> CreateServiceAsync(MailAccount account, CancellationToken cancellationToken)
    {
        if (account.GmailDetails is null)
        {
            throw new InvalidOperationException("Gmail details are missing for this account.");
        }

        var clientSecrets = new ClientSecrets
        {
            ClientId = _options.ClientId,
            ClientSecret = _options.ClientSecret
        };

        var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = clientSecrets,
            Scopes = new[] { _options.Scopes },
            DataStore = new FileDataStore("AutoWarm.GmailTokens")
        });

        var expiresInSeconds = (long)Math.Max((account.GmailDetails.TokenExpiresAt - DateTime.UtcNow).TotalSeconds, 0);
        var issuedUtc = account.GmailDetails.TokenExpiresAt.AddSeconds(-expiresInSeconds);
        if (issuedUtc < DateTime.SpecifyKind(DateTime.MinValue.AddDays(1), DateTimeKind.Utc))
        {
            issuedUtc = DateTime.UtcNow;
        }

        var token = new Google.Apis.Auth.OAuth2.Responses.TokenResponse
        {
            AccessToken = account.GmailDetails.AccessToken,
            RefreshToken = account.GmailDetails.RefreshToken,
            ExpiresInSeconds = expiresInSeconds,
            IssuedUtc = issuedUtc
        };

        var credential = new UserCredential(flow, account.GmailDetails.GoogleUserId ?? account.EmailAddress, token);
        if (credential.Token.IsStale)
        {
            var refreshed = await credential.RefreshTokenAsync(cancellationToken);
            if (!refreshed)
            {
                throw new InvalidOperationException("Failed to refresh Gmail access token.");
            }

            account.GmailDetails.AccessToken = credential.Token.AccessToken;
            if (!string.IsNullOrEmpty(credential.Token.RefreshToken))
            {
                account.GmailDetails.RefreshToken = credential.Token.RefreshToken;
            }

            var seconds = credential.Token.ExpiresInSeconds ?? 3600;
            account.GmailDetails.TokenExpiresAt = DateTime.UtcNow.AddSeconds(seconds);
            credential.Token.IssuedUtc = DateTime.UtcNow;
        }

        var service = new GmailService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "AutoWarm"
        });

        return service;
    }

    private static string Base64UrlEncode(byte[] input)
    {
        var s = Convert.ToBase64String(input);
        s = s.Replace('+', '-').Replace('/', '_').TrimEnd('=');
        return s;
    }
}
