using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoWarm.Application.Interfaces;
using AutoWarm.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AutoWarm.Api.BackgroundServices;

/// <summary>
/// Periodically scans warmup accounts' inboxes to rescue warmup mails from spam and persist logs.
/// </summary>
public class WarmupInboxMonitor : BackgroundService
{
    private readonly ILogger<WarmupInboxMonitor> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(5);

    public WarmupInboxMonitor(ILogger<WarmupInboxMonitor> logger, IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("WarmupInboxMonitor started");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ScanAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "WarmupInboxMonitor failed");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }

    private async Task ScanAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var profiles = scope.ServiceProvider.GetRequiredService<IWarmupProfileRepository>();
        var providerFactory = scope.ServiceProvider.GetRequiredService<IMailProviderFactory>();
        var logRepo = scope.ServiceProvider.GetRequiredService<IWarmupEmailLogRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var activeProfiles = await profiles.GetActiveProfilesAsync(cancellationToken);
        var accounts = activeProfiles
            .Select(p => p.MailAccount)
            .Where(a => a is not null && a.Status == MailAccountStatus.Connected)
            .Select(a => a!)
            .ToList();

        foreach (var account in accounts)
        {
            try
            {
                var provider = providerFactory.Resolve(account);
                var fetched = await provider.FetchRecentEmailsAsync(account, cancellationToken);
                foreach (var log in fetched)
                {
                    if (await logRepo.ExistsAsync(log.MailAccountId, log.MessageId, cancellationToken))
                    {
                        continue;
                    }

                    await logRepo.AddAsync(log, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to scan inbox for account {Account}", account.EmailAddress);
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
