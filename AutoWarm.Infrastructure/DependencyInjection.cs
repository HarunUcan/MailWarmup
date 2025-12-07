using AutoWarm.Application.Interfaces;
using AutoWarm.Application.Services;
using AutoWarm.Application.Strategies;
using AutoWarm.Infrastructure.Persistence;
using AutoWarm.Infrastructure.Persistence.Repositories;
using AutoWarm.Infrastructure.Services;
using AutoWarm.Infrastructure.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AutoWarm.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<GmailOAuthOptions>(configuration.GetSection(GmailOAuthOptions.SectionName));

        var connectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<AppDbContext>(options =>
        {
            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                options.UseSqlServer(connectionString);
            }
            else
            {
                options.UseInMemoryDatabase("AutoWarmDb");
            }
        });

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IMailAccountRepository, MailAccountRepository>();
        services.AddScoped<IWarmupProfileRepository, WarmupProfileRepository>();
        services.AddScoped<IWarmupJobRepository, WarmupJobRepository>();
        services.AddScoped<IWarmupEmailLogRepository, WarmupEmailLogRepository>();
        services.AddScoped<IWarmupPlannedEmailRepository, WarmupPlannedEmailRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IGmailOAuthService, GmailOAuthService>();
        services.AddScoped<IMailProviderFactory, MailProviderFactory>();
        services.AddScoped<IDnsHealthChecker, DnsHealthChecker>();
        services.AddScoped<GmailMailProvider>();
        services.AddScoped<SmtpImapMailProvider>();

        return services;
    }
}
