using Microsoft.Extensions.DependencyInjection;
using Wendlemire.NetCode;
using Wendlemire.NetCode.Contracts;
using Wendlemire.Sim;
using Wendlemire.Sim.Combat;
using Wendlemire.Sim.Entities.Pawns;
using Wendlemire.Sim.Zones;
using Xunit;
using Xunit.Abstractions;

namespace Wendlemire.Tests;

[Collection("Sim")]
public class ClientServerDuelTests
{
    private readonly ITestOutputHelper _output;

    public ClientServerDuelTests(ITestOutputHelper output)
    {
        TestData.EnsureLoaded();
        _output = output;
    }

    public static IEnumerable<object[]> Matchups()
    {
        var templates = BuildTemplates.All.ToArray();
        var seeds = new[] { 1, 99, CombatReplay.DefaultRunSeed };
        for (var i = 0; i < templates.Length; i++)
        {
            var attacker = templates[i] with { PlayerId = "attacker" };
            var defender = templates[(i + 1) % templates.Length] with { PlayerId = "defender" };
            foreach (var seed in seeds)
            {
                yield return [attacker, defender, seed];
            }
        }
    }

    [Theory]
    [MemberData(nameof(Matchups))]
    public void CleanClientReplayMatchesServer(BuildSnapshot attacker, BuildSnapshot defender, int seed)
    {
        var server = DuelSimulator.Run(attacker, defender, seed);
        var client = RunClientStyle(attacker, defender, seed, restoreFirst: true, dirtyHp: false);
        Assert.Equal(server.WinnerPlayerId, client.WinnerPlayerId);
        Assert.Equal(server.Ticks, client.Ticks);
    }

    [Fact]
    public void DirtyPawnWithoutRestoreDivergesFromServer()
    {
        var mismatches = new List<string>();
        foreach (var row in Matchups())
        {
            var attacker = (BuildSnapshot)row[0];
            var defender = (BuildSnapshot)row[1];
            var seed = (int)row[2];
            var server = DuelSimulator.Run(attacker, defender, seed);
            var dirty = RunClientStyle(attacker, defender, seed, restoreFirst: false, dirtyHp: true);
            if (server.WinnerPlayerId == dirty.WinnerPlayerId && server.Ticks == dirty.Ticks)
            {
                continue;
            }

            mismatches.Add(
                $"{attacker.BuildId} vs {defender.BuildId} seed={seed}: " +
                $"server {server.WinnerPlayerId}/{server.Ticks} dirty {dirty.WinnerPlayerId}/{dirty.Ticks}");
        }

        foreach (var line in mismatches)
        {
            _output.WriteLine(line);
        }

        Assert.True(
            mismatches.Count > 0,
            "Leftover HP after Apply did not change any replay. Hydration leftover is not a proven desync on these kits.");
    }

    private static CombatResult RunClientStyle(
        BuildSnapshot attacker,
        BuildSnapshot defender,
        int seed,
        bool restoreFirst,
        bool dirtyHp)
    {
        using var root = SimServices.BuildRoot();
        using var scope = root.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<GameContext>();
        context.InitializeArena(attacker.PlayerId, attacker.PawnName ?? "Attacker", seed);
        BuildSnapshotFactory.Apply(context.PlayerPawn, attacker);

        if (dirtyHp)
        {
            foreach (var part in context.PlayerPawn.Body.AllExternalParts)
            {
                part.HitPoints = Math.Min(part.HitPoints, 1);
            }
        }

        var zone = context.World.Zones.OrderBy(z => z.ZoneDef.Stage).First();
        context.EnterZone(zone.ZoneDef);
        if (restoreFirst)
        {
            context.RestoreArenaPawn();
        }

        var opponent = BuildSnapshotFactory.CreatePawn(context, defender, PawnType.Enemy);
        BuildSnapshotFactory.Apply(context.PlayerPawn, attacker);
        context.CurrentZone!.StartHumanDuel(context.PlayerPawn, opponent, seed);

        var encounter = context.CurrentZone.ActiveEncounter
                        ?? throw new InvalidOperationException("StartHumanDuel did not create an encounter.");
        var guard = 0;
        while (encounter.State == EncounterState.InProgress && guard < CombatReplay.MaxTicks)
        {
            context.Tick();
            guard++;
        }

        var localWon = !context.PlayerPawn.IsDead;
        return new CombatResult
        {
            MatchId = "client",
            WinnerPlayerId = localWon ? attacker.PlayerId : defender.PlayerId,
            Ticks = encounter.Ticks,
            CauseOfDeath = encounter.CombatHandler?.CauseOfDeath,
            DefenderPlayerId = defender.PlayerId,
            Defender = defender,
            EncounterSeed = encounter.Seed
        };
    }
}
