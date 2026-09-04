using System.Text;
using Wendlemire.NetCode;
using Wendlemire.NetCode.Contracts;
using Wendlemire.Sim.Arena;
using Xunit;
using Xunit.Abstractions;

namespace Wendlemire.Tests.Depth;

[Collection("Sim")]
public class StrategicDepthReport
{
    private const int ReportRunSeeds = 20;
    private const int SmokeRunSeeds = 3;
    private const int ReportFightSeeds = 2;
    private const int SmokeFightSeeds = 1;
    private const int MatrixSeeds = 8;
    private const int MirrorSeeds = 64;
    private const int HeadToHeadSeeds = 8;

    private readonly ITestOutputHelper _output;

    public StrategicDepthReport(ITestOutputHelper output)
    {
        _output = output;
        TestData.EnsureLoaded();
    }

    [Fact]
    public void EveryPolicyRunTerminates()
    {
        var rng = new Random(20260904);
        foreach (var policy in ShopPolicies.AllForReport(rng))
        {
            var result = ArenaRunHarness.Play(policy, runSeed: 101 + policy.Name.Length, fightSeeds: 1);
            Assert.True(result.Terminated, $"{policy.Name} did not end the run.");
            Assert.True(result.Fights <= 16, $"{policy.Name} played {result.Fights} fights.");
            Assert.False(result.GoldWentNegative, $"{policy.Name} went negative gold.");
            Assert.Equal(result.Wins + result.Losses, result.Fights);
            Assert.True(result.Victory || result.Losses >= ArenaRun.LossesToFinish);
        }
    }

    [Fact]
    public void SmokePlannedAndRandomPlayThreeSeeds()
    {
        var planned = new List<RunResult>();
        var random = new List<RunResult>();
        for (var seed = 1; seed <= SmokeRunSeeds; seed++)
        {
            planned.Add(ArenaRunHarness.Play(ShopPolicies.Planned(BuildGenerator.Archetype.Bruiser), 300 + seed, SmokeFightSeeds));
            random.Add(ArenaRunHarness.Play(ShopPolicies.Random(), 400 + seed, SmokeFightSeeds));
        }

        Assert.All(planned.Concat(random), r =>
        {
            Assert.True(r.Terminated);
            Assert.InRange(r.Wins, 0, ArenaRun.WinsToFinish);
        });
        _output.WriteLine(
            $"Smoke Planned mean wins {planned.Average(r => r.Wins):0.0}  " +
            $"Random mean wins {random.Average(r => r.Wins):0.0}");
    }

    [Fact]
    public void MirrorFirstStrikeBiasIsBounded()
    {
        if (!DepthGate.FullSuite)
        {
            _output.WriteLine("Skipped unless WENDLEMIRE_DEPTH=1");
            return;
        }

        var build = OpponentLadder.Generate(BuildStage.Mid, BuildGenerator.Archetype.Bruiser, 1, 17);
        var aWin = SideSwappedDuel.FirstPlayerWinRate(build, MirrorSeeds, seedOffset: 50);
        _output.WriteLine($"Identical-build first-player win rate: {aWin:P0} over {MirrorSeeds} seeds");
        Assert.InRange(aWin, 0.30, 0.70);
    }

    [Fact]
    public void RandomShoppingIsNotTheBestPolicy()
    {
        if (!DepthGate.FullSuite)
        {
            _output.WriteLine("Skipped unless WENDLEMIRE_DEPTH=1");
            return;
        }

        var planned = PlayPolicy(ShopPolicies.Planned(BuildGenerator.Archetype.Bruiser), 500);
        var random = PlayPolicy(ShopPolicies.Random(), 600);
        var plannedMean = planned.Average(r => r.Wins);
        var randomMean = random.Average(r => r.Wins);
        _output.WriteLine($"Planned mean wins {plannedMean:0.00} vs Random {randomMean:0.00}");
        Assert.True(
            plannedMean > randomMean + 0.5,
            $"Planned mean wins {plannedMean:0.00} did not beat Random {randomMean:0.00} by 0.5.");
    }

    [Fact]
    public void HoardingGoldIsWorseThanSpendingIt()
    {
        if (!DepthGate.FullSuite)
        {
            _output.WriteLine("Skipped unless WENDLEMIRE_DEPTH=1");
            return;
        }

        var planned = PlayPolicy(ShopPolicies.Planned(BuildGenerator.Archetype.Bruiser), 700);
        var hoarder = PlayPolicy(ShopPolicies.Hoarder(), 800);
        var plannedMean = planned.Average(r => r.Wins);
        var hoarderMean = hoarder.Average(r => r.Wins);
        _output.WriteLine($"Planned mean wins {plannedMean:0.00} vs Hoarder {hoarderMean:0.00}");
        Assert.True(
            plannedMean > hoarderMean + 0.5,
            $"Planned mean wins {plannedMean:0.00} did not beat Hoarder {hoarderMean:0.00} by 0.5.");
    }

