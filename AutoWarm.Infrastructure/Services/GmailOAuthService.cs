using System;
using System.Threading;
using System.Threading.Tasks;
using AutoWarm.Application.Interfaces;
using AutoWarm.Domain.Entities;
using AutoWarm.Infrastructure.Settings;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Gmail.v1;
using Google.Apis.Util.Store;
using Microsoft.Extensions.Options;

namespace AutoWarm.Infrastructure.Services;

public class GmailOAuthService : IGmailOAuthService
{
    private readonly GmailOAuthOptions _options;

    public GmailOAuthService(IOptions<GmailOAuthOptions> options)
    {
        _options = options.Value;
    }

    public Task<(string authorizationUrl, string state)> GenerateAuthorizationUrlAsync(Guid userId, string userEmail, CancellationToken cancellationToken = default)
    {
        var clientSecrets = new ClientSecrets
        {
            ClientId = _options.ClientId,
            ClientSecret = _options.ClientSecret
        };

        var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = clientSecrets,
            Scopes = new[] { _options.Scopes }
        });

        var state = $"{userId}:{Guid.NewGuid():N}";
        var request = flow.CreateAuthorizationCodeRequest(_options.RedirectUri);
        request.State = state;
        var url = request.Build();

        return Task.FromResult((url.AbsoluteUri, state));
    }

    public async Task<GmailAccountDetails> ExchangeCodeAsync(string code, string state, CancellationToken cancellationToken = default)
    {
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

        var token = await flow.ExchangeCodeForTokenAsync(
            "auto",
            code,
            _options.RedirectUri,
            cancellationToken);

        var issuedAt = token.IssuedUtc;
        if (issuedAt == default)
        {
            issuedAt = DateTime.UtcNow;
        }

        var details = new GmailAccountDetails
        {
            MailAccountId = Guid.Empty, // set by EF via relationship
            AccessToken = token.AccessToken,
            RefreshToken = token.RefreshToken,
            TokenExpiresAt = issuedAt.AddSeconds(token.ExpiresInSeconds ?? 3600),
            Scopes = _options.Scopes,
            GoogleUserId = "unknown",
            EmailAddress = string.Empty
        };

        try
        {
            var credential = new UserCredential(flow, "me", token);
            var gmail = new GmailService(new Google.Apis.Services.BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "AutoWarm"
            });

            var profile = await gmail.Users.GetProfile("me").ExecuteAsync(cancellationToken);
            details.GoogleUserId = profile.EmailAddress;
            details.EmailAddress = profile.EmailAddress;
        }
        catch
        {
            // ignore lookup errors; will be set during provider use if needed
        }

        return details;
    }
}
