using AutoWarm.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System;
using System.Linq;

namespace AutoWarm.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<MailAccount> MailAccounts => Set<MailAccount>();
    public DbSet<GmailAccountDetails> GmailAccountDetails => Set<GmailAccountDetails>();
    public DbSet<SmtpImapAccountDetails> SmtpImapAccountDetails => Set<SmtpImapAccountDetails>();
    public DbSet<WarmupProfile> WarmupProfiles => Set<WarmupProfile>();
    public DbSet<WarmupJob> WarmupJobs => Set<WarmupJob>();
    public DbSet<WarmupEmailLog> WarmupEmailLogs => Set<WarmupEmailLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyUtcDateTimeConverter();

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<MailAccount>()
            .HasOne(a => a.User)
            .WithMany(u => u.MailAccounts)
            .HasForeignKey(a => a.UserId);

        modelBuilder.Entity<MailAccount>()
            .HasOne(a => a.GmailDetails)
            .WithOne(g => g.MailAccount)
            .HasForeignKey<GmailAccountDetails>(g => g.MailAccountId);

        modelBuilder.Entity<MailAccount>()
            .HasOne(a => a.SmtpImapDetails)
            .WithOne(s => s.MailAccount)
            .HasForeignKey<SmtpImapAccountDetails>(s => s.MailAccountId);

        modelBuilder.Entity<GmailAccountDetails>()
            .HasKey(g => g.MailAccountId);

        modelBuilder.Entity<SmtpImapAccountDetails>()
            .HasKey(s => s.MailAccountId);

        modelBuilder.Entity<WarmupProfile>()
            .HasOne(p => p.MailAccount)
            .WithOne(a => a.WarmupProfile)
            .HasForeignKey<WarmupProfile>(p => p.MailAccountId);

        modelBuilder.Entity<WarmupJob>()
            .HasOne(j => j.MailAccount)
            .WithMany(a => a.WarmupJobs)
            .HasForeignKey(j => j.MailAccountId);

        modelBuilder.Entity<WarmupEmailLog>()
            .HasOne(l => l.MailAccount)
            .WithMany(a => a.WarmupEmailLogs)
            .HasForeignKey(l => l.MailAccountId);
    }
}

internal static class ModelBuilderExtensions
{
    public static void ApplyUtcDateTimeConverter(this ModelBuilder modelBuilder)
    {
        var dateTimeConverter = new ValueConverter<DateTime, DateTime>(
            v => v.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(v, DateTimeKind.Utc) : v.ToUniversalTime(),
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

        var nullableDateTimeConverter = new ValueConverter<DateTime?, DateTime?>(
            v => v.HasValue
                ? (v.Value.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v.Value.ToUniversalTime())
                : v,
            v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime))
                {
                    property.SetValueConverter(dateTimeConverter);
                }
                else if (property.ClrType == typeof(DateTime?))
                {
                    property.SetValueConverter(nullableDateTimeConverter);
                }
            }
        }
    }
}