    [Fact]
    public void MirrorArchetypeMatchupsAreNotAbsolute()
    {
        if (!DepthGate.FullSuite)
        {
            _output.WriteLine("Skipped unless WENDLEMIRE_DEPTH=1");
            return;
        }

        foreach (var stage in BuildStages.Generated)
        {
            foreach (var archetype in (BuildGenerator.Archetype[])Enum.GetValues(typeof(BuildGenerator.Archetype)))
            {
                var a = OpponentLadder.Generate(stage, archetype, 1, 19);
                var b = OpponentLadder.Generate(stage, archetype, 2, 19);
                var sample = SideSwappedDuel.Sample(a, b, MatrixSeeds, seedOffset: 90 + (int)stage);
                Assert.False(
                    sample.WinRate is 0 or 1,
                    $"{stage} {archetype} mirror was {sample.WinRate:P0} over {sample.Fights} fights.");
            }
        }
    }

    [Fact]
    public void GenerateReport()
    {
        if (!DepthGate.FullSuite)
        {
            _output.WriteLine("Skipped full depth report. Set WENDLEMIRE_DEPTH=1 to run it.");
            return;
        }

        var path = Path.Combine(TestData.FindContentRoot(), "depth-report.txt");
        var sb = new StringBuilder();
        var rng = new Random(384710648);
        var policies = ShopPolicies.AllForReport(rng);
        var byPolicy = new Dictionary<string, List<RunResult>>();
        foreach (var policy in policies)
        {
            byPolicy[policy.Name] = PlayPolicy(policy, 1000 + StableHash(policy.Name));
        }

        sb.AppendLine("=== Wendlemire Arena Strategic Depth ===");
        sb.AppendLine($"Run seeds: {ReportRunSeeds}   Fight samples/round: {ReportFightSeeds} side-swapped");
        sb.AppendLine($"Matrix seeds: {MatrixSeeds} side-swapped   Mirror first-strike seeds: {MirrorSeeds}");
        sb.AppendLine();
        sb.AppendLine("--- Policy runs vs shared opponent ladder ---");
        sb.AppendLine(
            $"{"Policy",-22} {"n",3} {"medW",5} {"meanW",6} {"12win%",7} {"goldLeft",8} {"spent",6} {"spent/W",8}");
        sb.AppendLine(new string('-', 78));
        foreach (var (name, runs) in byPolicy)
        {
            var wins = runs.Select(r => (double)r.Wins).OrderBy(v => v).ToList();
            var mean = wins.Average();
            var median = wins[wins.Count / 2];
            var twelve = 100.0 * runs.Count(r => r.Victory) / runs.Count;
            var goldLeft = runs.Average(r => r.GoldRemaining);
            var spent = runs.Average(r => r.GoldSpent);
            var spentPerWin = runs.Average(r => r.Wins == 0 ? r.GoldSpent : (double)r.GoldSpent / r.Wins);
            sb.AppendLine(
                $"{name,-22} {runs.Count,3} {median,5:0.0} {mean,6:0.00} {twelve,6:0}  {goldLeft,8:0} {spent,6:0} {spentPerWin,8:0.0}");
        }

        sb.AppendLine();
        sb.AppendLine("--- First-strike bias (identical Bruiser mid build, A swings first) ---");
        var mirror = OpponentLadder.Generate(BuildStage.Mid, BuildGenerator.Archetype.Bruiser, 1, 17);
        var firstStrike = SideSwappedDuel.FirstPlayerWinRate(mirror, MirrorSeeds, 50);
        sb.AppendLine($"  A-win% {firstStrike * 100:0.0} over {MirrorSeeds} seeds  (target 30-70)");

        sb.AppendLine();
        AppendPayoffMatrices(sb);

        sb.AppendLine();
        AppendGoldCorrelation(sb);

        sb.AppendLine();
        AppendHeadToHead(sb, byPolicy);

        File.WriteAllText(path, sb.ToString());
        _output.WriteLine(sb.ToString());
        _output.WriteLine($"Wrote {path}");
    }

