using System;
using System.Collections.Generic;
using AutoWarm.Application.Interfaces;
using AutoWarm.Domain.Entities;
using AutoWarm.Domain.Enums;

namespace AutoWarm.Application.Strategies;

public class LinearWarmupStrategy : IWarmupStrategy
{
    public IReadOnlyCollection<WarmupJob> GenerateDailyJobs(WarmupProfile profile, DateTime date)
    {
        var jobs = new List<WarmupJob>();
        var totalEmails = profile.DailyMinEmails;
        if (profile.UseRandomization)
        {
            var random = new Random();
            totalEmails = random.Next(profile.DailyMinEmails, profile.DailyMaxEmails + 1);
        }
        else
        {
            totalEmails = profile.DailyMaxEmails;
        }

        if (totalEmails <= 0)
        {
            return jobs;
        }

        var baseDateLocal = DateTime.SpecifyKind(date.Date, DateTimeKind.Local);
        var windowStartLocal = baseDateLocal.Add(profile.TimeWindowStart);
        var windowEndLocal = baseDateLocal.Add(profile.TimeWindowEnd);
        var windowMinutes = Math.Max((int)(windowEndLocal - windowStartLocal).TotalMinutes, 1);
        var step = windowMinutes / totalEmails;

        for (var i = 0; i < totalEmails; i++)
        {
            var scheduledLocal = windowStartLocal.AddMinutes(step * i);
            jobs.Add(new WarmupJob
            {
                Id = Guid.NewGuid(),
                MailAccountId = profile.MailAccountId,
                ScheduledAt = DateTime.SpecifyKind(scheduledLocal, DateTimeKind.Local).ToUniversalTime(),
                Type = i % 3 == 0 ? WarmupJobType.ReplyEmail : WarmupJobType.SendEmail,
                Status = WarmupJobStatus.Pending
            });
        }

        return jobs;
    }
}
