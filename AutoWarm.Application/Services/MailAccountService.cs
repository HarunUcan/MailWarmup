using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoWarm.Application.DTOs.MailAccounts;
using AutoWarm.Application.Interfaces;
using AutoWarm.Domain.Entities;
using AutoWarm.Domain.Enums;

namespace AutoWarm.Application.Services;

public class MailAccountService : IMailAccountService
{
    private readonly IMailAccountRepository _mailAccounts;
    private readonly IUserRepository _users;
    private readonly IGmailOAuthService _gmailOAuth;
    private readonly IMailProviderFactory _mailProviderFactory;
    private readonly IWarmupEmailLogRepository _logs;
    private readonly IUnitOfWork _unitOfWork;

    public MailAccountService(
        IMailAccountRepository mailAccounts,
        IUserRepository users,
        IGmailOAuthService gmailOAuth,
        IMailProviderFactory mailProviderFactory,
        IWarmupEmailLogRepository logs,
        IUnitOfWork unitOfWork)
    {
        _mailAccounts = mailAccounts;
        _users = users;
        _gmailOAuth = gmailOAuth;
        _mailProviderFactory = mailProviderFactory;
        _logs = logs;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyCollection<MailAccountDto>> GetMailAccountsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var accounts = await _mailAccounts.GetForUserAsync(userId, cancellationToken);
        return accounts
            .Select(a => new MailAccountDto(a.Id, a.DisplayName, a.EmailAddress, a.ProviderType, a.Status, a.CreatedAt, a.LastHealthCheckAt))
            .ToArray();
    }

    public async Task<GmailAuthUrlResponse> StartGmailAuthAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            throw new InvalidOperationException("User not found.");
        }

        var (url, state) = await _gmailOAuth.GenerateAuthorizationUrlAsync(userId, user.Email, cancellationToken);
        return new GmailAuthUrlResponse(url, state);
    }

    public async Task<MailAccountDto> CompleteGmailAuthAsync(Guid userId, string code, string state, CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            throw new InvalidOperationException("User not found.");
        }

        var details = await _gmailOAuth.ExchangeCodeAsync(code, state, cancellationToken);
        var account = new MailAccount
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            DisplayName = user.Email,
            EmailAddress = string.IsNullOrWhiteSpace(details.EmailAddress) ? user.Email : details.EmailAddress,
            ProviderType = MailProviderType.Gmail,
            Status = MailAccountStatus.Connected,
            CreatedAt = DateTime.UtcNow,
            GmailDetails = details,
            WarmupProfile = null
        };

        await _mailAccounts.AddAsync(account, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new MailAccountDto(account.Id, account.DisplayName, account.EmailAddress, account.ProviderType, account.Status, account.CreatedAt, account.LastHealthCheckAt);
    }

    public async Task<MailAccountDto> CreateCustomAsync(Guid userId, CreateCustomMailAccountRequest request, CancellationToken cancellationToken = default)
    {
        var account = new MailAccount
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            DisplayName = request.DisplayName,
            EmailAddress = request.EmailAddress,
            ProviderType = MailProviderType.CustomSmtp,
            Status = MailAccountStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            SmtpImapDetails = new SmtpImapAccountDetails
            {
                MailAccountId = Guid.Empty, // will be set by EF via relationship
                SmtpHost = request.SmtpHost,
                SmtpPort = request.SmtpPort,
                SmtpUseSsl = request.SmtpUseSsl,
                SmtpUsername = request.SmtpUsername,
                SmtpPassword = request.SmtpPassword,
                ImapHost = request.ImapHost,
                ImapPort = request.ImapPort,
                ImapUseSsl = request.ImapUseSsl,
                ImapUsername = request.ImapUsername,
                ImapPassword = request.ImapPassword
            }
        };

        // validate credentials quickly to surface configuration issues
        var provider = _mailProviderFactory.Resolve(account);
        var isValid = await provider.ValidateCredentialsAsync(account, cancellationToken);
        account.Status = isValid ? MailAccountStatus.Connected : MailAccountStatus.Pending;

        await _mailAccounts.AddAsync(account, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new MailAccountDto(account.Id, account.DisplayName, account.EmailAddress, account.ProviderType, account.Status, account.CreatedAt, account.LastHealthCheckAt);
    }

    public async Task<string> SendTestAsync(Guid userId, Guid mailAccountId, SendTestMailRequest request, CancellationToken cancellationToken = default)
    {
        var account = await _mailAccounts.GetByIdAsync(mailAccountId, cancellationToken);
        if (account is null || account.UserId != userId)
        {
            throw new InvalidOperationException("Mail account not found.");
        }

        var provider = _mailProviderFactory.Resolve(account);
        var to = string.IsNullOrWhiteSpace(request.To) ? account.EmailAddress : request.To;
        var subject = string.IsNullOrWhiteSpace(request.Subject) ? "AutoWarm test" : request.Subject;
        var body = string.IsNullOrWhiteSpace(request.Body) ? "AutoWarm warmup test email." : request.Body;

        var messageId = await provider.SendEmailAsync(account, to, subject, body, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return messageId;
    }

    public async Task<IReadOnlyCollection<Application.DTOs.Logs.WarmupLogDto>> FetchRecentAsync(Guid userId, Guid mailAccountId, CancellationToken cancellationToken = default)
    {
        var account = await _mailAccounts.GetByIdAsync(mailAccountId, cancellationToken);
        if (account is null || account.UserId != userId)
        {
            throw new InvalidOperationException("Mail account not found.");
        }

        var provider = _mailProviderFactory.Resolve(account);
        var fetched = await provider.FetchRecentEmailsAsync(account, cancellationToken);

        var result = new List<Application.DTOs.Logs.WarmupLogDto>();
        foreach (var log in fetched)
        {
            await _logs.AddAsync(log, cancellationToken);
            result.Add(new Application.DTOs.Logs.WarmupLogDto(
                log.Id,
                log.MailAccountId,
                log.MessageId,
                log.Direction,
                log.Subject,
                log.ToAddress,
                log.FromAddress,
                log.SentAt,
                log.DeliveredAt,
                log.OpenedAt,
                log.MarkedAsImportant,
                log.IsSpam));
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return result;
    }
}
