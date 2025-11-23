using System;
using System.Collections.Generic;
using AutoWarm.Domain.Entities;
using AutoWarm.Domain.Enums;

namespace AutoWarm.Application.Interfaces;

public interface IWarmupStrategy
{
    IReadOnlyCollection<WarmupJob> GenerateDailyJobs(WarmupProfile profile, DateTime date);
}
