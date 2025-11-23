using System;

namespace AutoWarm.Application.DTOs.WarmupProfiles;

public record WarmupProfileDto(
    Guid Id,
    Guid MailAccountId,
    bool IsEnabled,
    DateTime StartDate,
    int DailyMinEmails,
    int DailyMaxEmails,
    double ReplyRate,
    int MaxDurationDays,
    int CurrentDay,
    TimeSpan TimeWindowStart,
    TimeSpan TimeWindowEnd,
    bool UseRandomization);
