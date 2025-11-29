using System;
using System.Globalization;

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

    public string ToHeaderValue()
    {
        return $"v=1;r={(MarkRead ? 1 : 0)};rep={(SendReply ? 1 : 0)};imp={(MarkImportant ? 1 : 0)};star={(AddStar ? 1 : 0)};arc={(Archive ? 1 : 0)};del={(Delete ? 1 : 0)};res={(RescueFromSpam ? 1 : 0)};gr={ImportantStarGraceLimit}";
    }

    public static bool TryParse(string? value, out WarmupActionPlan plan)
    {
        plan = default!;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        try
        {
            var parts = value.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length == 0)
            {
                return false;
            }

            var dict = new System.Collections.Generic.Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var part in parts)
            {
                var kv = part.Split('=', 2, StringSplitOptions.TrimEntries);
                if (kv.Length == 2)
                {
                    dict[kv[0]] = kv[1];
                }
            }

            bool ParseBool(string key) => dict.TryGetValue(key, out var v) && v == "1";
            int ParseInt(string key, int fallback) =>
                dict.TryGetValue(key, out var v) && int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n)
                    ? n
                    : fallback;

            plan = new WarmupActionPlan
            {
                MarkRead = ParseBool("r"),
                SendReply = ParseBool("rep"),
                MarkImportant = ParseBool("imp"),
                AddStar = ParseBool("star"),
                Archive = ParseBool("arc"),
                Delete = ParseBool("del"),
                RescueFromSpam = ParseBool("res"),
                ImportantStarGraceLimit = ParseInt("gr", 7)
            };

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool NextBool(double min, double max) => Random.Shared.NextDouble() < NextProbability(min, max);

    private static double NextProbability(double minInclusive, double maxInclusive)
    {
        var range = maxInclusive - minInclusive;
        return minInclusive + range * Random.Shared.NextDouble();
    }
}
