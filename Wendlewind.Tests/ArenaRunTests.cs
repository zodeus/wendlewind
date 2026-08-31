using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Wendlewind.Definitions;
using Wendlewind.NetCode;
using Wendlewind.NetCode.Contracts;
using Wendlewind.Sim;
using Wendlewind.Sim.Arena;
using Wendlewind.Sim.Combat;
using Wendlewind.Sim.Entities.Items;
using Xunit;

namespace Wendlewind.Tests;

[Collection("Sim")]
public class ArenaRunTests
{
    public ArenaRunTests()
    {
        TestData.EnsureLoaded();
    }

    [Fact]
    public void StartsWithThreeHundredGold()
    {
        using var scope = CreateArena();
        var context = scope.Context;
        Assert.Equal(ArenaRun.StartingGold, context.ArenaRun!.Gold);
        Assert.Equal(0, context.ArenaRun.Wins);
        Assert.Equal(0, context.ArenaRun.Losses);
        Assert.Equal(ArenaPhase.GeneralStore, context.ArenaRun.Phase);
        Assert.Equal(5, context.ArenaRun.LivesRemaining);
    }

    [Fact]
    public void WinAddsGoldAndIncrementsWins()
    {
        using var scope = CreateArena();
        var context = scope.Context;
        context.ArenaRun!.ApplyMatchResult(true, "opp-1");
        Assert.Equal(ArenaRun.StartingGold + ArenaRun.WinGold, context.ArenaRun.Gold);
        Assert.Equal(1, context.ArenaRun.Wins);
        Assert.Equal(0, context.ArenaRun.Losses);
        Assert.Equal(ArenaPhase.Results, context.ArenaRun.Phase);
    }

    [Fact]
    public void LossAddsGoldAndIncrementsLosses()
    {
        using var scope = CreateArena();
        var context = scope.Context;
        context.ArenaRun!.ApplyMatchResult(false, "opp-1");
        Assert.Equal(ArenaRun.StartingGold + ArenaRun.LoseGold, context.ArenaRun.Gold);
        Assert.Equal(0, context.ArenaRun.Wins);
        Assert.Equal(1, context.ArenaRun.Losses);
        Assert.Equal(4, context.ArenaRun.LivesRemaining);
    }

    [Fact]
    public void TenWinsEndsRunAsVictory()
    {
        using var scope = CreateArena();
        var context = scope.Context;
        for (var i = 0; i < ArenaRun.WinsToFinish; i++)
        {
            context.ArenaRun!.ApplyMatchResult(true, $"opp-{i}");
        }

        Assert.True(context.ArenaRun!.IsRunOver);
        Assert.True(context.ArenaRun.IsVictory);
        Assert.Equal(10, context.ArenaRun.Wins);
    }

    [Fact]
    public void FiveLossesEndsRunAsDefeat()
    {
        using var scope = CreateArena();
        var context = scope.Context;
        for (var i = 0; i < ArenaRun.LossesToFinish; i++)
        {
            context.ArenaRun!.ApplyMatchResult(false, $"opp-{i}");
        }

        Assert.True(context.ArenaRun!.IsRunOver);
        Assert.False(context.ArenaRun.IsVictory);
        Assert.Equal(5, context.ArenaRun.Losses);
        Assert.Equal(0, context.ArenaRun.LivesRemaining);
    }

    [Fact]
    public void TryBuyDeductsGoldAndAddsItem()
    {
        using var scope = CreateArena();
        var context = scope.Context;
        var merchant = DefRepository<MerchantDef>.GetByMoniker("GeneralStore")!;
        var offer = merchant.AllOffers.First(o => !o.IsSet && o.GoldCost <= context.ArenaRun!.Gold);
        var goldBefore = context.ArenaRun!.Gold;

        Assert.True(context.ArenaRun.TryBuy(context, offer));
        Assert.Equal(goldBefore - offer.GoldCost, context.ArenaRun.Gold);
        Assert.True(context.PlayerPawn.Inventory.Contains(offer.ItemDef!));
    }

