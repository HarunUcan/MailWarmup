using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoWarm.Application.DTOs.MailAccounts;

namespace AutoWarm.Application.Interfaces;

public interface IMailAccountService
{
    Task<IReadOnlyCollection<MailAccountDto>> GetMailAccountsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<GmailAuthUrlResponse> StartGmailAuthAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<MailAccountDto> CompleteGmailAuthAsync(Guid userId, string code, string state, CancellationToken cancellationToken = default);
    Task<MailAccountDto> CreateCustomAsync(Guid userId, CreateCustomMailAccountRequest request, CancellationToken cancellationToken = default);
    Task<string> SendTestAsync(Guid userId, Guid mailAccountId, SendTestMailRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<DTOs.Logs.WarmupLogDto>> FetchRecentAsync(Guid userId, Guid mailAccountId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<DnsCheckDto>> GetDnsChecksAsync(Guid userId, CancellationToken cancellationToken = default);
}
