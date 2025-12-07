using System;

namespace AutoWarm.Domain.Models;

public sealed class WarmupActionPlan
{
    public bool MarkRead { get; init; }
    public bool SendReply { get; init; }
    public bool MarkImportant { get; init; }
    public bool AddStar { get; init; }
    public bool Archive { get; init; }
    public bool Delete { get; init; }
    public bool RescueFromSpam { get; init; }
    public int ImportantStarGraceLimit { get; init; }

    public static WarmupActionPlan Generate()
    {
        var reply = NextBool(0.20, 0.40);
        var importantProb = NextProbability(0.05, 0.15);
        if (reply)
        {
            importantProb = Math.Min(1.0, importantProb + 0.20);
        }

        return new WarmupActionPlan
        {
            MarkRead = NextBool(0.50, 0.70),
            SendReply = reply,
            MarkImportant = Random.Shared.NextDouble() < importantProb,
            AddStar = NextBool(0.03, 0.07),
            Archive = NextBool(0.10, 0.25),
            Delete = NextBool(0.05, 0.15),
            RescueFromSpam = NextBool(0.60, 0.90),
            ImportantStarGraceLimit = Random.Shared.Next(5, 11) // inclusive 5-10
        };
    }

    private static bool NextBool(double min, double max) => Random.Shared.NextDouble() < NextProbability(min, max);

    private static double NextProbability(double minInclusive, double maxInclusive)
    {
        var range = maxInclusive - minInclusive;
        return minInclusive + range * Random.Shared.NextDouble();
    }
}
