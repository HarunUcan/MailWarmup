using System;

namespace AutoWarm.Application.DTOs.WarmupProfiles;

public record CreateWarmupProfileRequest(
    Guid MailAccountId,
    bool IsEnabled,
    DateTime StartDate,
    int DailyMinEmails,
    int DailyMaxEmails,
    double ReplyRate,
    int MaxDurationDays,
    TimeSpan TimeWindowStart,
    TimeSpan TimeWindowEnd,
    bool UseRandomization);
