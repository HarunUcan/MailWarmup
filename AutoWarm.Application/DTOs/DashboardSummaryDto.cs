namespace AutoWarm.Application.DTOs;

public record DashboardSummaryDto(
    int ActiveAccounts,
    int DailySentEmails,
    int DailyReplies,
    int WarmupJobsPending);
