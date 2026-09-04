using Wendlemire.NetCode;
using Wendlemire.NetCode.Contracts;
using Wendlemire.Sim.Combat;

namespace Wendlemire.Tests.Depth;

internal sealed record SideSwappedResult(
    double WinRate,
    double WilsonLow,
    double WilsonHigh,
    double MedianSeconds,
    double CloserShare,
    double FirstStrikeDelta,
    int Seeds,
    int LeftWins,
    int AFirstLeftWins,
    int BFirstLeftWins,
    int CloserFights,
    int Fights,
    int Timeouts)
{
    public double AFirstWinRate => Seeds == 0 ? 0 : (double)AFirstLeftWins / Seeds;

    public double BFirstWinRate => Seeds == 0 ? 0 : (double)BFirstLeftWins / Seeds;
}

internal static class SideSwappedDuel
{
    public static SideSwappedResult Sample(
        BuildSnapshot left,
        BuildSnapshot right,
        int seedCount,
        int seedOffset = 1)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(seedCount);

        var taggedLeft = left with { PlayerId = "left" };
        var taggedRight = right with { PlayerId = "right" };
        var ticks = new int[seedCount * 2];
        var aFirstLeftWins = 0;
        var bFirstLeftWins = 0;
        var closer = 0;
        var timeouts = 0;

        Parallel.For(0, seedCount, new ParallelOptions
        {
            MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount)
        }, i =>
        {
            var seed = seedOffset + i;
            var aFirst = RunOne(taggedLeft, taggedRight, seed);
            var bFirst = RunOne(taggedRight, taggedLeft, seed + 1_000_003);

            ticks[i * 2] = aFirst.Ticks;
            ticks[i * 2 + 1] = bFirst.Ticks;

            if (aFirst.Timeout)
            {
                Interlocked.Increment(ref timeouts);
            }

            if (bFirst.Timeout)
            {
                Interlocked.Increment(ref timeouts);
            }

            if (aFirst.Closer)
            {
                Interlocked.Increment(ref closer);
            }

            if (bFirst.Closer)
            {
                Interlocked.Increment(ref closer);
            }

            if (aFirst.WinnerId == "left")
            {
                Interlocked.Increment(ref aFirstLeftWins);
            }

            if (bFirst.WinnerId == "left")
            {
                Interlocked.Increment(ref bFirstLeftWins);
            }
        });

        Array.Sort(ticks);
        var fights = seedCount * 2;
        var leftWins = aFirstLeftWins + bFirstLeftWins;
        var winRate = fights == 0 ? 0 : (double)leftWins / fights;
        var (low, high) = WilsonInterval(leftWins, fights);
        var aFirstRate = (double)aFirstLeftWins / seedCount;
        var bFirstRate = (double)bFirstLeftWins / seedCount;
        return new SideSwappedResult(
            WinRate: winRate,
            WilsonLow: low,
            WilsonHigh: high,
            MedianSeconds: ticks[ticks.Length / 2] / 60.0,
            CloserShare: fights == 0 ? 0 : (double)closer / fights,
            FirstStrikeDelta: aFirstRate - bFirstRate,
            Seeds: seedCount,
            LeftWins: leftWins,
            AFirstLeftWins: aFirstLeftWins,
            BFirstLeftWins: bFirstLeftWins,
            CloserFights: closer,
            Fights: fights,
            Timeouts: timeouts);
    }

    public static double FirstPlayerWinRate(BuildSnapshot build, int seedCount, int seedOffset = 1)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(seedCount);
        var a = build with { PlayerId = "left" };
        var b = build with { PlayerId = "right" };
        var wins = 0;
        Parallel.For(0, seedCount, new ParallelOptions
        {
            MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount)
        }, i =>
        {
            var result = RunOne(a, b, seedOffset + i);
            if (result.WinnerId == "left")
            {
                Interlocked.Increment(ref wins);
            }
        });

        return (double)wins / seedCount;
    }

    private static Outcome RunOne(BuildSnapshot attacker, BuildSnapshot defender, int seed)
    {
        try
        {
            var sim = DuelSimulator.Simulate(attacker, defender, seed);
            var ticks = sim.Result.Ticks;
            var cause = sim.Result.CauseOfDeath ?? "";
            var closer = ticks >= CombatCloser.StartTicks
                         || cause.Equals(CombatCloser.CauseOfDeath, StringComparison.OrdinalIgnoreCase);
            return new Outcome(sim.Result.WinnerPlayerId, ticks, closer, Timeout: false);
        }
        catch (TimeoutException)
        {
            return new Outcome(null, CombatReplay.MaxTicks, Closer: true, Timeout: true);
        }
    }

    internal static (double Low, double High) WilsonInterval(int wins, int n, double z = 1.96)
    {
        if (n <= 0)
        {
            return (0, 1);
        }

        var p = (double)wins / n;
        var z2 = z * z;
        var denom = 1 + z2 / n;
        var center = (p + z2 / (2 * n)) / denom;
        var margin = z * Math.Sqrt(p * (1 - p) / n + z2 / (4 * n * n)) / denom;
        return (Math.Clamp(center - margin, 0, 1), Math.Clamp(center + margin, 0, 1));
    }

    private readonly record struct Outcome(string? WinnerId, int Ticks, bool Closer, bool Timeout);
}
