using AutoWarm.Application.Interfaces;
using AutoWarm.Application.Security;
using AutoWarm.Application.Services;
using AutoWarm.Application.Strategies;
using Microsoft.Extensions.DependencyInjection;

namespace AutoWarm.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IMailAccountService, MailAccountService>();
        services.AddScoped<IWarmupProfileService, WarmupProfileService>();
        services.AddScoped<IWarmupJobService, WarmupJobService>();
        services.AddScoped<IEmailOptimizationService, EmailOptimizationService>();
        services.AddScoped<IWarmupStrategy, LinearWarmupStrategy>();
        services.AddScoped<IWarmupEngine, WarmupEngine>();
        services.AddSingleton<IWarmupInboxRescueQueue, WarmupInboxRescueQueue>();
        return services;
    }
}
