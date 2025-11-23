using System;
using System.Threading;
using System.Threading.Tasks;
using AutoWarm.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AutoWarm.Api.BackgroundServices;

public class DailyWarmupJobScheduler : BackgroundService
{
    private readonly ILogger<DailyWarmupJobScheduler> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeSpan _targetTime = TimeSpan.FromHours(2);

    public DailyWarmupJobScheduler(ILogger<DailyWarmupJobScheduler> logger, IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("DailyWarmupJobScheduler started");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Generate immediately on startup to avoid waiting for the first window.
                await RunOnce(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate warmup jobs on startup");
            }

            var now = DateTimeOffset.Now;
            var nextRun = now.Date.Add(_targetTime);
            if (now.TimeOfDay > _targetTime)
            {
                nextRun = nextRun.AddDays(1);
            }

            var delay = nextRun - now;
            if (delay < TimeSpan.Zero) delay = TimeSpan.Zero;

            await Task.Delay(delay, stoppingToken);

            try
            {
                await RunOnce(stoppingToken);
                _logger.LogInformation("Warmup jobs generated for {Date}", DateTime.Now.Date);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate warmup jobs");
            }
        }
    }

    private async Task RunOnce(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var engine = scope.ServiceProvider.GetRequiredService<IWarmupEngine>();
        await engine.GenerateDailyJobsAsync(stoppingToken);
    }
}
