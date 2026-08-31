using System.Text.Json;
using Wendlewind.Definitions;
using Wendlewind.NetCode;
using Wendlewind.NetCode.Contracts;
using Wendlewind.Sim.Arena;
using Wendlewind.Sim.Combat;
using Xunit;

namespace Wendlewind.Tests;

[Collection("Sim")]
public class PlayerStoreTests
{
    public PlayerStoreTests()
    {
        TestData.EnsureLoaded();
    }

    [Fact]
    public void PersistsProfileAchievementsProgressAndReplayableFights()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"ww-data-{Guid.NewGuid():N}");
        try
        {
            var store = new PlayerStore(dir);
            var profile = store.GetOrCreateProfile("alice", "Alice", "alice");
            Assert.Equal("alice", profile.PlayerId);
            Assert.Equal("Alice", profile.DisplayName);
            Assert.Equal("alice", profile.Username);

            store.SaveAchievements("alice", new AchievementState
            {
                Achievements =
                [
                    new AchievementRecord
                    {
                        Moniker = "CaveDiver",
                        CurrentValue = 1,
                        IsUnlocked = true,
                        IsAcknowledged = true
                    }
                ]
            });
            Assert.Equal("CaveDiver", store.GetAchievements("alice").Achievements[0].Moniker);

            var started = store.StartArena("alice", "Alice", 99);
            store.SaveCurrentArena(started with
            {
                Gold = 400,
                Wins = 2,
                Loadout = BuildTemplates.TankRegen() with { PlayerId = "alice", Round = 1 }
            });
            Assert.Equal(400, store.GetCurrentArena("alice")!.Gold);
            Assert.Equal("IronSword", store.GetCurrentArena("alice")!.Loadout!.EntityDefMonikers[0]);

            var attacker = BuildTemplates.TankRegen() with { PlayerId = "alice", Round = 1 };
            var defender = BuildTemplates.AcidRusher() with { PlayerId = "bob", Round = 1 };
            var simulation = DuelSimulator.Simulate(attacker, defender, CombatReplay.DefaultRunSeed);
            var result = simulation.Result;
            store.AppendFight("alice", new ArenaFightRecord
            {
                MatchId = result.MatchId,
                Round = 1,
                Attacker = attacker,
                Defender = defender,
                EncounterSeed = result.EncounterSeed,
                WinnerPlayerId = result.WinnerPlayerId,
                Ticks = result.Ticks,
                CauseOfDeath = result.CauseOfDeath,
                FoughtAt = DateTimeOffset.UtcNow,
                Analytics = simulation.Analytics
            }, simulation.Log);

            var run = store.GetRun("alice", started.RunId);
            Assert.NotNull(run);
            Assert.Single(run.Fights);
            var storedAnalytics = run.Fights[0].Analytics;
            Assert.NotNull(storedAnalytics);
            Assert.Equal(result.Ticks, storedAnalytics.DurationTicks);
            Assert.Equal(result.Ticks / 60.0, storedAnalytics.DurationSeconds);
            var storedLog = store.GetCombatLog("alice", started.RunId, result.MatchId);
            Assert.NotNull(storedLog);
            Assert.Equal(result.MatchId, storedLog.MatchId);
            Assert.Equal(simulation.Log.Length, storedLog.Events.Length);
            Assert.Equal(simulation.Log[0].Kind, storedLog.Events[0].Kind);
            var runDir = Path.Combine(dir, "players", "alice", "arena-runs", started.RunId);
            Assert.True(File.Exists(Path.Combine(runDir, "match.json")));
            Assert.True(File.Exists(Path.Combine(runDir, "combat-events.json")));
            Assert.False(File.Exists(Path.Combine(dir, "players", "alice", "arena-runs", $"{started.RunId}.json")));
            Assert.False(Directory.Exists(Path.Combine(runDir, "logs")));
            var replayed = DuelSimulator.Run(run.Fights[0].Attacker, run.Fights[0].Defender, run.Fights[0].EncounterSeed);
            Assert.Equal(run.Fights[0].WinnerPlayerId, replayed.WinnerPlayerId);
            Assert.Equal(run.Fights[0].Ticks, replayed.Ticks);

            store.FinishCurrent("alice", victory: false);
            Assert.Null(store.GetCurrentArena("alice"));
            Assert.False(store.GetRun("alice", started.RunId)!.Victory);
        }
        finally
        {
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, true);
            }
        }
    }

    [Fact]
    public void RestartingUnfinishedRunKeepsRootSeedAndOpeningShop()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"ww-data-{Guid.NewGuid():N}");
        try
        {
            var store = new PlayerStore(dir);
            var first = store.StartArena("alice", "Alice");
            var second = store.StartArena("alice", "Alice");
            Assert.Equal(first.RunSeed, second.RunSeed);
            Assert.NotEqual(first.RunId, second.RunId);

            var merchant = DefRepository<MerchantDef>.GetByMoniker("GeneralStore")!;
            Assert.Equal(
                ShopStock.Flatten(ShopStock.Roll(merchant, first.RunSeed, 0)).Select(offer => offer.StockKey),
                ShopStock.Flatten(ShopStock.Roll(merchant, second.RunSeed, 0)).Select(offer => offer.StockKey));
        }
        finally
        {
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, true);
            }
        }
    }

    [Fact]
    public void MigratesLegacyRunFileAndPerMatchLogs()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"ww-data-{Guid.NewGuid():N}");
        try
        {
            var store = new PlayerStore(dir);
            var started = store.StartArena("alice", "Alice", 1);
            var attacker = BuildTemplates.TankRegen() with { PlayerId = "alice" };
            var defender = BuildTemplates.AcidRusher() with { PlayerId = "bob" };
            store.AppendFight("alice", new ArenaFightRecord
            {
                MatchId = "legacy-match",
                Round = 1,
                Attacker = attacker,
                Defender = defender,
                EncounterSeed = 1,
                WinnerPlayerId = "alice",
                Ticks = 1800,
                CauseOfDeath = "Blood loss",
                FoughtAt = DateTimeOffset.UtcNow
            });

            var runsDir = Path.Combine(dir, "players", "alice", "arena-runs");
            var runDir = Path.Combine(runsDir, started.RunId);
            var matchPath = Path.Combine(runDir, "match.json");
            var eventsPath = Path.Combine(runDir, "combat-events.json");
            var legacyRunPath = Path.Combine(runsDir, $"{started.RunId}.json");
            var legacyLogPath = Path.Combine(runDir, "logs", "legacy-match.json");
            Directory.CreateDirectory(Path.GetDirectoryName(legacyLogPath)!);
            File.Move(matchPath, legacyRunPath);
            File.Delete(eventsPath);
            File.WriteAllText(
                legacyLogPath,
                JsonSerializer.Serialize(
                    new CombatLogRecord
                    {
                        MatchId = "legacy-match",
                        Events = [new CombatLogEvent { Kind = CombatEventKind.Death, Tick = 1800, Message = "Blood loss" }]
                    },
                    NetCodeJsonContext.Default.CombatLogRecord));

            var migrated = store.GetRun("alice", started.RunId);
            Assert.NotNull(migrated);
            Assert.Single(migrated.Fights);
            Assert.True(File.Exists(matchPath));
            Assert.True(File.Exists(eventsPath));
            Assert.False(File.Exists(legacyRunPath));
            Assert.False(Directory.Exists(Path.Combine(runDir, "logs")));
            var log = store.GetCombatLog("alice", started.RunId, "legacy-match");
            Assert.NotNull(log);
            Assert.Equal(CombatEventKind.Death, log.Events[0].Kind);
        }
        finally
        {
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, true);
            }
        }
    }

    [Fact]
    public void AnalyticsSummaryReportsBandAndPercentiles()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"ww-data-{Guid.NewGuid():N}");
        try
        {
            var store = new PlayerStore(dir);
            store.StartArena("alice", "Alice", 1);
            var attacker = BuildTemplates.TankRegen() with { PlayerId = "alice" };
            var defender = BuildTemplates.AcidRusher() with { PlayerId = "bob" };
            AppendAnalyzedFight(store, "m-short", attacker, defender, seconds: 20);
            AppendAnalyzedFight(store, "m-mid", attacker, defender, seconds: 40);
            AppendAnalyzedFight(store, "m-long", attacker, defender, seconds: 80);

            var service = new FightAnalyticsService(store);
            var summary = service.Summarize();
            Assert.Equal(3, summary.Count);
            Assert.Equal(20, summary.DurationMin);
            Assert.Equal(80, summary.DurationMax);
            Assert.Equal(40, summary.DurationP50);
            Assert.Equal(80, summary.DurationP90);
            Assert.Equal(100.0 / 3, summary.InTargetBandPercent, 5);
            Assert.Equal("m-long", summary.LongestMatchId);
            Assert.Equal("m-short", summary.ShortestMatchId);
            Assert.Equal(2, summary.CauseOfDeath["Blood loss"]);
            Assert.Equal(1, summary.CauseOfDeath["Burning"]);
        }
        finally
        {
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, true);
            }
        }
    }

    [Fact]
    public void BackfillWritesAnalyticsAndNoOpsExisting()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"ww-data-{Guid.NewGuid():N}");
        try
        {
            var store = new PlayerStore(dir);
            var started = store.StartArena("alice", "Alice", 99);
            var attacker = BuildTemplates.TankRegen() with { PlayerId = "alice", Round = 1 };
            var defender = BuildTemplates.AcidRusher() with { PlayerId = "bob", Round = 1 };
            var result = DuelSimulator.Run(attacker, defender, CombatReplay.DefaultRunSeed);
            store.AppendFight("alice", new ArenaFightRecord
            {
                MatchId = result.MatchId,
                Round = 1,
                Attacker = attacker,
                Defender = defender,
                EncounterSeed = result.EncounterSeed,
                WinnerPlayerId = result.WinnerPlayerId,
                Ticks = result.Ticks,
                CauseOfDeath = result.CauseOfDeath,
                FoughtAt = DateTimeOffset.UtcNow
            });

            Assert.Null(store.GetRun("alice", started.RunId)!.Fights[0].Analytics);
            Assert.Null(store.GetCombatLog("alice", started.RunId, result.MatchId));

            var service = new FightAnalyticsService(store);
            var first = service.Backfill();
            Assert.Equal(1, first.Scanned);
            Assert.Equal(1, first.Updated);
            Assert.Equal(0, first.Skipped);

            var run = store.GetRun("alice", started.RunId)!;
            var backfilled = run.Fights[0].Analytics;
            Assert.NotNull(backfilled);
            Assert.Equal(result.Ticks, backfilled.DurationTicks);
            var log = store.GetCombatLog("alice", started.RunId, result.MatchId);
            Assert.NotNull(log);
            Assert.NotEmpty(log.Events);

            var rows = service.ListFights();
            Assert.Single(rows);
            Assert.Equal(result.MatchId, rows[0].MatchId);
            Assert.Equal(result.Ticks / 60.0, rows[0].DurationSeconds);
            var summary = service.Summarize();
            Assert.Equal(1, summary.Count);
            Assert.Equal(result.MatchId, summary.LongestMatchId);
            Assert.Equal(result.MatchId, summary.ShortestMatchId);
            Assert.NotNull(service.GetLog(result.MatchId));

            var second = service.Backfill();
            Assert.Equal(1, second.Scanned);
            Assert.Equal(0, second.Updated);
            Assert.Equal(1, second.Skipped);
        }
        finally
        {
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, true);
            }
        }
    }

    private static void AppendAnalyzedFight(
        PlayerStore store,
        string matchId,
        BuildSnapshot attacker,
        BuildSnapshot defender,
        double seconds)
    {
        var ticks = (int)(seconds * 60);
        store.AppendFight("alice", new ArenaFightRecord
        {
            MatchId = matchId,
            Round = 1,
            Attacker = attacker,
            Defender = defender,
            EncounterSeed = 1,
            WinnerPlayerId = "alice",
            Ticks = ticks,
            CauseOfDeath = seconds > 60 ? "Blood loss" : seconds < 30 ? "Blood loss" : "Burning",
            FoughtAt = DateTimeOffset.UtcNow,
            Analytics = CombatAnalytics.From([], ticks, 1, 2)
        });
    }
}
