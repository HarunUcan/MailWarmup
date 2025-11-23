using System;
using System.Collections.Generic;

namespace AutoWarm.Application.DTOs;

public record ReputationScoreDto(
    Guid MailAccountId,
    string EmailAddress,
    double Score,
    string Label,
    IReadOnlyCollection<double> Trend);
