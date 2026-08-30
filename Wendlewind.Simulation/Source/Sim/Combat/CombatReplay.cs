using Microsoft.Extensions.DependencyInjection;

namespace Wendlewind.Sim.Combat;

/// <summary>
/// Runs a seeded encounter to completion so two contexts can be compared.
/// </summary>
public static class CombatReplay
{
    public const int DefaultRunSeed = 384710648;
    public const int MaxTicks = 100_000;

    public readonly record struct Result(
        int RunSeed,
        int EncounterSeed,
        string ZoneMoniker,
        string EnemyMoniker,
        bool PlayerAlive,
        int Ticks,
        string? CauseOfDeath);

    public static Result Run(int runSeed = DefaultRunSeed, Action<GameContext>? configure = null)
    {
        using var root = SimServices.BuildRoot();
        using var scope = root.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<GameContext>();
        context.Initialize(runSeed);
        configure?.Invoke(context);

        var zone = context.World.Zones.OrderBy(z => z.ZoneDef.Stage).First();
        context.EnterZone(zone.ZoneDef);
        context.CurrentZone!.NextEncounter();

        var encounter = context.CurrentZone.ActiveEncounter
                        ?? throw new InvalidOperationException("NextEncounter did not create an encounter.");

        int guard = 0;
        while (encounter.State == EncounterState.InProgress && guard < MaxTicks)
        {
            context.Tick();
            guard++;
        }

        if (encounter.State == EncounterState.InProgress)
        {
            throw new TimeoutException($"Encounter did not finish within {MaxTicks} ticks.");
        }

        var handler = encounter.CombatHandler;
        return new Result(
            RunSeed: runSeed,
            EncounterSeed: encounter.Seed,
            ZoneMoniker: zone.ZoneDef.Moniker,
            EnemyMoniker: encounter.EnemyPawns.First().PawnDef.Moniker,
            PlayerAlive: !context.PlayerPawn.IsDead,
            Ticks: encounter.Ticks,
            CauseOfDeath: handler?.CauseOfDeath);
    }

    public static (Result Summary, CombatLogEvent[] Log) RunWithLog(
        int runSeed = DefaultRunSeed,
        Action<GameContext>? configure = null)
    {
        using var root = SimServices.BuildRoot();
        using var scope = root.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<GameContext>();
        context.Initialize(runSeed);
        configure?.Invoke(context);

        var zone = context.World.Zones.OrderBy(z => z.ZoneDef.Stage).First();
        context.EnterZone(zone.ZoneDef);
        context.CurrentZone!.NextEncounter();

        var encounter = context.CurrentZone.ActiveEncounter
                        ?? throw new InvalidOperationException("NextEncounter did not create an encounter.");

        int guard = 0;
        while (encounter.State == EncounterState.InProgress && guard < MaxTicks)
        {
            context.Tick();
            guard++;
        }

        if (encounter.State == EncounterState.InProgress)
        {
            throw new TimeoutException($"Encounter did not finish within {MaxTicks} ticks.");
        }

        var handler = encounter.CombatHandler;
        var summary = new Result(
            RunSeed: runSeed,
            EncounterSeed: encounter.Seed,
            ZoneMoniker: zone.ZoneDef.Moniker,
            EnemyMoniker: encounter.EnemyPawns.First().PawnDef.Moniker,
            PlayerAlive: !context.PlayerPawn.IsDead,
            Ticks: encounter.Ticks,
            CauseOfDeath: handler?.CauseOfDeath);
        return (summary, handler?.Log.ToArray() ?? []);
    }

    public static Result AssertDeterministic(int runSeed = DefaultRunSeed, Action<GameContext>? configure = null)
    {
        var first = Run(runSeed, configure);
        var second = Run(runSeed, configure);

        if (first != second)
        {
            throw new InvalidOperationException(
                $"Combat replay diverged.\nFirst:  {first}\nSecond: {second}");
        }

        return first;
    }
}