    [Fact]
    public void TryBuyRejectsOverspend()
    {
        using var scope = CreateArena();
        var context = scope.Context;
        var expensive = new MerchantOffer
        {
            ItemDef = DefRepository<ItemDef>.GetByMoniker("IronSword")!,
            GoldCost = context.ArenaRun!.Gold + 1
        };

        Assert.False(context.ArenaRun.TryBuy(context, expensive));
        Assert.Equal(ArenaRun.StartingGold, context.ArenaRun.Gold);
        Assert.False(context.PlayerPawn.Inventory.Contains(expensive.ItemDef!));
    }

    [Fact]
    public void ArenaEncounterSeedIsStablePerRound()
    {
        Assert.Equal(ArenaSeeds.Encounter(99, 1), ArenaSeeds.Encounter(99, 1));
        Assert.NotEqual(ArenaSeeds.Encounter(99, 1), ArenaSeeds.Encounter(99, 2));
        Assert.NotEqual(ArenaSeeds.Encounter(99, 1), ArenaSeeds.Encounter(100, 1));
    }

    [Fact]
    public void ShopStockIsDeterministic()
    {
        var merchant = DefRepository<MerchantDef>.GetByMoniker("Blacksmith")!;
        var first = ShopStock.Flatten(ShopStock.Roll(merchant, 12345, 2));
        var second = ShopStock.Flatten(ShopStock.Roll(merchant, 12345, 2));
        Assert.Equal(first.Select(o => o.StockKey), second.Select(o => o.StockKey));
        Assert.Equal(merchant.Shelves.Select(s => s.Category), ShopStock.Roll(merchant, 12345, 2).Select(s => s.Category));
    }

    [Fact]
    public void ShopStockAlwaysIncludesUnlockedArmorSets()
    {
        var merchant = DefRepository<MerchantDef>.GetByMoniker("Blacksmith")!;
        var early = ShopStock.Roll(merchant, 99, 1).Single(shelf => shelf.Category == ShopCategory.Armor);
        Assert.Contains(early.Offers, offer => offer.SetLabel == "Leather Set");
        Assert.DoesNotContain(early.Offers, offer => offer.SetLabel == "Chain Set");

        var late = ShopStock.Roll(merchant, 99, 4).Single(shelf => shelf.Category == ShopCategory.Armor);
        Assert.Contains(late.Offers, offer => offer.SetLabel == "Leather Set");
        Assert.Contains(late.Offers, offer => offer.SetLabel == "Chain Set");
    }

    [Fact]
    public void ShopStockHidesLockedOffersUntilTheirRound()
    {
        var merchant = DefRepository<MerchantDef>.GetByMoniker("Blacksmith")!;
        Assert.DoesNotContain(ShopStock.AvailableOffers(merchant, 0), o => o.ItemDef?.Moniker == "FireStaff");
        Assert.Contains(ShopStock.AvailableOffers(merchant, 4), o => o.ItemDef?.Moniker == "FireStaff");
        Assert.Contains(ShopStock.AvailableOffers(merchant, 4), o => o.ItemDef?.Moniker == "IronDagger");
    }

    [Fact]
    public void LateShopStockStillIncludesEarlyOffers()
    {
        var merchant = DefRepository<MerchantDef>.GetByMoniker("Ranger")!;
        var seenEarly = false;
        var seenLate = false;
        for (var seed = 1; seed <= 40 && !(seenEarly && seenLate); seed++)
        {
            var ammo = ShopStock.Roll(merchant, seed, 6).Single(shelf => shelf.Category == ShopCategory.Ammo);
            seenEarly |= ammo.Offers.Any(o => o.ItemDef?.Moniker == "BoneDart");
            seenLate |= ammo.Offers.Any(o => o.ItemDef?.Moniker == "ExplosiveFang");
        }

        Assert.True(seenEarly);
        Assert.True(seenLate);
    }

