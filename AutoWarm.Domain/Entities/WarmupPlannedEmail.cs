using System;

namespace AutoWarm.Domain.Entities;

public class WarmupPlannedEmail
{
    public Guid Id { get; set; }
    public Guid? SenderMailAccountId { get; set; }
    public Guid? TargetMailAccountId { get; set; }
    public string TargetAddress { get; set; } = string.Empty;
    public string InternetMessageId { get; set; } = string.Empty;
    public bool MarkRead { get; set; }
    public bool SendReply { get; set; }
    public bool MarkImportant { get; set; }
    public bool AddStar { get; set; }
    public bool Archive { get; set; }
    public bool Delete { get; set; }
    public bool RescueFromSpam { get; set; }
    public int ImportantStarGraceLimit { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? AppliedAt { get; set; }
}
