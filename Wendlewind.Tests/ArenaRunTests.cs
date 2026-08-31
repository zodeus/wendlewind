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
        Assert.Equal(ArenaPhase.RunEnd, context.ArenaRun.Phase);
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
        Assert.Equal(ArenaPhase.RunEnd, context.ArenaRun.Phase);
    }

    [Fact]
    public void TryBuyDeductsGoldAndAddsItem()
    {
        using var scope = CreateArena();
        var context = scope.Context;
        var merchant = DefRepository<MerchantDef>.GetByMoniker("GeneralStore")!;
        var offer = merchant.AllOffers.First(o => !o.IsSet && o.ResolveGoldCost() <= context.ArenaRun!.Gold);
        var goldBefore = context.ArenaRun!.Gold;

        Assert.True(context.ArenaRun.TryBuy(context, offer));
        Assert.Equal(goldBefore - offer.ResolveGoldCost(), context.ArenaRun.Gold);
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
    public void FoodAndSingleUseMedicalOfferBulkBuy()
    {
        var food = new MerchantOffer { ItemDef = DefRepository<ItemDef>.GetByMoniker("CookedCorn")! };
        var medKit = new MerchantOffer { ItemDef = DefRepository<ItemDef>.GetByMoniker("MedKit")! };
        var cauterize = new MerchantOffer { ItemDef = DefRepository<ItemDef>.GetByMoniker("Cauterize")! };
        var sword = new MerchantOffer { ItemDef = DefRepository<ItemDef>.GetByMoniker("IronSword")! };
        Assert.False(food.OffersBulkBuy);
        Assert.True(medKit.OffersBulkBuy);
        Assert.False(cauterize.OffersBulkBuy);
        Assert.False(sword.OffersBulkBuy);
    }

    [Fact]
    public void TryBuyQuantityAddsStackAndChargesMultiple()
    {
        using var scope = CreateArena();
        var context = scope.Context;
        var offer = new MerchantOffer { ItemDef = DefRepository<ItemDef>.GetByMoniker("MedKit")! };
        context.ArenaRun!.Gold = offer.ResolveGoldCost() * MerchantOffer.BulkBuyQuantity + 50;
        var goldBefore = context.ArenaRun.Gold;

        Assert.True(context.ArenaRun.TryBuy(context, offer, MerchantOffer.BulkBuyQuantity));
        Assert.Equal(goldBefore - offer.ResolveGoldCost() * MerchantOffer.BulkBuyQuantity, context.ArenaRun.Gold);
        Assert.Equal(MerchantOffer.BulkBuyQuantity, context.PlayerPawn.Inventory.AmountOf(offer.ItemDef!));
    }

    [Fact]
    public void TryBuyQuantityRejectsOverspendWithoutAddingItems()
    {
        using var scope = CreateArena();
        var context = scope.Context;
        var offer = new MerchantOffer { ItemDef = DefRepository<ItemDef>.GetByMoniker("MedKit")! };
        context.ArenaRun!.Gold = offer.ResolveGoldCost() * (MerchantOffer.BulkBuyQuantity - 1);

        Assert.False(context.ArenaRun.TryBuy(context, offer, MerchantOffer.BulkBuyQuantity));
        Assert.Equal(offer.ResolveGoldCost() * (MerchantOffer.BulkBuyQuantity - 1), context.ArenaRun.Gold);
        Assert.Equal(0, context.PlayerPawn.Inventory.AmountOf(offer.ItemDef!));
    }

    [Fact]
    public void FoodAndIncenseAreNonStackable()
    {
        Assert.Equal(1, DefRepository<ItemDef>.GetByMoniker("CookedCorn")!.StackLimit);
        Assert.Equal(1, DefRepository<ItemDef>.GetByMoniker("MullinStick")!.StackLimit);
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
    public void ShopShelfPoolsExceedStockSize()
    {
        foreach (var merchant in DefRepository<MerchantDef>.Defs)
        {
            foreach (var shelf in merchant.Shelves)
            {
                var setCount = shelf.Offers.Count(offer => offer.IsSet);
                var pieceCount = shelf.Offers.Count(offer => !offer.IsSet);
                var rolledSlots = Math.Max(0, shelf.StockSize - setCount);
                Assert.True(
                    pieceCount > rolledSlots,
                    $"{merchant.Moniker} {shelf.Category}: {pieceCount} pieces for {rolledSlots} rolled slots");
            }
        }
    }

    [Fact]
    public void ShopStockVariesAcrossSeeds()
    {
        var merchant = DefRepository<MerchantDef>.GetByMoniker("GeneralStore")!;
        var distinct = Enumerable.Range(1, 16)
            .Select(seed => string.Join(",", ShopStock.Flatten(ShopStock.Roll(merchant, seed, 0)).Select(o => o.StockKey)))
            .Distinct()
            .Count();
        Assert.True(distinct > 1);
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
    public void SetPricesAreTwentyPercentOffPieceGoldCosts()
    {
        foreach (var merchant in DefRepository<MerchantDef>.Defs)
        {
            foreach (var offer in merchant.AllOffers.Where(o => o.IsSet))
            {
                Assert.All(offer.SetPieces, piece => Assert.True(piece.GoldCost > 0));
                Assert.Equal(ShopCatalog.ComputeSetCost(offer.SetPieces), offer.ResolveGoldCost());
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
        context.ArenaRun!.Gold = Math.Max(context.ArenaRun.Gold, set.ResolveGoldCost());
        var goldBefore = context.ArenaRun.Gold;

        Assert.True(context.ArenaRun.TryBuy(context, set));
        Assert.Equal(goldBefore - set.ResolveGoldCost(), context.ArenaRun.Gold);
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
        var offer = merchant.AllOffers.First(o => !o.IsSet && o.ItemDef!.Moniker == "CookedCorn");
        Assert.True(context.ArenaRun.TryBuy(context, offer));
        var item = context.PlayerPawn.Inventory.First(i => i.ItemDef == offer.ItemDef);
        var goldBeforeSell = context.ArenaRun.Gold;

        Assert.True(context.ArenaRun.TrySell(context, item));
        Assert.Equal(goldBeforeSell + offer.ResolveGoldCost() / 10, context.ArenaRun.Gold);
        Assert.False(context.PlayerPawn.Inventory.Contains(offer.ItemDef!));
    }

    [Fact]
    public void GeneralStoreUsesTwelveColumnShelfRows()
    {
        var merchant = DefRepository<MerchantDef>.GetByMoniker("GeneralStore")!;
        var rows = ShopLayout.GroupRows(merchant.Shelves, shelf => shelf.Columns);
        Assert.Equal(
        [
            [ShopCategory.Food, ShopCategory.Incense],
            [ShopCategory.Weapons, ShopCategory.Armor],
            [ShopCategory.Medicine, ShopCategory.Potions]
        ], rows.Select(row => row.Select(shelf => shelf.Category).ToArray()).ToArray());
        Assert.Equal(2, merchant.Shelves[0].StockSize);
        Assert.Equal(1, merchant.Shelves[0].ItemColumns);
        Assert.All(rows, row => Assert.Equal(ShopLayout.GridColumns, row.Sum(shelf => shelf.ResolvedColumns)));
    }

    [Fact]
    public void ShopRowsUseSpansThatFillTwelveColumns()
    {
        foreach (var merchant in DefRepository<MerchantDef>.Defs)
        {
            foreach (var shelf in merchant.Shelves)
            {
                Assert.Equal(1, shelf.ResolvedItemColumns);
                Assert.Equal(0, ShopLayout.GridColumns % shelf.ResolvedColumns);
                Assert.Equal(0, shelf.ResolvedColumns % shelf.ResolvedItemColumns);
            }

            foreach (var row in ShopLayout.GroupRows(merchant.Shelves, shelf => shelf.Columns))
            {
                Assert.Equal(ShopLayout.GridColumns, row.Sum(shelf => shelf.ResolvedColumns));
            }
        }
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
    public void BuildPoolPrefersOtherPlayersOverSelf()
    {
        var path = Path.Combine(Path.GetTempPath(), $"wendlewind-pool-{Guid.NewGuid():N}.json");
        try
        {
            var pool = new BuildPool(path);
            pool.Upsert(BuildTemplates.TankRegen() with { PlayerId = "alice", Round = 1 });
            pool.Upsert(BuildTemplates.AcidRusher() with { PlayerId = "bob", Round = 1 });

            for (var i = 0; i < 20; i++)
            {
                Assert.Equal("bob", pool.PickOpponent(1, "alice")!.PlayerId);
            }
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
    public void BuildPoolFallsBackToOtherRoundsWhenAloneThisRound()
    {
        var pool = new BuildPool();
        pool.Upsert(BuildTemplates.TankRegen() with { PlayerId = "alice", Round = 1 });
        pool.Upsert(BuildTemplates.AcidRusher() with { PlayerId = "bob", Round = 2 });

        Assert.Equal("alice", pool.PickOpponent(2, "bob")!.PlayerId);
    }

    [Fact]
    public void BuildPoolPicksOwnBuildWhenAlone()
    {
        var pool = new BuildPool();
        pool.Upsert(BuildTemplates.TankRegen() with { PlayerId = "solo", BuildId = "arena-1", Round = 1 });
        var opponent = pool.PickOpponent(1, "solo");
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
    public void MealAndIncenseRemainAfterSnapshotRestore()
    {
        using var scope = CreateArena();
        var pawn = scope.Context.PlayerPawn;
        var meatDef = DefRepository<ItemDef>.GetByMoniker("DriedMeat")!;
        var incenseDef = DefRepository<ItemDef>.GetByMoniker("MullinStick")!;
        var meat = scope.Context.Factory.CreateEntity<Item>(meatDef, 1);
        var incense = scope.Context.Factory.CreateEntity<Item>(incenseDef, 1);
        Assert.True(pawn.Inventory.TryAdd(meat));
        Assert.True(pawn.Inventory.TryAdd(incense));
        Assert.True(pawn.MealPlan.TryAdd(meat));
        Assert.True(pawn.TryLightIncense(incense, requireFlameStick: false));
        Assert.Equal(1, pawn.Inventory.AmountOf(incenseDef));

        var snapshot = BuildSnapshotFactory.ToSnapshot(pawn, "p", "arena-1", 1, round: 1);
        using var restored = CreateArena();
        var restoredPawn = restored.Context.PlayerPawn;
        BuildSnapshotFactory.Apply(restoredPawn, snapshot);

        Assert.Equal(1, restoredPawn.Inventory.AmountOf(meatDef));
        Assert.Equal(1, restoredPawn.Inventory.AmountOf(incenseDef));
        Assert.Single(restoredPawn.MealPlan.Items);
        Assert.Single(restoredPawn.ActiveIncense);
        Assert.Equal(2, restoredPawn.ActiveIncense[0].EncountersRemaining);
    }

    [Fact]
    public void LightingIncenseDoesNotDestroyTheStick()
    {
        using var scope = CreateArena();
        var pawn = scope.Context.PlayerPawn;
        var incenseDef = DefRepository<ItemDef>.GetByMoniker("MullinStick")!;
        var incense = scope.Context.Factory.CreateEntity<Item>(incenseDef, 1);
        Assert.True(pawn.Inventory.TryAdd(incense));
        Assert.True(pawn.TryLightIncense(incense, requireFlameStick: false));
        Assert.Equal(1, pawn.Inventory.AmountOf(incenseDef));
        Assert.False(pawn.CanLightIncense(incense, requireFlameStick: false));
    }

    [Fact]
    public void BattleStartDoesNotConsumeMealFood()
    {
        using var scope = CreateArena();
        var pawn = scope.Context.PlayerPawn;
        var meatDef = DefRepository<ItemDef>.GetByMoniker("DriedMeat")!;
        var meat = scope.Context.Factory.CreateEntity<Item>(meatDef, 1);
        Assert.True(pawn.Inventory.TryAdd(meat));
        Assert.True(pawn.MealPlan.TryAdd(meat));
        pawn.ApplyBattleStartConsumables();
        Assert.Equal(1, pawn.Inventory.AmountOf(meatDef));
        Assert.Single(pawn.MealPlan.Items);
    }

    [Fact]
    public void ShopRollOmitsOwnedFoodAndIncense()
    {
        var merchant = DefRepository<MerchantDef>.GetByMoniker("GeneralStore")!;
        var owned = new HashSet<string> { "CookedCorn", "MullinStick" };
        for (var seed = 1; seed <= 40; seed++)
        {
            var offers = ShopStock.Flatten(ShopStock.Roll(merchant, seed, 0, owned));
            Assert.DoesNotContain(offers, o => o.ItemDef?.Moniker == "CookedCorn");
            Assert.DoesNotContain(offers, o => o.ItemDef?.Moniker == "MullinStick");
        }
    }

    [Fact]
    public void TrinketsAreUniqueOwnedTypes()
    {
        var trinket = new MerchantOffer { ItemDef = DefRepository<ItemDef>.GetByMoniker("FlameStick")! };
        var food = new MerchantOffer { ItemDef = DefRepository<ItemDef>.GetByMoniker("CookedCorn")! };
        var sword = new MerchantOffer { ItemDef = DefRepository<ItemDef>.GetByMoniker("IronSword")! };
        Assert.True(trinket.IsUniqueOwnedType);
        Assert.True(food.IsUniqueOwnedType);
        Assert.False(sword.IsUniqueOwnedType);
    }

    [Fact]
    public void ShopRollOmitsOwnedOrFoundTrinkets()
    {
        var merchant = DefRepository<MerchantDef>.GetByMoniker("Alchemist")!;
        var owned = new HashSet<string> { "FlameStick" };
        for (var seed = 1; seed <= 40; seed++)
        {
            var offers = ShopStock.Flatten(ShopStock.Roll(merchant, seed, 0, owned));
            Assert.DoesNotContain(offers, o => o.ItemDef?.Moniker == "FlameStick");
        }

        using var scope = CreateArena();
        var stick = scope.Context.Factory.CreateEntity<Item>(DefRepository<ItemDef>.GetByMoniker("FlameStick")!, 1);
        Assert.True(scope.Context.PlayerPawn.Inventory.TryAdd(stick));
        Assert.Contains("FlameStick", ShopStock.OwnedUniqueMonikers(scope.Context.Player));
    }

    [Fact]
    public void TryBuyRejectsTrinketThePlayerAlreadyFound()
    {
        using var scope = CreateArena();
        var context = scope.Context;
        var offer = new MerchantOffer { ItemDef = DefRepository<ItemDef>.GetByMoniker("FlameStick")! };
        context.ArenaRun!.Gold = Math.Max(context.ArenaRun.Gold, offer.ResolveGoldCost() * 2);
        Assert.True(context.ArenaRun.TryBuy(context, offer));
        Assert.True(context.Player.HasTrinket(offer.ItemDef!));
        var goldAfterFirst = context.ArenaRun.Gold;
        var item = context.PlayerPawn.Inventory.First(i => i.ItemDef == offer.ItemDef);
        Assert.True(context.ArenaRun.TrySell(context, item));
        Assert.False(context.ArenaRun.TryBuy(context, offer));
        Assert.Equal(goldAfterFirst + offer.ResolveGoldCost() / 10, context.ArenaRun.Gold);
        Assert.Equal(0, context.PlayerPawn.Inventory.AmountOf(offer.ItemDef!));
    }

    [Fact]
    public void TryBuyRejectsFoodThePlayerAlreadyOwns()
    {
        using var scope = CreateArena();
        var context = scope.Context;
        var offer = new MerchantOffer { ItemDef = DefRepository<ItemDef>.GetByMoniker("CookedCorn")! };
        Assert.True(context.ArenaRun!.TryBuy(context, offer));
        var goldAfterFirst = context.ArenaRun.Gold;
        Assert.False(context.ArenaRun.TryBuy(context, offer));
        Assert.Equal(goldAfterFirst, context.ArenaRun.Gold);
        Assert.Equal(1, context.PlayerPawn.Inventory.AmountOf(offer.ItemDef!));
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