    [Fact]
    public void AuthoredSetPricesAreTwentyPercentOffPieces()
    {
        foreach (var merchant in DefRepository<MerchantDef>.Defs)
        {
            foreach (var offer in merchant.AllOffers.Where(o => o.IsSet))
            {
                Assert.Equal(ShopCatalog.ComputeSetCost(offer.SetPieces, merchant), offer.GoldCost);
            }
        }
    }

    [Fact]
    public void TryBuySetAddsEveryPiece()
    {
        using var scope = CreateArena();
        var context = scope.Context;
        var merchant = DefRepository<MerchantDef>.GetByMoniker("GeneralStore")!;
        var set = merchant.AllOffers.First(o => o.IsSet && o.SetLabel == "Cloth Set");
        var goldBefore = context.ArenaRun!.Gold;

        Assert.True(context.ArenaRun.TryBuy(context, set));
        Assert.Equal(goldBefore - set.GoldCost, context.ArenaRun.Gold);
        foreach (var piece in set.SetPieces.Distinct())
        {
            Assert.True(context.PlayerPawn.Inventory.Contains(piece));
        }
    }

    [Fact]
    public void TryBuySetRejectsOverspendWithoutAddingPieces()
    {
        using var scope = CreateArena();
        var context = scope.Context;
        var merchant = DefRepository<MerchantDef>.GetByMoniker("Blacksmith")!;
        var set = merchant.AllOffers.First(o => o.IsSet && o.SetLabel == "Chain Set");
        var tooExpensive = new MerchantOffer
        {
            SetLabel = set.SetLabel,
            SetPieces = [..set.SetPieces],
            GoldCost = context.ArenaRun!.Gold + 1
        };

        Assert.False(context.ArenaRun.TryBuy(context, tooExpensive));
        Assert.Equal(ArenaRun.StartingGold, context.ArenaRun.Gold);
        Assert.False(context.PlayerPawn.Inventory.Contains(set.SetPieces[0]));
    }

    [Fact]
    public void TrySellPaysOneTenthAndRemovesItem()
    {
        using var scope = CreateArena();
        var context = scope.Context;
        var merchant = DefRepository<MerchantDef>.GetByMoniker("GeneralStore")!;
        context.ArenaRun!.CurrentMerchant = merchant;
        var offer = merchant.AllOffers.First(o => !o.IsSet && o.ItemDef!.Moniker == "WoodClub");
        Assert.True(context.ArenaRun.TryBuy(context, offer));
        var item = context.PlayerPawn.Inventory.First(i => i.ItemDef == offer.ItemDef);
        var goldBeforeSell = context.ArenaRun.Gold;

        Assert.True(context.ArenaRun.TrySell(context, item));
        Assert.Equal(goldBeforeSell + offer.GoldCost / 10, context.ArenaRun.Gold);
        Assert.False(context.PlayerPawn.Inventory.Contains(offer.ItemDef!));
    }

    [Fact]
    public void MidRunMapHasFourMerchantsAndNoWitchDoctor()
    {
        var midRun = DefRepository<MerchantDef>.Defs
            .Where(m => !m.IsGeneralStore)
            .Select(m => m.Moniker)
            .OrderBy(m => m)
            .ToArray();
        Assert.Equal(["Alchemist", "Blacksmith", "Magician", "Ranger"], midRun);
        Assert.DoesNotContain("WitchDoctor", DefRepository<MerchantDef>.Defs.Select(m => m.Moniker));
    }

    [Fact]
    public void ApplyToRemapsWitchDoctorToAlchemist()
    {
        using var scope = CreateArena();
        var run = scope.Context.ArenaRun!;
        var record = ArenaProgressMapper.FromRun(run, null, "remap", DateTimeOffset.UtcNow) with
        {
            CurrentMerchantMoniker = "WitchDoctor"
        };
        ArenaProgressMapper.ApplyTo(run, record);
        Assert.Equal("Alchemist", run.CurrentMerchant!.Moniker);
    }

