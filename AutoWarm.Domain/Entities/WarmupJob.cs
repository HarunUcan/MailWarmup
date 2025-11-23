using System;
using AutoWarm.Domain.Enums;

namespace AutoWarm.Domain.Entities;

public class WarmupJob
{
    public Guid Id { get; set; }
    public Guid MailAccountId { get; set; }
    public DateTime ScheduledAt { get; set; }
    public DateTime? ExecutedAt { get; set; }
    public WarmupJobStatus Status { get; set; } = WarmupJobStatus.Pending;
    public string? ErrorMessage { get; set; }
    public WarmupJobType Type { get; set; }

    public MailAccount? MailAccount { get; set; }
}
