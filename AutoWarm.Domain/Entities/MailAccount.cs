using System;
using AutoWarm.Domain.Enums;

namespace AutoWarm.Domain.Entities;

public class MailAccount
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string EmailAddress { get; set; } = string.Empty;
    public MailProviderType ProviderType { get; set; }
    public MailAccountStatus Status { get; set; } = MailAccountStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastHealthCheckAt { get; set; }

    public User? User { get; set; }
    public GmailAccountDetails? GmailDetails { get; set; }
    public SmtpImapAccountDetails? SmtpImapDetails { get; set; }
    public WarmupProfile? WarmupProfile { get; set; }
    public ICollection<WarmupJob> WarmupJobs { get; set; } = new List<WarmupJob>();
    public ICollection<WarmupEmailLog> WarmupEmailLogs { get; set; } = new List<WarmupEmailLog>();
}