    [Fact]
    public void RunDuelIsDeterministicForTemplates()
    {
        CombatReplay.AssertDuelDeterministic(
            CombatReplay.DefaultRunSeed,
            (_, player, enemy) =>
            {
                BuildSnapshotFactory.Apply(player, BuildTemplates.TankRegen());
                BuildSnapshotFactory.Apply(enemy, BuildTemplates.AcidRusher());
            });
    }

    [Fact]
    public void BuildPoolPicksAnyBuildInTheRoundIncludingSelf()
    {
        var path = Path.Combine(Path.GetTempPath(), $"wendlewind-pool-{Guid.NewGuid():N}.json");
        try
        {
            var pool = new BuildPool(path);
            var alice = BuildTemplates.TankRegen() with { PlayerId = "alice", Round = 1 };
            var bob = BuildTemplates.AcidRusher() with { PlayerId = "bob", Round = 1 };
            pool.Upsert(alice);
            pool.Upsert(bob);

            var opponent = pool.PickOpponent(1);
            Assert.NotNull(opponent);
            Assert.Contains(opponent.PlayerId, (string[])["alice", "bob"]);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Fact]
    public void BuildPoolPicksOwnBuildWhenAlone()
    {
        var pool = new BuildPool();
        pool.Upsert(BuildTemplates.TankRegen() with { PlayerId = "solo", BuildId = "arena-1", Round = 1 });
        var opponent = pool.PickOpponent(1);
        Assert.NotNull(opponent);
        Assert.Equal("solo", opponent.PlayerId);
        Assert.Equal("arena-1", opponent.BuildId);
    }

    [Fact]
    public void BuildPoolReturnsNullWhenRoundIsEmpty()
    {
        var pool = new BuildPool();
        pool.Upsert(BuildTemplates.TankRegen() with { PlayerId = "solo", Round = 1 });
        Assert.Null(pool.PickOpponent(2));
        Assert.Equal("mirror:solo", BuildPool.MirrorOf(pool.Get("solo")!).PlayerId);
    }

    [Fact]
    public void BuildPoolRegistersEverySubmitInTheSameRound()
    {
        var pool = new BuildPool();
        pool.Upsert(BuildTemplates.TankRegen() with { PlayerId = "alice", BuildId = "arena-1", Round = 1 });
        pool.Upsert(BuildTemplates.AcidRusher() with { PlayerId = "alice", BuildId = "arena-1b", Round = 1 });
        Assert.Equal(2, pool.Count);
        Assert.Equal("alice", pool.PickOpponent(1)!.PlayerId);

        pool.Upsert(BuildTemplates.Glasscannon() with { PlayerId = "bob", BuildId = "arena-1", Round = 1 });
        Assert.Equal(3, pool.Count);
        Assert.Contains(pool.PickOpponent(1)!.PlayerId, (string[])["alice", "bob"]);
    }

    [Fact]
    public void BuildPoolMatchesOnlyTheSameRound()
    {
        var pool = new BuildPool();
        pool.Upsert(BuildTemplates.TankRegen() with { PlayerId = "alice", Round = 1 });
        pool.Upsert(BuildTemplates.AcidRusher() with { PlayerId = "bob", Round = 2 });

        Assert.Equal("alice", pool.PickOpponent(1)!.PlayerId);
        Assert.Equal("bob", pool.PickOpponent(2)!.PlayerId);
        Assert.Null(pool.PickOpponent(3));
    }

    [Fact]
    public void SnapshotRoundTripKeepsUnequippedPurchases()
    {
        using var scope = CreateArena();
        var pawn = scope.Context.PlayerPawn;
        var axe = scope.Context.Factory.CreateEntity<Item>(DefRepository<ItemDef>.GetByMoniker("BoneAxe")!, 1);
        var helm = scope.Context.Factory.CreateEntity<Item>(DefRepository<ItemDef>.GetByMoniker("LeatherHelmet")!, 1);
        Assert.True(pawn.Inventory.TryAdd(axe));
        Assert.True(pawn.Inventory.TryAdd(helm));

        var snapshot = BuildSnapshotFactory.ToSnapshot(pawn, "p", "arena-1", 1, round: 1);
        Assert.Contains(snapshot.Inventory, stack => stack.ItemMoniker == "BoneAxe");
        Assert.Contains(snapshot.Inventory, stack => stack.ItemMoniker == "LeatherHelmet");

        using var restored = CreateArena();
        BuildSnapshotFactory.Apply(restored.Context.PlayerPawn, snapshot);
        Assert.True(restored.Context.PlayerPawn.Inventory.Contains(DefRepository<ItemDef>.GetByMoniker("BoneAxe")!));
        Assert.True(restored.Context.PlayerPawn.Inventory.Contains(DefRepository<ItemDef>.GetByMoniker("LeatherHelmet")!));
    }

    [Fact]
    public void ToSnapshotCapturesPawnDefAndSubmittedAt()
    {
        using var scope = CreateArena();
        var snapshot = BuildSnapshotFactory.ToSnapshot(scope.Context.PlayerPawn, "p", "arena-1", 1, round: 3);
        Assert.Equal("HumanA", snapshot.PawnDefMoniker);
        Assert.Equal(3, snapshot.Round);
        Assert.False(string.IsNullOrWhiteSpace(snapshot.PawnName));
        Assert.NotNull(snapshot.SubmittedAt);
    }

    [Fact]
    public void DuelSimulatorReturnsDefenderSnapshot()
    {
        var attacker = BuildTemplates.TankRegen() with { PlayerId = "a" };
        var defender = BuildTemplates.AcidRusher() with { PlayerId = "b" };
        var result = DuelSimulator.Run(attacker, defender, CombatReplay.DefaultRunSeed);
        Assert.False(string.IsNullOrEmpty(result.MatchId));
        Assert.Equal("b", result.DefenderPlayerId);
        Assert.NotNull(result.Defender);
        Assert.True(result.WinnerPlayerId is "a" or "b");
        Assert.True(result.Ticks > 0);
    }

    [Fact]
    public void MirrorDuelDistinguishesWinnerFromSelf()
    {
        var attacker = BuildTemplates.TankRegen() with { PlayerId = "solo" };
        var defender = BuildPool.MirrorOf(attacker);
        var result = DuelSimulator.Run(attacker, defender, CombatReplay.DefaultRunSeed);
        Assert.Equal("mirror:solo", result.DefenderPlayerId);
        Assert.True(result.WinnerPlayerId is "solo" or "mirror:solo");
    }

    [Fact]
    public void CombatResultReadsAspNetCamelCase()
    {
        const string json =
            """{"matchId":"abc","winnerPlayerId":"alice","ticks":12,"causeOfDeath":"bleed","defenderPlayerId":"bob","encounterSeed":9}""";

        var result = JsonSerializer.Deserialize(json, NetCodeJsonContext.Default.CombatResult);
        Assert.NotNull(result);
        Assert.Equal("abc", result.MatchId);
        Assert.Equal("alice", result.WinnerPlayerId);
        Assert.Equal(12, result.Ticks);
        Assert.Equal("bob", result.DefenderPlayerId);
        Assert.Equal(9, result.EncounterSeed);
    }

    private static ArenaContextScope CreateArena()
    {
        return new ArenaContextScope();
    }

    private sealed class ArenaContextScope : IDisposable
    {
        private readonly ServiceProvider _root = SimServices.BuildRoot();
        private readonly IServiceScope _scope;

        public GameContext Context { get; }

        public ArenaContextScope()
        {
            _scope = _root.CreateScope();
            Context = _scope.ServiceProvider.GetRequiredService<GameContext>();
            Context.InitializeArena("tester", "Tester", 99);
        }

        public void Dispose()
        {
            _scope.Dispose();
            _root.Dispose();
        }
    }
}
