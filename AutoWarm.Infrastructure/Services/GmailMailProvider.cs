using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AutoWarm.Application.Interfaces;
using AutoWarm.Domain.Entities;
using AutoWarm.Domain.Enums;
using AutoWarm.Domain.Interfaces;
using AutoWarm.Domain.Models;
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
    private readonly IWarmupEmailLogRepository _logRepository;

    public GmailMailProvider(
        IOptions<GmailOAuthOptions> options,
        ILogger<GmailMailProvider> logger,
        IWarmupEmailLogRepository logRepository)
    {
        _options = options.Value;
        _logger = logger;
        _logRepository = logRepository;
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

        // Add a unique id so we can later detect our own messages in spam.
        var warmupId = $"AutoWarm-{Guid.NewGuid():N}";
        var plan = WarmupActionPlan.Generate();
        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(account.EmailAddress));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body = new TextPart("plain") { Text = body };
        message.Headers.Add("X-AutoWarm-Id", warmupId);
        message.Headers.Add("X-AutoWarm-Plan", plan.ToHeaderValue());

        var raw = Base64UrlEncode(Encoding.UTF8.GetBytes(message.ToString()));
        var gmailMessage = new Message { Raw = raw };
        var result = await service.Users.Messages.Send(gmailMessage, "me").ExecuteAsync(cancellationToken);
        return result.Id;
    }

    public async Task<IReadOnlyCollection<WarmupEmailLog>> FetchRecentEmailsAsync(MailAccount account, CancellationToken cancellationToken = default)
    {
        var service = await CreateServiceAsync(account, cancellationToken);
        var logs = new List<WarmupEmailLog>();
        var processed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var warmupContext = await BuildWarmupActionContextAsync(account.Id, cancellationToken);

        // Process spam first so warmup mails are rescued quickly, then inbox.
        await AddLogsForLabelAsync(service, account, logs, processed, "SPAM", warmupContext, cancellationToken);
        await AddLogsForLabelAsync(service, account, logs, processed, "INBOX", warmupContext, cancellationToken);

        return logs;
    }

    private async Task AddLogsForLabelAsync(
        GmailService service,
        MailAccount account,
        List<WarmupEmailLog> logs,
        HashSet<string> processed,
        string labelId,
        WarmupActionContext warmupContext,
        CancellationToken cancellationToken)
    {
        var listRequest = service.Users.Messages.List("me");
        listRequest.LabelIds = labelId;
        listRequest.MaxResults = 10;
        listRequest.IncludeSpamTrash = true;

        var list = await listRequest.ExecuteAsync(cancellationToken);
        if (list.Messages == null || list.Messages.Count == 0)
        {
            return;
        }

        foreach (var m in list.Messages)
        {
            if (!processed.Add(m.Id))
            {
                continue;
            }

            var msg = await service.Users.Messages.Get("me", m.Id).ExecuteAsync(cancellationToken);
            var headers = msg.Payload?.Headers ?? new List<MessagePartHeader>();
            var subject = headers.FirstOrDefault(h => h.Name.Equals("Subject", StringComparison.OrdinalIgnoreCase))?.Value ?? "(no subject)";
            var from = headers.FirstOrDefault(h => h.Name.Equals("From", StringComparison.OrdinalIgnoreCase))?.Value ?? string.Empty;
            var to = headers.FirstOrDefault(h => h.Name.Equals("To", StringComparison.OrdinalIgnoreCase))?.Value ?? account.EmailAddress;
            var autoWarmId = headers.FirstOrDefault(h => h.Name.Equals("X-AutoWarm-Id", StringComparison.OrdinalIgnoreCase))?.Value;
            var planHeader = headers.FirstOrDefault(h => h.Name.Equals("X-AutoWarm-Plan", StringComparison.OrdinalIgnoreCase))?.Value;
            var originalMessageId = headers.FirstOrDefault(h => h.Name.Equals("Message-ID", StringComparison.OrdinalIgnoreCase))?.Value;
            var internalDate = msg.InternalDate.HasValue
                ? DateTimeOffset.FromUnixTimeMilliseconds((long)msg.InternalDate.Value).UtcDateTime
                : (DateTime?)null;

            var isSpam = msg.LabelIds?.Contains("SPAM") == true;
            var isStarred = msg.LabelIds?.Contains("STARRED") == true;
            var isImportant = msg.LabelIds?.Contains("IMPORTANT") == true;
            var isWarmup = !string.IsNullOrWhiteSpace(autoWarmId);
            var isUnread = msg.LabelIds?.Contains("UNREAD") == true;
            DateTime? openedAt = null;

            if (await _logRepository.ExistsAsync(account.Id, msg.Id, cancellationToken))
            {
                continue;
            }

            WarmupActionPlan? plan = null;
            if (WarmupActionPlan.TryParse(planHeader, out var parsedPlan))
            {
                plan = parsedPlan;
            }

            var log = new WarmupEmailLog
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
                MarkedAsImportant = isImportant,
                MarkedAsStarred = isStarred,
                IsWarmup = isWarmup,
                WarmupId = autoWarmId,
                IsSpam = isSpam,
                OpenedAt = openedAt
            };
            logs.Add(log);

            // Only handle our own warmup mails: apply planned actions once, guided by headers.
            if (isWarmup && plan is not null)
            {
                var withinGrace = warmupContext.WarmupCount < plan.ImportantStarGraceLimit;
                var markRead = plan.MarkRead;
                var markImportant = plan.MarkImportant && !withinGrace;
                var addStar = plan.AddStar && !withinGrace;
                var archive = plan.Archive;
                var delete = plan.Delete;
                var rescueFromSpam = plan.RescueFromSpam && isSpam;
                var sendReply = plan.SendReply;
                var addLabelIds = new List<string>();
                var removeLabelIds = new List<string>();

                // Early delete overrides all other label adjustments.
                if (delete)
                {
                    try
                    {
                        await service.Users.Messages.Trash("me", msg.Id).ExecuteAsync(cancellationToken);
                        log.MarkedAsImportant = false;
                        log.MarkedAsStarred = false;
                        log.IsSpam = false;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete warmup mail {Message} for {Account}", msg.Id, account.EmailAddress);
                    }

                    warmupContext.WarmupCount++;
                    continue;
                }

                if (addStar && !isStarred)
                {
                    addLabelIds.Add("STARRED");
                    log.MarkedAsStarred = true;
                }

                if (markImportant && !isImportant)
                {
                    addLabelIds.Add("IMPORTANT");
                    log.MarkedAsImportant = true;
                }

                if (rescueFromSpam && isSpam)
                {
                    removeLabelIds.Add("SPAM");
                    log.IsSpam = false;
                }

                // Archive by removing INBOX; do not add INBOX so it skips the user's inbox view.
                if (archive && msg.LabelIds?.Contains("INBOX") == true)
                {
                    removeLabelIds.Add("INBOX");
                }

                if (markRead && isUnread)
                {
                    removeLabelIds.Add("UNREAD");
                    openedAt = DateTime.UtcNow;
                }

                var modify = new ModifyMessageRequest();
                if (addLabelIds.Count > 0)
                {
                    modify.AddLabelIds = addLabelIds;
                }

                if (removeLabelIds.Count > 0)
                {
                    modify.RemoveLabelIds = removeLabelIds;
                }

                if ((modify.AddLabelIds?.Any() == true) || (modify.RemoveLabelIds?.Any() == true))
                {
                    await service.Users.Messages.Modify(modify, "me", msg.Id).ExecuteAsync(cancellationToken);

                    var hadInbox = msg.LabelIds?.Contains("INBOX") == true;
                    await CleanRemainingLabelsAsync(service, msg.Id, isSpam, isUnread, hadInbox, modify.RemoveLabelIds, cancellationToken);
                }

                if (sendReply)
                {
                    await TryReplyAsync(service, account, msg, subject, from, autoWarmId, originalMessageId, cancellationToken);
                }

                if (openedAt.HasValue)
                {
                    log.OpenedAt = openedAt;
                }

                warmupContext.WarmupCount++;
            }
        }
    }

    private async Task<WarmupActionContext> BuildWarmupActionContextAsync(Guid accountId, CancellationToken cancellationToken)
    {
        var count = await _logRepository.CountWarmupReceivedAsync(accountId, cancellationToken);
        return new WarmupActionContext(count);
    }

    private async Task TryReplyAsync(
        GmailService service,
        MailAccount account,
        Message originalMessage,
        string subject,
        string fromAddress,
        string? warmupId,
        string? originalMessageId,
        CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(fromAddress) ||
                fromAddress.Contains(account.EmailAddress, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var replySubject = subject.StartsWith("Re:", StringComparison.OrdinalIgnoreCase) ? subject : $"Re: {subject}";

            var reply = new MimeMessage();
            reply.From.Add(MailboxAddress.Parse(account.EmailAddress));
            reply.To.Add(MailboxAddress.Parse(fromAddress));
            reply.Subject = replySubject;
            reply.Body = new TextPart("plain")
            {
                Text = "Thanks for your email, noted."
            };
            reply.Headers.Add("X-AutoWarm-Plan", WarmupActionPlan.Generate().ToHeaderValue());

            if (!string.IsNullOrWhiteSpace(originalMessageId))
            {
                reply.InReplyTo = originalMessageId;
                reply.Headers.Add("References", originalMessageId);
            }

            if (!string.IsNullOrWhiteSpace(warmupId))
            {
                reply.Headers.Add("X-AutoWarm-Id", warmupId);
            }

            var raw = Base64UrlEncode(Encoding.UTF8.GetBytes(reply.ToString()));
            var sendRequest = new Message
            {
                Raw = raw,
                ThreadId = originalMessage.ThreadId
            };

            await service.Users.Messages.Send(sendRequest, "me").ExecuteAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to auto-reply to warmup mail for {Account}", account.EmailAddress);
        }
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

    private static async Task CleanRemainingLabelsAsync(
        GmailService service,
        string messageId,
        bool wasSpam,
        bool wasUnread,
        bool hadInbox,
        IList<string>? removeLabelIds,
        CancellationToken cancellationToken)
    {
        if (wasSpam && (removeLabelIds?.Contains("SPAM") == true))
        {
            var cleanSpam = new ModifyMessageRequest { RemoveLabelIds = new[] { "SPAM" } };
            await service.Users.Messages.Modify(cleanSpam, "me", messageId).ExecuteAsync(cancellationToken);
        }

        if (wasUnread && (removeLabelIds?.Contains("UNREAD") == true))
        {
            var cleanUnread = new ModifyMessageRequest { RemoveLabelIds = new[] { "UNREAD" } };
            await service.Users.Messages.Modify(cleanUnread, "me", messageId).ExecuteAsync(cancellationToken);
        }

        if (hadInbox && (removeLabelIds?.Contains("INBOX") == true))
        {
            var cleanInbox = new ModifyMessageRequest { RemoveLabelIds = new[] { "INBOX" } };
            await service.Users.Messages.Modify(cleanInbox, "me", messageId).ExecuteAsync(cancellationToken);
        }
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

    private sealed class WarmupActionContext
    {
        public WarmupActionContext(int warmupCount)
        {
            WarmupCount = warmupCount;
        }

        public int WarmupCount { get; set; }
    }
}