    private static List<RunResult> PlayPolicy(IShopPolicy policy, int seedBase)
    {
        var results = new RunResult[ReportRunSeeds];
        Parallel.For(0, ReportRunSeeds, new ParallelOptions
        {
            MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount)
        }, i =>
        {
            results[i] = ArenaRunHarness.Play(policy, seedBase + i, ReportFightSeeds);
        });
        return results.ToList();
    }

    private static void AppendPayoffMatrices(StringBuilder sb)
    {
        var archetypes = (BuildGenerator.Archetype[])Enum.GetValues(typeof(BuildGenerator.Archetype));
        sb.AppendLine("--- Archetype payoff matrices (side-swapped win% for row vs column) ---");
        foreach (var stage in BuildStages.Generated)
        {
            var builds = archetypes
                .Select(a => OpponentLadder.Generate(stage, a, 1, 21))
                .ToArray();
            sb.AppendLine();
            sb.AppendLine($"R{stage.TargetRound()} {stage}");
            sb.Append("        ");
            foreach (var archetype in archetypes)
            {
                sb.Append($"{Trunc(archetype.ToString(), 7),8}");
            }

            sb.AppendLine("   field");
            var field = new double[archetypes.Length];
            for (var i = 0; i < archetypes.Length; i++)
            {
                sb.Append($"{Trunc(archetypes[i].ToString(), 7),-8}");
                var row = 0.0;
                for (var j = 0; j < archetypes.Length; j++)
                {
                    var sample = SideSwappedDuel.Sample(builds[i], builds[j], MatrixSeeds, 200 + (int)stage * 20 + i * 6 + j);
                    sb.Append($"{sample.WinRate * 100,7:0} ");
                    row += sample.WinRate;
                }

                field[i] = row / archetypes.Length;
                sb.AppendLine($"  {field[i] * 100,5:0.0}");
            }
        }
    }

    private static void AppendGoldCorrelation(StringBuilder sb)
    {
        sb.AppendLine("--- Gold spent vs side-swapped win rate (same-round generated pairs) ---");
        var pairs = new List<(int GoldGap, double RicherWinRate)>();
        foreach (var stage in BuildStages.Generated)
        {
            foreach (var left in (BuildGenerator.Archetype[])Enum.GetValues(typeof(BuildGenerator.Archetype)))
            {
                foreach (var right in (BuildGenerator.Archetype[])Enum.GetValues(typeof(BuildGenerator.Archetype)))
                {
                    if (left == right)
                    {
                        continue;
                    }

                    var a = OpponentLadder.Generate(stage, left, 1, 33);
                    var b = OpponentLadder.Generate(stage, right, 2, 33);
                    if (a.GoldSpent == b.GoldSpent)
                    {
                        continue;
                    }

                    var sample = SideSwappedDuel.Sample(a, b, MatrixSeeds, 400 + (int)stage * 40 + (int)left * 6 + (int)right);
                    var richerIsLeft = a.GoldSpent > b.GoldSpent;
                    var gap = Math.Abs(a.GoldSpent - b.GoldSpent);
                    var richerWin = richerIsLeft ? sample.WinRate : 1 - sample.WinRate;
                    pairs.Add((gap, richerWin));
                }
            }
        }

        if (pairs.Count == 0)
        {
            sb.AppendLine("  no gold-gap pairs");
            return;
        }

        var meanWin = pairs.Average(p => p.RicherWinRate);
        var meanGap = pairs.Average(p => p.GoldGap);
        var corr = Pearson(
            pairs.Select(p => (double)p.GoldGap).ToArray(),
            pairs.Select(p => p.RicherWinRate).ToArray());
        sb.AppendLine($"  Pairs: {pairs.Count}   mean gold gap {meanGap:0}   richer win% {meanWin * 100:0.0}   corr(gap, richerWR) {corr:0.00}");
        sb.AppendLine("  (richer win% should sit above 50 if gold converts to power)");
    }

    private static void AppendHeadToHead(StringBuilder sb, Dictionary<string, List<RunResult>> byPolicy)
    {
        sb.AppendLine("--- Policy snapshots head-to-head (first stored snapshot per round) ---");
        var planned = byPolicy.FirstOrDefault(kv => kv.Key.StartsWith("Planned", StringComparison.Ordinal)).Value;
        var random = byPolicy.GetValueOrDefault("Random");
        if (planned == null || random == null)
        {
            sb.AppendLine("  missing Planned or Random snapshots");
            return;
        }

        foreach (var round in ArenaRunHarness.SnapshotRounds)
        {
            var a = planned.Select(r => r.SnapshotsAtRound.GetValueOrDefault(round)).FirstOrDefault(s => s != null);
            var b = random.Select(r => r.SnapshotsAtRound.GetValueOrDefault(round)).FirstOrDefault(s => s != null);
            if (a == null || b == null)
            {
                sb.AppendLine($"  R{round}: no snapshot (runs died earlier)");
                continue;
            }

            var sample = SideSwappedDuel.Sample(a, b, HeadToHeadSeeds, 800 + round);
            sb.AppendLine(
                $"  R{round} Planned vs Random  win% {sample.WinRate * 100:0}  " +
                $"med {sample.MedianSeconds:0.0}s  gold {a.GoldSpent}/{b.GoldSpent}");
        }
    }

    private static double Pearson(double[] x, double[] y)
    {
        var n = x.Length;
        if (n == 0)
        {
            return double.NaN;
        }

        var mx = x.Average();
        var my = y.Average();
        var num = 0.0;
        var dx = 0.0;
        var dy = 0.0;
        for (var i = 0; i < n; i++)
        {
            var a = x[i] - mx;
            var b = y[i] - my;
            num += a * b;
            dx += a * a;
            dy += b * b;
        }

        return dx == 0 || dy == 0 ? double.NaN : num / Math.Sqrt(dx * dy);
    }

    private static string Trunc(string value, int n) => value.Length <= n ? value : value[..n];

    private static int StableHash(string value)
    {
        var hash = 23;
        foreach (var c in value)
        {
            hash = unchecked(hash * 31 + c);
        }

        return hash;
    }

}
