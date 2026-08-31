using Wendlewind.NetCode.Contracts;
using Wendlewind.Sim.Combat;

namespace Wendlewind.NetCode;

public sealed class FightAnalyticsService
{
    private readonly PlayerStore _players;

    public FightAnalyticsService(PlayerStore players)
    {
        _players = players;
    }

    public List<FightAnalyticsRow> ListFights()
    {
        return _players.ListAllRuns()
            .SelectMany(run => run.Fights.Select(fight => ToRow(run, fight)))
            .OrderByDescending(row => row.DurationSeconds)
            .ToList();
    }

    public FightAnalyticsSummary Summarize()
    {
        var rows = ListFights();
        if (rows.Count == 0)
        {
            return new FightAnalyticsSummary();
        }

        var durations = rows.Select(row => row.DurationSeconds).OrderBy(seconds => seconds).ToArray();
        var inBand = rows.Count(row => row.InTargetBand);
        var longest = rows.MaxBy(row => row.DurationSeconds);
        var shortest = rows.MinBy(row => row.DurationSeconds);
        var causes = rows
            .GroupBy(row => string.IsNullOrWhiteSpace(row.CauseOfDeath) ? "unknown" : row.CauseOfDeath)
            .ToDictionary(group => group.Key, group => group.Count());

        return new FightAnalyticsSummary
        {
            Count = rows.Count,
            InTargetBandPercent = 100.0 * inBand / rows.Count,
            DurationP50 = Percentile(durations, 0.5),
            DurationP90 = Percentile(durations, 0.9),
            DurationMin = durations[0],
            DurationMax = durations[^1],
            CauseOfDeath = causes,
            LongestMatchId = longest?.MatchId,
            ShortestMatchId = shortest?.MatchId
        };
    }

    public CombatLogRecord? GetLog(string matchId) => _players.FindCombatLog(matchId);

    public BackfillResult Backfill()
    {
        var runs = _players.ListAllRuns();
        var pending = runs
            .SelectMany(run => run.Fights
                .Where(fight => fight.Analytics == null)
                .Select(fight => (run.PlayerId, run.RunId, Fight: fight)))
            .ToList();

        var scanned = runs.Sum(run => run.Fights.Count);
        var updated = 0;
        foreach (var item in pending)
        {
            var simulation = DuelSimulator.Simulate(
                item.Fight.Attacker,
                item.Fight.Defender,
                item.Fight.EncounterSeed);
            if (_players.TryUpdateFight(item.PlayerId, item.RunId, item.Fight.MatchId, simulation.Analytics, simulation.Log))
            {
                updated++;
            }
        }

        return new BackfillResult
        {
            Scanned = scanned,
            Updated = updated,
            Skipped = scanned - updated
        };
    }

    private static FightAnalyticsRow ToRow(ArenaRunRecord run, ArenaFightRecord fight)
    {
        var seconds = fight.Analytics?.DurationSeconds ?? CombatAnalytics.TicksToSeconds(fight.Ticks);
        return new FightAnalyticsRow
        {
            MatchId = fight.MatchId,
            PlayerId = run.PlayerId,
            RunId = run.RunId,
            Round = fight.Round,
            DurationSeconds = seconds,
            InTargetBand = fight.Analytics?.InTargetBand ?? CombatAnalytics.IsInTargetBand(seconds),
            WinnerPlayerId = fight.WinnerPlayerId,
            CauseOfDeath = fight.CauseOfDeath,
            AttackerDamagePerSecond = fight.Analytics?.Attacker.DamagePerSecond ?? 0,
            DefenderDamagePerSecond = fight.Analytics?.Defender.DamagePerSecond ?? 0,
            AttackerDamage = fight.Analytics?.Attacker.DamageDealt ?? 0,
            DefenderDamage = fight.Analytics?.Defender.DamageDealt ?? 0,
            AttackerHealing = fight.Analytics?.Attacker.Healing ?? 0,
            DefenderHealing = fight.Analytics?.Defender.Healing ?? 0,
            KillingWeapon = fight.Analytics?.KillingWeapon,
            KillingManeuver = fight.Analytics?.KillingManeuver
        };
    }

    private static double Percentile(IReadOnlyList<double> sorted, double p)
    {
        if (sorted.Count == 1)
        {
            return sorted[0];
        }

        var index = (int)Math.Round((sorted.Count - 1) * p, MidpointRounding.AwayFromZero);
        return sorted[index];
    }
}
