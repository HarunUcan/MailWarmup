using System;

namespace AutoWarm.Application.DTOs.WarmupProfiles;

public record UpdateWarmupProfileRequest(
    bool IsEnabled,
    int DailyMinEmails,
    int DailyMaxEmails,
    double ReplyRate,
    int MaxDurationDays,
    TimeSpan TimeWindowStart,
    TimeSpan TimeWindowEnd,
    bool UseRandomization);
