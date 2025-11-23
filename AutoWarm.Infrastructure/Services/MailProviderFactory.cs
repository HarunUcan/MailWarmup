using System;
using AutoWarm.Application.Interfaces;
using AutoWarm.Domain.Entities;
using AutoWarm.Domain.Enums;
using AutoWarm.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace AutoWarm.Infrastructure.Services;

public class MailProviderFactory : IMailProviderFactory
{
    private readonly IServiceProvider _serviceProvider;

    public MailProviderFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IMailProvider Resolve(MailAccount account)
    {
        return account.ProviderType switch
        {
            MailProviderType.Gmail => _serviceProvider.GetRequiredService<GmailMailProvider>(),
            MailProviderType.CustomSmtp => _serviceProvider.GetRequiredService<SmtpImapMailProvider>(),
            _ => throw new InvalidOperationException("Unsupported provider")
        };
    }
}
