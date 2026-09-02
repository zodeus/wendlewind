using System.Text.Json;
using Wendlemire.Definitions;
using Wendlemire.NetCode;
using Wendlemire.NetCode.Contracts;
using Wendlemire.Sim.Achievements;
using Wendlemire.Sim.Arena;
using Wendlemire.Sim.Combat;
using Xunit;

namespace Wendlemire.Tests;

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
            Assert.Equal(GameVersion.Current, started.Version);
            store.SaveCurrentArena(started with
            {
                Gold = 400,
                Wins = 2,
                Loadout = BuildTemplates.TankRegen() with { PlayerId = "alice", Round = 1 }
            });
            Assert.Equal(400, store.GetCurrentArena("alice")!.Gold);
            Assert.Equal(GameVersion.Current, store.GetCurrentArena("alice")!.Version);
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
            Assert.Equal(GameVersion.Current, run.Version);
            Assert.Equal(GameVersion.Current, run.Fights[0].Version);
            var storedLog = store.GetCombatLog("alice", started.RunId, result.MatchId);
            Assert.NotNull(storedLog);
            Assert.Equal(GameVersion.Current, storedLog.Version);
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

            var verified = new FightAnalyticsService(store).VerifyRecorded();
            Assert.Equal(1, verified.Scanned);
            Assert.Equal(1, verified.Matched);
            Assert.Empty(verified.Mismatches);

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
    public void ListsPlayersAndAdminOverview()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"ww-data-{Guid.NewGuid():N}");
        try
        {
            var store = new PlayerStore(dir);
            store.GetOrCreateProfile("alice", "Alice", "alice");
            store.GetOrCreateProfile("bob", "Bob", "bob");
            var started = store.StartArena("alice", "Alice", 7);
            store.SaveCurrentArena(started with { Gold = 250, Wins = 1, Phase = "Blacksmith" });

            var players = store.ListPlayers();
            Assert.Equal(2, players.Count);
            var alice = players.Single(player => player.PlayerId == "alice");
            Assert.Equal("Alice", alice.DisplayName);
            Assert.True(alice.HasActiveArena);
            Assert.Equal("Blacksmith", alice.ActivePhase);
            Assert.Equal(250, alice.ActiveGold);
            Assert.Equal(1, alice.RunCount);
            Assert.False(players.Single(player => player.PlayerId == "bob").HasActiveArena);

            var detail = store.GetPlayerDetail("alice");
            Assert.NotNull(detail);
            Assert.Equal(started.RunId, detail.CurrentArena!.RunId);
            Assert.Single(detail.Runs);
            Assert.True(detail.Runs[0].IsActive);

            var overview = store.SummarizeAdmin(3, [new AdminPoolRound { Round = 1, Builds = 3 }], new FightAnalyticsSummary());
            Assert.Equal(2, overview.Players);
            Assert.Equal(1, overview.ActiveArenas);
            Assert.Equal(1, overview.Runs);
            Assert.Equal(3, overview.PoolBuilds);
            Assert.Single(overview.ActivePlayers);

            Assert.True(store.DeletePlayer("alice"));
            Assert.Null(store.GetProfile("alice"));
            Assert.Null(store.GetPlayerDetail("alice"));
            Assert.False(store.DeletePlayer("alice"));
            Assert.Single(store.ListPlayers());
            Assert.Equal("bob", store.ListPlayers()[0].PlayerId);
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
            Assert.Equal("Alice", rows[0].PlayerName);
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

    [Fact]
    public void FinishedRunWithRealOpponentChangesRating()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"ww-data-{Guid.NewGuid():N}");
        try
        {
            var store = new PlayerStore(dir);
            var started = store.StartArena("alice", "Alice", 1);
            Assert.Equal(ArenaRank.StartingRating, store.GetProfile("alice")!.Rating);

            store.SaveCurrentArena(started with { Wins = 7, Losses = 5 });
            store.AppendFight("alice", new ArenaFightRecord
            {
                MatchId = "rank-fight",
                Round = 1,
                Attacker = BuildTemplates.TankRegen() with { PlayerId = "alice" },
                Defender = BuildTemplates.AcidRusher() with { PlayerId = "bob" },
                EncounterSeed = 1,
                WinnerPlayerId = "alice",
                Ticks = 1800,
                FoughtAt = DateTimeOffset.UtcNow
            });

            var finished = store.FinishCurrent("alice", victory: false);
            Assert.True(finished!.RankApplied);
            Assert.Equal(ArenaRank.StartingRating, finished.RatingBefore);
            Assert.True(finished.RatingAfter > ArenaRank.StartingRating);
            Assert.Equal(finished.RatingAfter, store.GetProfile("alice")!.Rating);
            Assert.Equal(1, store.GetProfile("alice")!.RatedRuns);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void AbandonedAndMirrorRunsDoNotChangeRating()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"ww-data-{Guid.NewGuid():N}");
        try
        {
            var store = new PlayerStore(dir);
            store.StartArena("alice", "Alice", 1);
            store.StartArena("alice", "Alice", 1);
            Assert.Equal(ArenaRank.StartingRating, store.GetProfile("alice")!.Rating);
            Assert.Equal(0, store.GetProfile("alice")!.RatedRuns);

            var mirror = store.StartArena("alice", "Alice", 2);
            store.SaveCurrentArena(mirror with { Wins = 10, Losses = 0 });
            store.AppendFight("alice", new ArenaFightRecord
            {
                MatchId = "mirror-fight",
                Round = 1,
                Attacker = BuildTemplates.TankRegen() with { PlayerId = "alice" },
                Defender = BuildTemplates.TankRegen() with { PlayerId = "mirror:alice" },
                EncounterSeed = 1,
                WinnerPlayerId = "alice",
                Ticks = 1800,
                FoughtAt = DateTimeOffset.UtcNow
            });

            var finished = store.FinishCurrent("alice", victory: true);
            Assert.False(finished!.RankApplied);
            Assert.Equal(ArenaRank.StartingRating, store.GetProfile("alice")!.Rating);
            Assert.Equal(0, store.GetProfile("alice")!.RatedRuns);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void FightRowsIncludeUsernamesForPlayerAndWinner()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"ww-data-{Guid.NewGuid():N}");
        try
        {
            var store = new PlayerStore(dir);
            store.GetOrCreateProfile("alice", "Alice", "lunch_box77");
            store.GetOrCreateProfile("bob", "Bob", "ButtersFROMNutters");
            store.StartArena("alice", "Alice", 1);
            store.AppendFight("alice", new ArenaFightRecord
            {
                MatchId = "named-fight",
                Round = 2,
                Attacker = BuildTemplates.TankRegen() with { PlayerId = "alice", PawnName = "Alice" },
                Defender = BuildTemplates.AcidRusher() with { PlayerId = "bob", PawnName = "Bob" },
                EncounterSeed = 1,
                WinnerPlayerId = "bob",
                Ticks = 600,
                FoughtAt = DateTimeOffset.Parse("2026-09-01T20:00:00Z")
            });
            store.AppendFight("alice", new ArenaFightRecord
            {
                MatchId = "mirror-named-fight",
                Round = 3,
                Attacker = BuildTemplates.TankRegen() with { PlayerId = "alice", PawnName = "Alice" },
                Defender = BuildTemplates.TankRegen() with { PlayerId = "mirror:alice", PawnName = "Alice" },
                EncounterSeed = 2,
                WinnerPlayerId = "mirror:alice",
                Ticks = 900,
                FoughtAt = DateTimeOffset.Parse("2026-09-01T21:00:00Z")
            });

            var listed = new FightAnalyticsService(store).ListFights();
            Assert.Equal(["mirror-named-fight", "named-fight"], listed.Select(row => row.MatchId).ToArray());
            var rows = listed.ToDictionary(row => row.MatchId);
            Assert.Equal("lunch_box77", rows["named-fight"].PlayerName);
            Assert.Equal("ButtersFROMNutters", rows["named-fight"].WinnerName);
            Assert.Equal("bob", rows["named-fight"].OpponentPlayerId);
            Assert.Equal("ButtersFROMNutters", rows["named-fight"].OpponentName);
            Assert.NotEqual(default, rows["named-fight"].FoughtAt);
            Assert.Equal("lunch_box77", rows["mirror-named-fight"].PlayerName);
            Assert.Equal("mirror:lunch_box77", rows["mirror-named-fight"].WinnerName);
            Assert.Equal("mirror:alice", rows["mirror-named-fight"].OpponentPlayerId);
            Assert.Equal("mirror:lunch_box77", rows["mirror-named-fight"].OpponentName);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void VerifyRecordedFightsFlagsAWrongWinner()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"ww-data-{Guid.NewGuid():N}");
        try
        {
            var store = new PlayerStore(dir);
            store.GetOrCreateProfile("alice", "Alice", "alice");
            store.StartArena("alice", "Alice", 1);
            var attacker = BuildTemplates.TankRegen() with { PlayerId = "alice" };
            var defender = BuildTemplates.AcidRusher() with { PlayerId = "bob" };
            var honest = DuelSimulator.Run(attacker, defender, CombatReplay.DefaultRunSeed);
            var fakeWinner = honest.WinnerPlayerId == "alice" ? "bob" : "alice";
            store.AppendFight("alice", new ArenaFightRecord
            {
                MatchId = honest.MatchId,
                Round = 1,
                Attacker = attacker,
                Defender = defender,
                EncounterSeed = honest.EncounterSeed,
                WinnerPlayerId = fakeWinner,
                Ticks = honest.Ticks,
                CauseOfDeath = honest.CauseOfDeath,
                FoughtAt = DateTimeOffset.UtcNow
            });

            var verified = new FightAnalyticsService(store).VerifyRecorded();
            var mismatch = Assert.Single(verified.Mismatches);
            Assert.Equal(honest.MatchId, mismatch.MatchId);
            Assert.Equal(fakeWinner, mismatch.RecordedWinner);
            Assert.Equal(honest.WinnerPlayerId, mismatch.ReplayedWinner);
            Assert.Equal(honest.Ticks, mismatch.ReplayedTicks);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void NewProfilesOwnAndEquipDefaultNamePlate()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"ww-data-{Guid.NewGuid():N}");
        try
        {
            var store = new PlayerStore(dir);
            var profile = store.GetOrCreateProfile("alice", "Alice", "alice");
            Assert.Equal(0, profile.Marks);
            Assert.Contains(ArenaMarks.DefaultNamePlate, profile.OwnedCosmeticMonikers);
            Assert.Equal(ArenaMarks.DefaultNamePlate, profile.EquippedNamePlate);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void NewlyUnlockedAchievementsAwardMarksOnce()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"ww-data-{Guid.NewGuid():N}");
        try
        {
            var store = new PlayerStore(dir);
            store.GetOrCreateProfile("alice", "Alice", "alice");

            store.SaveAchievements("alice", new AchievementState
            {
                Achievements =
                [
                    new AchievementRecord { Moniker = "FirstBlood", CurrentValue = 1, IsUnlocked = true },
                    new AchievementRecord { Moniker = "BarTender", CurrentValue = 20, IsUnlocked = true }
                ]
            });
            Assert.Equal(AchievementRewards.MarksPerUnlock * 2, store.GetProfile("alice")!.Marks);

            store.SaveAchievements("alice", new AchievementState
            {
                Achievements =
                [
                    new AchievementRecord { Moniker = "FirstBlood", CurrentValue = 1, IsUnlocked = true },
                    new AchievementRecord { Moniker = "BarTender", CurrentValue = 20, IsUnlocked = true },
                    new AchievementRecord { Moniker = "CaveDiver", CurrentValue = 15, IsUnlocked = true }
                ]
            });
            Assert.Equal(AchievementRewards.MarksPerUnlock * 3, store.GetProfile("alice")!.Marks);

            store.SaveAchievements("alice", new AchievementState
            {
                Achievements =
                [
                    new AchievementRecord { Moniker = "FirstBlood", CurrentValue = 1, IsUnlocked = true },
                    new AchievementRecord { Moniker = "BarTender", CurrentValue = 20, IsUnlocked = true },
                    new AchievementRecord { Moniker = "CaveDiver", CurrentValue = 15, IsUnlocked = true }
                ]
            });
            Assert.Equal(AchievementRewards.MarksPerUnlock * 3, store.GetProfile("alice")!.Marks);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void FinishedTenWinRunAwardsMarksFormula()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"ww-data-{Guid.NewGuid():N}");
        try
        {
            var store = new PlayerStore(dir);
            var started = store.StartArena("alice", "Alice", 1);
            store.SaveCurrentArena(started with { Wins = 10, Losses = 0, Gold = 320 });
            var finished = store.FinishCurrent("alice");
            Assert.Equal(182, finished!.MarksAwarded);
            Assert.Equal(182, store.GetProfile("alice")!.Marks);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void FiveLossFinishAwardsWinsAndLeftoverGoldWithoutVictoryBonus()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"ww-data-{Guid.NewGuid():N}");
        try
        {
            var store = new PlayerStore(dir);
            var started = store.StartArena("alice", "Alice", 1);
            store.SaveCurrentArena(started with { Wins = 3, Losses = 5, Gold = 90 });
            var finished = store.FinishCurrent("alice");
            Assert.Equal(39, finished!.MarksAwarded);
            Assert.Equal(39, store.GetProfile("alice")!.Marks);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void AbandonedAndUnfinishedRunsAwardNoMarks()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"ww-data-{Guid.NewGuid():N}");
        try
        {
            var store = new PlayerStore(dir);
            var first = store.StartArena("alice", "Alice", 1);
            store.SaveCurrentArena(first with { Wins = 10, Losses = 0, Gold = 500 });
            store.StartArena("alice", "Alice", 2);
            Assert.Equal(0, store.GetRun("alice", first.RunId)!.MarksAwarded);
            Assert.Equal(0, store.GetProfile("alice")!.Marks);

            var unfinished = store.GetCurrentArena("alice")!;
            store.SaveCurrentArena(unfinished with { Wins = 3, Losses = 2, Gold = 90 });
            var finished = store.FinishCurrent("alice", victory: true);
            Assert.Equal(0, finished!.MarksAwarded);
            Assert.Equal(0, store.GetProfile("alice")!.Marks);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void BuyAndEquipNamePlates()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"ww-data-{Guid.NewGuid():N}");
        try
        {
            var store = new PlayerStore(dir);
            store.GetOrCreateProfile("alice", "Alice", "alice");

            var tooPoor = store.BuyCosmetic("alice", "BoneInlay");
            Assert.False(tooPoor.Ok);
            Assert.Equal("Not enough marks.", tooPoor.Error);

            var started = store.StartArena("alice", "Alice", 1);
            store.SaveCurrentArena(started with { Wins = 10, Losses = 0, Gold = 320 });
            store.FinishCurrent("alice");
            Assert.Equal(182, store.GetProfile("alice")!.Marks);

            var alreadyOwned = store.BuyCosmetic("alice", ArenaMarks.DefaultNamePlate);
            Assert.False(alreadyOwned.Ok);
            Assert.Equal("Already owned.", alreadyOwned.Error);

            var unknown = store.BuyCosmetic("alice", "DoesNotExist");
            Assert.False(unknown.Ok);
            Assert.Equal("Unknown cosmetic.", unknown.Error);

            var bought = store.BuyCosmetic("alice", "BoneInlay");
            Assert.True(bought.Ok);
            Assert.Equal(32, bought.Profile!.Marks);
            Assert.Contains("BoneInlay", bought.Profile.OwnedCosmeticMonikers);
            Assert.Equal(ArenaMarks.DefaultNamePlate, bought.Profile.EquippedNamePlate);

            var again = store.BuyCosmetic("alice", "BoneInlay");
            Assert.False(again.Ok);
            Assert.Equal("Already owned.", again.Error);

            var notOwned = store.EquipCosmetic("alice", "GildedRuin");
            Assert.False(notOwned.Ok);
            Assert.Equal("Not owned.", notOwned.Error);

            var equipped = store.EquipCosmetic("alice", "BoneInlay");
            Assert.True(equipped.Ok);
            Assert.Equal("BoneInlay", equipped.Profile!.EquippedNamePlate);

            var stamped = store.StampCosmetics(BuildTemplates.TankRegen() with { PlayerId = "alice" });
            Assert.Equal("BoneInlay", stamped.NamePlateMoniker);
        }
        finally
        {
            Directory.Delete(dir, true);
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
