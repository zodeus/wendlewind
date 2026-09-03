using Microsoft.Extensions.DependencyInjection;
using Wendlemire.NetCode;
using Wendlemire.NetCode.Contracts;
using Wendlemire.Sim;
using Wendlemire.Sim.Entities.Items.Medicinals;
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
    public void ComfortableStanceAliasesToBalanced()
    {
        var (attacker, defender, _, encounterSeed) = LiveClothPaulVsShnook();
        var withLegacy = defender with { StanceMoniker = "Comfortable" };
        var withBalanced = defender with { StanceMoniker = "Balanced" };
        var legacy = DuelSimulator.Run(attacker, withLegacy, encounterSeed);
        var current = DuelSimulator.Run(attacker, withBalanced, encounterSeed);
        Assert.Equal(current.WinnerPlayerId, legacy.WinnerPlayerId);
        Assert.Equal(current.Ticks, legacy.Ticks);
    }

    [Fact]
    public void LiveClothPaulVsPatatangatonkus_ReplayTicks()
    {
        var (attacker, defender, runSeed, encounterSeed) = LiveClothPaulVsPatatangatonkus();
        var server = DuelSimulator.Run(attacker, defender, encounterSeed);
        var arena = RunArenaVisualPath(attacker, defender, runSeed, encounterSeed, lightIncense: true, eatNothing: true);
        _output.WriteLine($"duel {server.WinnerPlayerId}/{server.Ticks}");
        _output.WriteLine($"arena {arena.WinnerPlayerId}/{arena.Ticks}");
        Assert.Equal(server.WinnerPlayerId, arena.WinnerPlayerId);
        Assert.Equal(server.Ticks, arena.Ticks);
        Assert.Equal(3802, server.Ticks);
    }

    [Fact]
    public void LiveClothPaulVsShnook_ArenaClientPathMatchesServer()
    {
        var (attacker, defender, runSeed, encounterSeed) = LiveClothPaulVsShnook();
        var server = DuelSimulator.Run(attacker, defender, encounterSeed);
        var clean = RunClientStyle(attacker, defender, encounterSeed, restoreFirst: true, dirtyHp: false);
        var arena = RunArenaVisualPath(attacker, defender, runSeed, encounterSeed, lightIncense: true, eatNothing: true);
        _output.WriteLine($"server {server.WinnerPlayerId}/{server.Ticks}");
        _output.WriteLine($"clean  {clean.WinnerPlayerId}/{clean.Ticks}");
        _output.WriteLine($"arena  {arena.WinnerPlayerId}/{arena.Ticks}");
        Assert.Equal(server.WinnerPlayerId, clean.WinnerPlayerId);
        Assert.Equal(server.Ticks, clean.Ticks);
        Assert.Equal(server.WinnerPlayerId, arena.WinnerPlayerId);
        Assert.Equal(server.Ticks, arena.Ticks);
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

    private static (BuildSnapshot Attacker, BuildSnapshot Defender, int RunSeed, int EncounterSeed)
        LiveClothPaulVsShnook()
    {
        var attacker = new BuildSnapshot
        {
            PlayerId = "b4d17c7a0ab245c1b78363dbdd320351",
            BuildId = "arena-1",
            EntityDefMonikers = ["LeatherHelmet", "LeatherTunic", "LeatherGlove", "BoneKnife"],
            Seed = 1705594468,
            PawnDefMoniker = "HumanA",
            PawnName = "Cloth Paul",
            NamePlateMoniker = "PlainWood",
            Round = 1,
            Rating = 725,
            StanceMoniker = "Balanced",
            Weapons =
            [
                new WeaponConfig { ItemMoniker = "FleshyHand", UseInCombat = false },
                new WeaponConfig { ItemMoniker = "BoneKnife", UseInCombat = true },
                new WeaponConfig { ItemMoniker = "FleshyHand", UseInCombat = false },
                new WeaponConfig { ItemMoniker = "FleshyFoot", UseInCombat = false },
                new WeaponConfig { ItemMoniker = "FleshyFoot", UseInCombat = false }
            ],
            Meal = ["CookedFish"],
            FoodBuffs = ["CookedFish"],
            Incense = [new IncenseConfig { ItemMoniker = "MullinStick", EncountersRemaining = 2 }],
            Inventory =
            [
                new InventoryStackConfig { ItemMoniker = "CookedFish", Amount = 1 },
                new InventoryStackConfig { ItemMoniker = "MullinStick", Amount = 1 }
            ]
        };
        var defender = new BuildSnapshot
        {
            PlayerId = "03cbf77d7301473f90932c87cdc082fa",
            BuildId = "arena-1",
            EntityDefMonikers = ["LeatherTunic", "BoneAxe"],
            Seed = 1902820420,
            PawnDefMoniker = "HumanA",
            PawnName = "shnook",
            NamePlateMoniker = "PlainWood",
            Round = 1,
            Rating = 800,
            StanceMoniker = "Comfortable",
            Weapons =
            [
                new WeaponConfig { ItemMoniker = "FleshyHand", UseInCombat = false },
                new WeaponConfig { ItemMoniker = "BoneAxe", UseInCombat = true },
                new WeaponConfig { ItemMoniker = "FleshyHand", UseInCombat = false },
                new WeaponConfig { ItemMoniker = "FleshyFoot", UseInCombat = false },
                new WeaponConfig { ItemMoniker = "FleshyFoot", UseInCombat = false }
            ],
            Inventory = [new InventoryStackConfig { ItemMoniker = "MedKit", Amount = 5 }]
        };
        return (attacker, defender, 1705594468, 557917426);
    }

    private static (BuildSnapshot Attacker, BuildSnapshot Defender, int RunSeed, int EncounterSeed)
        LiveClothPaulVsPatatangatonkus()
    {
        var attacker = new BuildSnapshot
        {
            PlayerId = "b4d17c7a0ab245c1b78363dbdd320351",
            BuildId = "arena-2",
            EntityDefMonikers = ["LeatherHelmet", "LeatherTunic", "LeatherVambrace", "LeatherGlove", "BoneKnife"],
            Seed = 1705594468,
            PawnDefMoniker = "HumanA",
            PawnName = "Cloth Paul",
            NamePlateMoniker = "PlainWood",
            Round = 2,
            Rating = 725,
            StanceMoniker = "Balanced",
            Weapons =
            [
                new WeaponConfig { ItemMoniker = "FleshyHand", UseInCombat = false },
                new WeaponConfig { ItemMoniker = "BoneKnife", UseInCombat = true },
                new WeaponConfig { ItemMoniker = "FleshyHand", UseInCombat = false },
                new WeaponConfig { ItemMoniker = "FleshyFoot", UseInCombat = false },
                new WeaponConfig { ItemMoniker = "FleshyFoot", UseInCombat = false }
            ],
            Meal = ["CookedFish"],
            FoodBuffs = ["CookedFish"],
            Incense = [new IncenseConfig { ItemMoniker = "MullinStick", EncountersRemaining = 2 }],
            Inventory =
            [
                new InventoryStackConfig { ItemMoniker = "CookedFish", Amount = 1 },
                new InventoryStackConfig { ItemMoniker = "MullinStick", Amount = 1 }
            ],
            Skills = [new SkillConfig { SkillMoniker = "Knives", Level = 0, CurrentLevelXp = 19 }]
        };
        var defender = new BuildSnapshot
        {
            PlayerId = "a55a2826794e434a8e721ed0a9e49a13",
            BuildId = "arena-2",
            EntityDefMonikers =
            [
                "LeatherHelmet", "LeatherGorget", "LeatherTunic", "LeatherVambrace",
                "LeatherGlove", "BoneKnife", "IronMace"
            ],
            Seed = 195940009,
            PawnDefMoniker = "HumanA",
            PawnName = "Patatangatonkus",
            NamePlateMoniker = "PlainWood",
            Round = 2,
            Rating = 800,
            StanceMoniker = "Comfortable",
            Weapons =
            [
                new WeaponConfig { ItemMoniker = "FleshyHand", UseInCombat = false },
                new WeaponConfig { ItemMoniker = "BoneKnife", UseInCombat = true },
                new WeaponConfig { ItemMoniker = "FleshyHand", UseInCombat = false },
                new WeaponConfig { ItemMoniker = "IronMace", UseInCombat = true },
                new WeaponConfig { ItemMoniker = "FleshyFoot", UseInCombat = false },
                new WeaponConfig { ItemMoniker = "FleshyFoot", UseInCombat = false }
            ],
            Meal = ["HeartyStew"],
            FoodBuffs = ["HeartyStew"],
            MedicalChest =
            [
                new MedicalChestConfig
                {
                    ItemMoniker = "MedKit",
                    Charges = 2,
                    Type = MedicalTriggerType.PartBelowHealth,
                    TargetSelector = MedicalTargetSelector.SpecificPart,
                    HealthThreshold = 0.7f,
                    TargetPartKey = "HumanHead_HeadSocket"
                },
                new MedicalChestConfig
                {
                    ItemMoniker = "MedKit",
                    Charges = 1,
                    Type = MedicalTriggerType.PartBelowHealth,
                    TargetSelector = MedicalTargetSelector.SpecificPart,
                    HealthThreshold = 0.65f,
                    TargetPartKey = "HumanHand_Left"
                },
                new MedicalChestConfig
                {
                    ItemMoniker = "MedKit",
                    Charges = 2,
                    Type = MedicalTriggerType.PartBelowHealth,
                    TargetSelector = MedicalTargetSelector.SpecificPart,
                    HealthThreshold = 0.7f,
                    TargetPartKey = "HumanTorso_TorsoSocket"
                }
            ],
            Inventory = [new InventoryStackConfig { ItemMoniker = "HeartyStew", Amount = 1 }],
            Skills = [new SkillConfig { SkillMoniker = "Knives", Level = 0, CurrentLevelXp = 16 }]
        };
        return (attacker, defender, 1705594468, 557917437);
    }

    private static CombatResult RunArenaVisualPath(
        BuildSnapshot attacker,
        BuildSnapshot defender,
        int runSeed,
        int encounterSeed,
        bool lightIncense,
        bool eatNothing)
    {
        using var root = SimServices.BuildRoot();
        using var scope = root.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<GameContext>();
        context.InitializeArena(attacker.PlayerId, attacker.PawnName ?? "Attacker", runSeed);
        BuildSnapshotFactory.Apply(context.PlayerPawn, attacker);
        if (lightIncense)
        {
            foreach (var item in context.PlayerPawn.Inventory.ToList())
            {
                if (item.ItemDef.IncenseProperties != null)
                {
                    context.PlayerPawn.TryLightIncense(item, requireFlameStick: false);
                }
            }
        }

        _ = eatNothing;
        var zone = context.World.Zones.OrderBy(z => z.ZoneDef.Stage).First();
        context.EnterZone(zone.ZoneDef);
        context.RestoreArenaPawn();
        var opponent = BuildSnapshotFactory.CreatePawn(context, defender, PawnType.Enemy);
        BuildSnapshotFactory.Apply(context.PlayerPawn, attacker);
        context.CurrentZone!.StartHumanDuel(context.PlayerPawn, opponent, encounterSeed);

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
            MatchId = "arena",
            WinnerPlayerId = localWon ? attacker.PlayerId : defender.PlayerId,
            Ticks = encounter.Ticks,
            CauseOfDeath = encounter.CombatHandler?.CauseOfDeath,
            DefenderPlayerId = defender.PlayerId,
            Defender = defender,
            EncounterSeed = encounter.Seed
        };
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
