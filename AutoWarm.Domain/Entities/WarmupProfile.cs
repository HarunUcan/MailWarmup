using System;

namespace AutoWarm.Domain.Entities;

public class WarmupProfile
{
    public Guid Id { get; set; }
    public Guid MailAccountId { get; set; }
    public bool IsEnabled { get; set; }
    public DateTime StartDate { get; set; } = DateTime.Now.Date;
    public int DailyMinEmails { get; set; }
    public int DailyMaxEmails { get; set; }
    public double ReplyRate { get; set; }
    public int MaxDurationDays { get; set; }
    public int CurrentDay { get; set; }
    public TimeSpan TimeWindowStart { get; set; }
    public TimeSpan TimeWindowEnd { get; set; }
    public bool UseRandomization { get; set; }

    public MailAccount? MailAccount { get; set; }
}
