using System;
using AutoWarm.Domain.Enums;

namespace AutoWarm.Domain.Entities;

public class WarmupEmailLog
{
    public Guid Id { get; set; }
    public Guid MailAccountId { get; set; }
    public string MessageId { get; set; } = string.Empty;
    public EmailDirection Direction { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string ToAddress { get; set; } = string.Empty;
    public string FromAddress { get; set; } = string.Empty;
    public DateTime? SentAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? OpenedAt { get; set; }
    public bool MarkedAsImportant { get; set; }
    public bool MarkedAsStarred { get; set; }
    public bool IsSpam { get; set; }

    public MailAccount? MailAccount { get; set; }
}
