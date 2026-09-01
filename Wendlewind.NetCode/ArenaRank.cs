namespace Wendlewind.NetCode;

public enum ArenaLeague
{
    Unranked,
    Bronze,
    Silver,
    Gold,
    Platinum,
    Diamond,
    Legend
}

public readonly record struct ArenaRankDisplay(
    ArenaLeague League,
    int Division,
    int Rating,
    int? LegendNumber,
    int RatedRuns)
{
    public string Label
    {
        get
        {
            if (League == ArenaLeague.Unranked)
            {
                return $"Unranked \u00b7 {Rating}";
            }

            if (League == ArenaLeague.Legend)
            {
                return LegendNumber is int number
                    ? $"Legend #{number} \u00b7 {Rating}"
                    : $"Legend \u00b7 {Rating}";
            }

            return $"{League} {Roman(Division)} \u00b7 {Rating}";
        }
    }

    public string BadgeTexturePath => $"UI/Ranks/{League}";

    private static string Roman(int division) => division switch
    {
        1 => "I",
        2 => "II",
        _ => "III"
    };
}

public readonly record struct ArenaRankDelta(
    int RatingBefore,
    int RatingAfter,
    int Delta,
    bool Applied,
    ArenaRankDisplay Before,
    ArenaRankDisplay After);

public static class ArenaRank
{
    public const int StartingRating = 800;
    public const int PlacementRuns = 5;
    public const int LegendThreshold = 1850;
    public const int UnknownSnapshotRating = 1000;

    public static int NormalizeRating(int rating, int ratedRuns) =>
        ratedRuns == 0 && rating <= 0 ? StartingRating : Math.Max(0, rating);

    public static ArenaRankDisplay FromRating(int rating, int ratedRuns, int? legendNumber = null)
    {
        rating = NormalizeRating(rating, ratedRuns);
        if (ratedRuns < PlacementRuns)
        {
            return new ArenaRankDisplay(ArenaLeague.Unranked, 0, rating, null, ratedRuns);
        }

        if (rating >= LegendThreshold)
        {
            return new ArenaRankDisplay(ArenaLeague.Legend, 0, rating, legendNumber, ratedRuns);
        }

        var (league, lo, hi) = Band(rating);
        return new ArenaRankDisplay(league, DivisionInBand(rating, lo, hi), rating, null, ratedRuns);
    }

    public static ArenaRankDelta ApplyRun(int rating, int ratedRuns, int wins, bool hadRealOpponent)
    {
        rating = NormalizeRating(rating, ratedRuns);
        var before = FromRating(rating, ratedRuns);
        if (!hadRealOpponent)
        {
            return new ArenaRankDelta(rating, rating, 0, false, before, before);
        }

        wins = Math.Clamp(wins, 0, 10);
        var expected = ExpectedWins(rating);
        var k = ratedRuns < PlacementRuns ? 25.0 : 12.0;
        var bonus = wins == 10 ? 10 : 0;
        var delta = (int)Math.Round((wins - expected) * k + bonus);
        var afterRating = Math.Max(0, rating + delta);
        var afterRuns = ratedRuns + 1;
        return new ArenaRankDelta(
            rating,
            afterRating,
            afterRating - rating,
            true,
            before,
            FromRating(afterRating, afterRuns));
    }

    public static double ExpectedWins(int rating)
    {
        rating = Math.Max(0, rating);
        return Math.Clamp(4.0 + (rating - StartingRating) / 250.0, 3.0, 8.5);
    }

    public static int EffectiveSnapshotRating(int rating) => rating > 0 ? rating : UnknownSnapshotRating;

    public static bool IsMirrorPlayerId(string? playerId) =>
        !string.IsNullOrEmpty(playerId) && playerId.StartsWith("mirror:", StringComparison.Ordinal);

    private static (ArenaLeague League, int Lo, int Hi) Band(int rating) => rating switch
    {
        < 1000 => (ArenaLeague.Bronze, 0, 999),
        < 1200 => (ArenaLeague.Silver, 1000, 1199),
        < 1400 => (ArenaLeague.Gold, 1200, 1399),
        < 1600 => (ArenaLeague.Platinum, 1400, 1599),
        _ => (ArenaLeague.Diamond, 1600, 1849)
    };

    private static int DivisionInBand(int rating, int lo, int hi)
    {
        if (lo == 0 && hi == 999)
        {
            return rating switch
            {
                <= 666 => 3,
                <= 833 => 2,
                _ => 1
            };
        }

        var span = hi - lo + 1;
        var third = Math.Max(1, span / 3);
        var offset = rating - lo;
        if (offset < third)
        {
            return 3;
        }

        return offset < third * 2 ? 2 : 1;
    }
}
