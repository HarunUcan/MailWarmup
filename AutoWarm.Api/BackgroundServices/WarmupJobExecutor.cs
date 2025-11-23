using System;
using System.Threading;
using System.Threading.Tasks;
using AutoWarm.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AutoWarm.Api.BackgroundServices;

public class WarmupJobExecutor : BackgroundService
{
    private readonly ILogger<WarmupJobExecutor> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public WarmupJobExecutor(ILogger<WarmupJobExecutor> logger, IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("WarmupJobExecutor started");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var engine = scope.ServiceProvider.GetRequiredService<IWarmupEngine>();
                await engine.ExecutePendingJobsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing warmup jobs");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
