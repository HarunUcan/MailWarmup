using System;
using System.Threading;
using System.Threading.Tasks;
using AutoWarm.Domain.Entities;

namespace AutoWarm.Application.Interfaces;

public interface IGmailOAuthService
{
    Task<(string authorizationUrl, string state)> GenerateAuthorizationUrlAsync(Guid userId, string userEmail, CancellationToken cancellationToken = default);
    Task<GmailAccountDetails> ExchangeCodeAsync(string code, string state, CancellationToken cancellationToken = default);
}
