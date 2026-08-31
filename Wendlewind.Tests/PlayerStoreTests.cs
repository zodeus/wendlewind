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

            var run = store.GetRun("alice", started.RunId);
            Assert.NotNull(run);
            Assert.Single(run.Fights);
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
                ShopStock.Roll(merchant, first.RunSeed, 0).Select(offer => offer.ItemDef.Moniker),
                ShopStock.Roll(merchant, second.RunSeed, 0).Select(offer => offer.ItemDef.Moniker));
        }
        finally
        {
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, true);
            }
        }
    }
}
