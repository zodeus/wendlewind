using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Wendlemire.Definitions;
using Wendlemire.NetCode;
using Wendlemire.NetCode.Contracts;
using Wendlemire.Sim;
using Wendlemire.Sim.Arena;
using Wendlemire.Sim.Combat;
using Wendlemire.Sim.Entities;
using Wendlemire.Sim.Entities.Items;
using Wendlemire.Sim.Entities.Items.Medicinals;
using Wendlemire.Sim.Entities.Pawns;
using Xunit;

namespace Wendlemire.Tests;

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
        Assert.Equal(ArenaPhase.MerchantSelect, context.ArenaRun.Phase);
        Assert.Equal("Blacksmith", context.ArenaRun.CurrentMerchant!.Moniker);
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
    public void RecordMatchResultPersistsLossWithoutLeavingCombat()
    {
        using var scope = CreateArena();
        var run = scope.Context.ArenaRun!;
        run.SetPhase(ArenaPhase.Combat);

        run.RecordMatchResult(false, "opp-1");

        Assert.Equal(1, run.Losses);
        Assert.Equal(4, run.LivesRemaining);
        Assert.Equal(ArenaRun.StartingGold + ArenaRun.LoseGold, run.Gold);
        Assert.Equal(ArenaPhase.Combat, run.Phase);

        var progress = ArenaProgressMapper.FromRun(run, null, "run-1", DateTimeOffset.UtcNow);
        Assert.Equal(1, progress.Losses);
        Assert.Equal(0, progress.Wins);
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
        Assert.True(Owns(context.PlayerPawn, offer.ItemDef!));
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
    public void FoodAndIncenseAreNonStackable()
    {
        Assert.Equal(1, DefRepository<ItemDef>.GetByMoniker("CookedCorn")!.StackLimit);
        Assert.Equal(1, DefRepository<ItemDef>.GetByMoniker("MullinStick")!.StackLimit);
    }

    [Fact]
    public void MedicalShopOffersHaveFiveAvailable()
    {
        Assert.Equal(MerchantOffer.MedicalStock, new MerchantOffer
        {
            ItemDef = DefRepository<ItemDef>.GetByMoniker("MedKit")!
        }.CloneForStock().Available);
        Assert.Equal(1, new MerchantOffer
        {
            ItemDef = DefRepository<ItemDef>.GetByMoniker("Cauterize")!
        }.CloneForStock().Available);
        Assert.Equal(1, new MerchantOffer
        {
            ItemDef = DefRepository<ItemDef>.GetByMoniker("StrengthenBones")!
        }.CloneForStock().Available);
        Assert.Equal(1, new MerchantOffer
        {
            ItemDef = DefRepository<ItemDef>.GetByMoniker("Cyberveins")!
        }.CloneForStock().Available);
        Assert.Equal(1, new MerchantOffer
        {
            ItemDef = DefRepository<ItemDef>.GetByMoniker("CookedCorn")!
        }.CloneForStock().Available);
        Assert.Equal(1, new MerchantOffer
        {
            ItemDef = DefRepository<ItemDef>.GetByMoniker("StrengthPotion")!
        }.CloneForStock().Available);

        var merchant = DefRepository<MerchantDef>.GetByMoniker("GeneralStore")!;
        var medicine = ShopStock.Roll(merchant, 1, 0).Single(shelf => shelf.Category == ShopCategory.Medicine);
        Assert.All(medicine.Offers, offer =>
        {
            Assert.Equal(ItemType.Medical, offer.ItemDef!.ItemType);
            Assert.Equal(MerchantOffer.MedicalStock, offer.Available);
        });
    }

    [Fact]
    public void MedicalChestChargesRestoreFromPrepSnapshot()
    {
        using var scope = CreateArena();
        var pawn = scope.Context.PlayerPawn;
        var def = DefRepository<ItemDef>.GetByMoniker("MedKit")!;
        pawn.MedicalChest.Clear();
        Assert.True(pawn.MedicalChest.TryInstall(def, 3, new MedicalTrigger
        {
            Type = MedicalTriggerType.PartBelowHealth,
            TargetSelector = MedicalTargetSelector.Auto,
            HealthThreshold = 0.5f
        }));

        var snapshot = BuildSnapshotFactory.ToSnapshot(pawn, "p", "arena-1", 1, round: 1);
        pawn.MedicalChest.Slots[0].Charges = 0;
        BuildSnapshotFactory.Apply(pawn, snapshot);

        var slot = Assert.Single(pawn.MedicalChest.Slots);
        Assert.Equal(def, slot.Def);
        Assert.Equal(3, slot.Charges);
        Assert.Equal(MedicalTriggerType.PartBelowHealth, slot.Trigger.Type);
        Assert.Equal(0.5f, slot.Trigger.HealthThreshold);
    }

    [Fact]
    public void ArenaEncounterSeedIsStablePerRound()
    {
        Assert.Equal(ArenaSeeds.Encounter(99, 1), ArenaSeeds.Encounter(99, 1));
        Assert.NotEqual(ArenaSeeds.Encounter(99, 1), ArenaSeeds.Encounter(99, 2));
        Assert.NotEqual(ArenaSeeds.Encounter(99, 1), ArenaSeeds.Encounter(100, 1));
    }

    [Fact]
    public void ShopStockGrowsAfterRoundFourUntilShelfIsFull()
    {
        var shelf = new MerchantShelf
        {
            StockSize = 4,
            Columns = 6,
            ItemColumns = 1
        };
        Assert.Equal(4, ShopStock.ResolvedStockSize(shelf, 4));
        Assert.Equal(4, ShopStock.ResolvedStockSize(shelf, 5));
        Assert.Equal(5, ShopStock.ResolvedStockSize(shelf, 6));
        Assert.Equal(5, ShopStock.ResolvedStockSize(shelf, 7));
        Assert.Equal(6, ShopStock.ResolvedStockSize(shelf, 8));
        Assert.Equal(6, ShopStock.ResolvedStockSize(shelf, 9));

        var full = new MerchantShelf
        {
            StockSize = 6,
            Columns = 6,
            ItemColumns = 1
        };
        Assert.Equal(6, ShopStock.ResolvedStockSize(full, 8));

        var merchant = DefRepository<MerchantDef>.GetByMoniker("Magician")!;
        var early = ShopStock.Roll(merchant, 1, 4).Single(s => s.Category == ShopCategory.Potions);
        var later = ShopStock.Roll(merchant, 1, 8).Single(s => s.Category == ShopCategory.Potions);
        Assert.Equal(4, early.Offers.Count);
        Assert.Equal(6, later.Offers.Count);
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
                var reservedSets = setCount > 0 ? 1 : 0;
                var rolledSlots = Math.Max(0, shelf.StockSize - reservedSets);
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
    public void ShopStockRollsOneUnlockedArmorSet()
    {
        var merchant = DefRepository<MerchantDef>.GetByMoniker("Blacksmith")!;
        var early = ShopStock.Roll(merchant, 99, 1).Single(shelf => shelf.Category == ShopCategory.Armor);
        var earlySets = early.Offers.Where(offer => offer.IsSet).ToList();
        Assert.Single(earlySets);
        Assert.Contains(earlySets[0].SetLabel, new[] { "Cloth Set", "Leather Set" });
        Assert.DoesNotContain(early.Offers, offer => offer.SetLabel == "Chain Set");

        var late = ShopStock.Roll(merchant, 99, 4).Single(shelf => shelf.Category == ShopCategory.Armor);
        var lateSets = late.Offers.Where(offer => offer.IsSet).ToList();
        Assert.Single(lateSets);
        Assert.Contains(lateSets[0].SetLabel, new[] { "Cloth Set", "Leather Set", "Chain Set" });
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
        foreach (var group in set.SetPieces.GroupBy(piece => piece))
        {
            var equipped = context.PlayerPawn.Equipment.Count(item => item.Def == group.Key && !item.IsDestroyed);
            Assert.Equal(group.Count(), equipped);
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
    public void TrySellUnequipsWornGearAndPaysSellPrice()
    {
        using var scope = CreateArena();
        var context = scope.Context;
        var offer = Offer("WoodClub");
        context.ArenaRun!.Gold = Math.Max(context.ArenaRun.Gold, offer.ResolveGoldCost());
        Assert.True(context.ArenaRun.TryBuy(context, offer));
        var equipped = context.PlayerPawn.Equipment.First(item => item.Def == offer.ItemDef && !item.IsDestroyed);
        Assert.Contains(ShopPack.SellableItems(context.PlayerPawn), entry => entry.Equipped && entry.Item == equipped);
        var goldBefore = context.ArenaRun.Gold;

        Assert.True(context.ArenaRun.TrySell(context, equipped));
        Assert.Equal(goldBefore + ShopCatalog.GetSellPrice(offer.ItemDef!), context.ArenaRun.Gold);
        Assert.False(Owns(context.PlayerPawn, offer.ItemDef!));
    }

    [Fact]
    public void ShopPackOmitsBuiltinEquipment()
    {
        using var scope = CreateArena();
        var pawn = scope.Context.PlayerPawn;
        Assert.Contains(pawn.Equipment, item => ShopPack.IsBuiltin(item));
        Assert.DoesNotContain(ShopPack.SellableItems(pawn), entry => ShopPack.IsBuiltin(entry.Item));
    }

    [Fact]
    public void OpenShopVisitKeepsPurchasedHoles()
    {
        using var scope = CreateArena();
        var context = scope.Context;
        var run = context.ArenaRun!;
        var merchant = DefRepository<MerchantDef>.GetByMoniker("GeneralStore")!;
        run.CurrentMerchant = merchant;
        var rolled = ShopStock.Roll(
            merchant,
            run.RunSeed,
            run.FightsPlayed,
            ShopStock.OwnedUniqueMonikers(context.Player));
        var stock = run.OpenShopVisit(merchant, rolled);
        var offer = ShopStock.Flatten(stock).First(o =>
            !o.IsSet && o.Available == 1 && o.ItemDef?.ItemType == ItemType.Equipment);
        run.Gold = Math.Max(run.Gold, offer.ResolveGoldCost());
        var stockKey = offer.StockKey;

        Assert.True(run.TryBuy(context, offer));
        var restored = run.OpenShopVisit(
            merchant,
            ShopStock.Roll(
                merchant,
                run.RunSeed,
                run.FightsPlayed,
                ShopStock.OwnedUniqueMonikers(context.Player)));
        Assert.DoesNotContain(ShopStock.Flatten(restored), o => o.StockKey == stockKey);
    }

    [Fact]
    public void ProgressRoundTripKeepsShopVisitHoles()
    {
        using var scope = CreateArena();
        var context = scope.Context;
        var run = context.ArenaRun!;
        var merchant = DefRepository<MerchantDef>.GetByMoniker("GeneralStore")!;
        run.CurrentMerchant = merchant;
        var stock = run.OpenShopVisit(
            merchant,
            ShopStock.Roll(merchant, run.RunSeed, run.FightsPlayed, ShopStock.OwnedUniqueMonikers(context.Player)));
        var offer = ShopStock.Flatten(stock).First(o =>
            !o.IsSet && o.Available == 1 && o.ItemDef?.ItemType == ItemType.Equipment);
        run.Gold = Math.Max(run.Gold, offer.ResolveGoldCost());
        var stockKey = offer.StockKey;
        Assert.True(run.TryBuy(context, offer));

        var record = ArenaProgressMapper.FromRun(run, null, "shop-visit", DateTimeOffset.UtcNow);
        using var restored = CreateArena();
        ArenaProgressMapper.ApplyTo(restored.Context.ArenaRun!, record);
        var shelves = restored.Context.ArenaRun!.OpenShopVisit(
            merchant,
            ShopStock.Roll(
                merchant,
                restored.Context.ArenaRun.RunSeed,
                restored.Context.ArenaRun.FightsPlayed,
                ShopStock.OwnedUniqueMonikers(restored.Context.Player)));
        Assert.DoesNotContain(ShopStock.Flatten(shelves), o => o.StockKey == stockKey);
    }

    [Fact]
    public void TryRefreshShelfCostsTenGoldAndReplacesOnlyThatShelf()
    {
        using var scope = CreateArena();
        var context = scope.Context;
        var run = context.ArenaRun!;
        var merchant = DefRepository<MerchantDef>.GetByMoniker("GeneralStore")!;
        var stock = OpenVisit(run, context, merchant);
        var foodBefore = ShelfKeys(stock, ShopCategory.Food);
        var goldBefore = run.Gold;

        Assert.True(run.TryRefreshShelf(merchant, ShopCategory.Weapons, ShopStock.OwnedUniqueMonikers(context.Player)));
        var restored = ShopStock.Restore(merchant, run.ShopShelves);
        Assert.Equal(goldBefore - ShopCatalog.ShelfRefreshBaseCost, run.Gold);
        Assert.Equal(1, restored.Single(shelf => shelf.Category == ShopCategory.Weapons).RefreshCount);
        Assert.Equal(foodBefore, ShelfKeys(restored, ShopCategory.Food));
        Assert.Equal(0, restored.Single(shelf => shelf.Category == ShopCategory.Food).RefreshCount);
    }

    [Fact]
    public void TryRefreshShelfDoublesPerShelf()
    {
        using var scope = CreateArena();
        var context = scope.Context;
        var run = context.ArenaRun!;
        var merchant = DefRepository<MerchantDef>.GetByMoniker("GeneralStore")!;
        OpenVisit(run, context, merchant);
        var goldBefore = run.Gold;
        var owned = ShopStock.OwnedUniqueMonikers(context.Player);

        Assert.True(run.TryRefreshShelf(merchant, ShopCategory.Weapons, owned));
        Assert.Equal(goldBefore - 10, run.Gold);
        Assert.True(run.TryRefreshShelf(merchant, ShopCategory.Weapons, owned));
        Assert.Equal(goldBefore - 30, run.Gold);
        Assert.True(run.TryRefreshShelf(merchant, ShopCategory.Food, owned));
        Assert.Equal(goldBefore - 40, run.Gold);
        Assert.Equal(2, run.ShopShelves.Single(shelf => shelf.Category == ShopCategory.Weapons).RefreshCount);
        Assert.Equal(1, run.ShopShelves.Single(shelf => shelf.Category == ShopCategory.Food).RefreshCount);
    }

    [Fact]
    public void FirstArmorRefreshIsFreeThenFollowsNormalCurve()
    {
        using var scope = CreateArena();
        var context = scope.Context;
        var run = context.ArenaRun!;
        var merchant = DefRepository<MerchantDef>.GetByMoniker("GeneralStore")!;
        OpenVisit(run, context, merchant);
        var goldBefore = run.Gold;
        var owned = ShopStock.OwnedUniqueMonikers(context.Player);

        Assert.Equal(0, ShopCatalog.ShelfRefreshCost(ShopCategory.Armor, 0));
        Assert.True(run.TryRefreshShelf(merchant, ShopCategory.Armor, owned));
        Assert.Equal(goldBefore, run.Gold);
        Assert.Equal(1, run.ShopShelves.Single(shelf => shelf.Category == ShopCategory.Armor).RefreshCount);
        Assert.True(run.TryRefreshShelf(merchant, ShopCategory.Armor, owned));
        Assert.Equal(goldBefore - 10, run.Gold);
        Assert.True(run.TryRefreshShelf(merchant, ShopCategory.Armor, owned));
        Assert.Equal(goldBefore - 30, run.Gold);
    }

    [Fact]
    public void TryRefreshShelfRejectsOverspend()
    {
        using var scope = CreateArena();
        var context = scope.Context;
        var run = context.ArenaRun!;
        var merchant = DefRepository<MerchantDef>.GetByMoniker("GeneralStore")!;
        var stock = OpenVisit(run, context, merchant);
        var weaponsBefore = ShelfKeys(stock, ShopCategory.Weapons);
        run.Gold = ShopCatalog.ShelfRefreshBaseCost - 1;

        Assert.False(run.TryRefreshShelf(merchant, ShopCategory.Weapons, ShopStock.OwnedUniqueMonikers(context.Player)));
        Assert.Equal(ShopCatalog.ShelfRefreshBaseCost - 1, run.Gold);
        Assert.Equal(weaponsBefore, ShelfKeys(ShopStock.Restore(merchant, run.ShopShelves), ShopCategory.Weapons));
        Assert.Equal(0, run.ShopShelves.Single(shelf => shelf.Category == ShopCategory.Weapons).RefreshCount);
    }

    [Fact]
    public void ShelfRefreshIsDeterministicForTheSameIndex()
    {
        using var first = CreateArena();
        using var second = CreateArena();
        var merchant = DefRepository<MerchantDef>.GetByMoniker("GeneralStore")!;
        OpenVisit(first.Context.ArenaRun!, first.Context, merchant);
        OpenVisit(second.Context.ArenaRun!, second.Context, merchant);
        var owned = ShopStock.OwnedUniqueMonikers(first.Context.Player);

        Assert.True(first.Context.ArenaRun!.TryRefreshShelf(merchant, ShopCategory.Weapons, owned));
        Assert.True(second.Context.ArenaRun!.TryRefreshShelf(merchant, ShopCategory.Weapons, owned));
        Assert.Equal(
            ShelfKeys(ShopStock.Restore(merchant, first.Context.ArenaRun.ShopShelves), ShopCategory.Weapons),
            ShelfKeys(ShopStock.Restore(merchant, second.Context.ArenaRun.ShopShelves), ShopCategory.Weapons));
    }

    [Fact]
    public void LaterShelfRefreshIndexDiffersFromTheFirstRoll()
    {
        var merchant = DefRepository<MerchantDef>.GetByMoniker("GeneralStore")!;
        var shelf = merchant.Shelves.Single(s => s.Category == ShopCategory.Weapons);
        var foundDifference = false;
        for (var seed = 1; seed <= 32 && !foundDifference; seed++)
        {
            var original = ShopStock.Roll(merchant, seed, 0).Single(s => s.Category == ShopCategory.Weapons)
                .Offers.Select(offer => offer.StockKey);
            var refreshed = ShopStock.RollShelf(
                    shelf,
                    new Random(ArenaSeeds.ShopRefresh(seed, merchant.Moniker, 0, ShopCategory.Weapons, 1)),
                    0)
                .Select(offer => offer.StockKey);
            foundDifference = !original.SequenceEqual(refreshed);
        }

        Assert.True(foundDifference);
    }

    [Fact]
    public void OpenShopVisitKeepsShelfRefreshCount()
    {
        using var scope = CreateArena();
        var context = scope.Context;
        var run = context.ArenaRun!;
        var merchant = DefRepository<MerchantDef>.GetByMoniker("GeneralStore")!;
        OpenVisit(run, context, merchant);
        Assert.True(run.TryRefreshShelf(merchant, ShopCategory.Weapons, ShopStock.OwnedUniqueMonikers(context.Player)));

        var restored = run.OpenShopVisit(
            merchant,
            ShopStock.Roll(
                merchant,
                run.RunSeed,
                run.FightsPlayed,
                ShopStock.OwnedUniqueMonikers(context.Player)));
        Assert.Equal(1, restored.Single(shelf => shelf.Category == ShopCategory.Weapons).RefreshCount);
        Assert.Equal(20, ShopCatalog.ShelfRefreshCost(
            ShopCategory.Weapons,
            restored.Single(shelf => shelf.Category == ShopCategory.Weapons).RefreshCount));
    }

    [Fact]
    public void ProgressRoundTripKeepsShelfRefreshCount()
    {
        using var scope = CreateArena();
        var context = scope.Context;
        var run = context.ArenaRun!;
        var merchant = DefRepository<MerchantDef>.GetByMoniker("GeneralStore")!;
        OpenVisit(run, context, merchant);
        Assert.True(run.TryRefreshShelf(merchant, ShopCategory.Weapons, ShopStock.OwnedUniqueMonikers(context.Player)));

        var record = ArenaProgressMapper.FromRun(run, null, "shop-refresh", DateTimeOffset.UtcNow);
        Assert.Equal(1, record.ShopShelves.Single(shelf => shelf.Category == nameof(ShopCategory.Weapons)).RefreshCount);

        using var restored = CreateArena();
        ArenaProgressMapper.ApplyTo(restored.Context.ArenaRun!, record);
        var shelves = restored.Context.ArenaRun!.OpenShopVisit(
            merchant,
            ShopStock.Roll(
                merchant,
                restored.Context.ArenaRun.RunSeed,
                restored.Context.ArenaRun.FightsPlayed,
                ShopStock.OwnedUniqueMonikers(restored.Context.Player)));
        Assert.Equal(1, shelves.Single(shelf => shelf.Category == ShopCategory.Weapons).RefreshCount);
        Assert.Equal(
            ShelfKeys(ShopStock.Restore(merchant, run.ShopShelves), ShopCategory.Weapons),
            ShelfKeys(shelves, ShopCategory.Weapons));
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

    [Theory]
    [InlineData(1, "Blacksmith")]
    [InlineData(2, "Ranger")]
    [InlineData(3, "Alchemist")]
    [InlineData(4, "Magician")]
    public void MerchantPoolIsExclusiveForTheFirstFourVisits(int fightsPlayed, string moniker)
    {
        var pool = MerchantPool.Available(fightsPlayed);
        Assert.Equal([moniker], pool.Select(merchant => merchant.Moniker));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    [InlineData(9)]
    public void MerchantPoolOffersEverySpecialtyMerchantAfterTheIntro(int fightsPlayed)
    {
        var pool = MerchantPool.Available(fightsPlayed)
            .Select(merchant => merchant.Moniker)
            .OrderBy(moniker => moniker)
            .ToArray();
        Assert.Equal(["Alchemist", "Blacksmith", "Magician", "Ranger"], pool);
    }

    [Theory]
    [InlineData(1, "Blacksmith")]
    [InlineData(2, "Ranger")]
    [InlineData(3, "Alchemist")]
    [InlineData(4, "Magician")]
    public void MerchantPoolSelectsTheExclusiveMerchant(int fightsPlayed, string moniker)
    {
        Assert.Equal(moniker, MerchantPool.Select(99, fightsPlayed).Moniker);
    }

    [Fact]
    public void MerchantPoolSelectIsDeterministicAfterTheIntro()
    {
        Assert.Equal(MerchantPool.Select(99, 5).Moniker, MerchantPool.Select(99, 5).Moniker);
    }

    [Fact]
    public void MerchantPoolSelectVariesAcrossSeedsAfterTheIntro()
    {
        var distinct = Enumerable.Range(1, 16)
            .Select(seed => MerchantPool.Select(seed, 5).Moniker)
            .Distinct()
            .Count();
        Assert.True(distinct > 1);
    }

    [Fact]
    public void AssignNextMerchantPicksFromTheVisitPool()
    {
        using var scope = CreateArena();
        var run = scope.Context.ArenaRun!;
        run.ApplyMatchResult(true, "opp-1");
        run.AssignNextMerchant();
        Assert.Equal("Blacksmith", run.CurrentMerchant!.Moniker);
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
        var path = Path.Combine(Path.GetTempPath(), $"wendlemire-pool-{Guid.NewGuid():N}.json");
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
    public void BuildPoolPrefersNearbyRatingThenWidens()
    {
        var pool = new BuildPool();
        pool.Upsert(BuildTemplates.TankRegen() with { PlayerId = "near", Round = 1, Rating = 820 });
        pool.Upsert(BuildTemplates.AcidRusher() with { PlayerId = "far", Round = 1, Rating = 1600 });

        for (var i = 0; i < 20; i++)
        {
            Assert.Equal("near", pool.PickOpponent(1, "alice", attackerRating: 800)!.PlayerId);
        }
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
    public void SkillXpSurvivesArenaPawnRestore()
    {
        using var scope = CreateArena();
        var pawn = scope.Context.PlayerPawn;
        var axes = pawn.GetSkill(WeaponType.Axe);
        Assert.NotNull(axes);
        axes!.Learn(5);

        var snapshot = BuildSnapshotFactory.ToSnapshot(pawn, "p", "arena-1", 1, round: 1);
        scope.Context.RestoreArenaPawn();
        BuildSnapshotFactory.Apply(scope.Context.PlayerPawn, snapshot);

        var restored = scope.Context.PlayerPawn.GetSkill(WeaponType.Axe);
        Assert.NotNull(restored);
        Assert.Equal(5f, restored!.CurrentLevelXp);
        Assert.Equal(0, restored.Level);
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
    public void CookedMeatRaisesPartHitPointsAndRestoreReturnsThem()
    {
        using var scope = CreateArena();
        var pawn = scope.Context.PlayerPawn;
        var torso = pawn.Body.AllParts.First(p => p.Type == BodyPartType.Torso);
        var baselineMax = torso.MaxHitPoints;
        var meatDef = DefRepository<ItemDef>.GetByMoniker("CookedMeat")!;
        var meat = scope.Context.Factory.CreateEntity<Item>(meatDef, 1);
        Assert.True(pawn.Inventory.TryAdd(meat));
        Assert.True(pawn.MealPlan.TryAdd(meat));

        pawn.ApplyBattleStartConsumables();

        Assert.Equal(1.08f, pawn.GetStatValue(Defs.Stats.BodyScale), 3);
        Assert.Equal(baselineMax * 1.08, torso.MaxHitPoints, 3);

        pawn.Body.RestoreBodyScale();

        Assert.Equal(baselineMax, torso.MaxHitPoints);
    }

    [Fact]
    public void HardtackRaisesBodyScale()
    {
        using var scope = CreateArena();
        var pawn = scope.Context.PlayerPawn;
        var hardtack = scope.Context.Factory.CreateEntity<Item>(DefRepository<ItemDef>.GetByMoniker("Hardtack")!, 1);
        Assert.True(pawn.Inventory.TryAdd(hardtack));
        Assert.True(pawn.MealPlan.TryAdd(hardtack));

        pawn.ApplyBattleStartConsumables();

        Assert.Equal(1.05f, pawn.GetStatValue(Defs.Stats.BodyScale), 3);
        Assert.Contains(pawn.Body.Effects, effect => effect.Def.Moniker == "Doughy");
    }

    [Fact]
    public void RoastedBulbRaisesEvasion()
    {
        using var scope = CreateArena();
        var pawn = scope.Context.PlayerPawn;
        var baseline = pawn.GetStatValue(Defs.Stats.Evasion);
        var bulb = scope.Context.Factory.CreateEntity<Item>(DefRepository<ItemDef>.GetByMoniker("RoastedBulb")!, 1);
        Assert.True(pawn.Inventory.TryAdd(bulb));
        Assert.True(pawn.MealPlan.TryAdd(bulb));

        pawn.ApplyBattleStartConsumables();

        Assert.Equal(baseline + 0.05f, pawn.GetStatValue(Defs.Stats.Evasion), 3);
        Assert.Contains(pawn.Body.Effects, effect => effect.Def.Moniker == "Springy");
    }

    [Fact]
    public void StewedBerriesAppliesBerried()
    {
        using var scope = CreateArena();
        var pawn = scope.Context.PlayerPawn;
        var baselineMagic = pawn.GetStatValue(Defs.Stats.Magic);
        var berries = scope.Context.Factory.CreateEntity<Item>(DefRepository<ItemDef>.GetByMoniker("StewedBerries")!, 1);
        Assert.True(pawn.Inventory.TryAdd(berries));
        Assert.True(pawn.MealPlan.TryAdd(berries));

        pawn.ApplyBattleStartConsumables();

        Assert.Contains(pawn.Body.Effects, effect => effect.Def.Moniker == "Berried");
        Assert.Equal(1.05f, pawn.GetStatValue(Defs.Stats.BodyScale), 3);
        Assert.Equal(baselineMagic + 0.08f, pawn.GetStatValue(Defs.Stats.Magic), 3);
    }

    [Fact]
    public void SharedFoodEffectsStackBodyScale()
    {
        using var scope = CreateArena();
        var pawn = scope.Context.PlayerPawn;
        var stew = scope.Context.Factory.CreateEntity<Item>(DefRepository<ItemDef>.GetByMoniker("HeartyStew")!, 1);
        var stew2 = scope.Context.Factory.CreateEntity<Item>(DefRepository<ItemDef>.GetByMoniker("HeartyStew")!, 1);
        Assert.True(pawn.Inventory.TryAdd(stew));
        Assert.True(pawn.Inventory.TryAdd(stew2));
        pawn.MealPlan.Capacity = MealPlan.MaxSlots;
        Assert.True(pawn.MealPlan.TryAdd(stew));
        Assert.True(pawn.MealPlan.TryAdd(stew2));

        pawn.ApplyBattleStartConsumables();

        var energized = pawn.Body.Effects.Single(effect => effect.Def.Moniker == "Energized");
        Assert.Equal(2f, energized.Power);

        var expected = 1f;
        expected += expected * 0.10f;
        expected += expected * 0.16f;
        expected += expected * 0.10f;
        Assert.Equal(expected, pawn.GetStatValue(Defs.Stats.BodyScale), 3);
    }

    [Fact]
    public void LightingMullinStickDoesNotRaiseMagicUntilDelay()
    {
        using var scope = CreateArena();
        var pawn = scope.Context.PlayerPawn;
        var incenseDef = DefRepository<ItemDef>.GetByMoniker("MullinStick")!;
        var incense = scope.Context.Factory.CreateEntity<Item>(incenseDef, 1);
        Assert.True(pawn.Inventory.TryAdd(incense));
        Assert.True(pawn.TryLightIncense(incense, requireFlameStick: false));

        pawn.ApplyBattleStartConsumables();

        Assert.Equal(1f, pawn.GetStatValue(Defs.Stats.Magic), 3);
        Assert.False(pawn.Body.Effects.Has(Defs.BodyEffects.Mulled));

        var incenseSlot = Assert.Single(pawn.ActiveIncense);
        Assert.False(incenseSlot.ShouldFire(IncenseProperties.GetIgniteTick(0) - 1, 0));
        Assert.True(incenseSlot.ShouldFire(IncenseProperties.GetIgniteTick(0), 0));
        Assert.True(pawn.TryIgniteIncense(incenseSlot));
        Assert.Equal(1.15f, pawn.GetStatValue(Defs.Stats.Magic), 3);
    }

    [Fact]
    public void FoodPoisoningLowersBodyScale()
    {
        using var scope = CreateArena();
        var pawn = scope.Context.PlayerPawn;
        var rawDef = DefRepository<ItemDef>.GetByMoniker("RawMeat")!;
        var raw = scope.Context.Factory.CreateEntity<Item>(rawDef, 1);
        Assert.True(pawn.Inventory.TryAdd(raw));
        Assert.True(pawn.MealPlan.TryAdd(raw));

        pawn.ApplyBattleStartConsumables();

        Assert.Equal(0.90f, pawn.GetStatValue(Defs.Stats.BodyScale), 3);
        Assert.True(pawn.Body.Effects.Has(Defs.BodyEffects.FoodPoisoning));
    }

    [Fact]
    public void SpecialtyFoodsStillApplyGoldenLipsAndFruiting()
    {
        using var scope = CreateArena();
        var pawn = scope.Context.PlayerPawn;
        var cap = scope.Context.Factory.CreateEntity<Item>(DefRepository<ItemDef>.GetByMoniker("GoldCapMushroom")!, 1);
        var jam = scope.Context.Factory.CreateEntity<Item>(DefRepository<ItemDef>.GetByMoniker("WondrousJam")!, 1);
        Assert.True(pawn.Inventory.TryAdd(cap));
        Assert.True(pawn.Inventory.TryAdd(jam));
        pawn.MealPlan.Capacity = MealPlan.MaxSlots;
        Assert.True(pawn.MealPlan.TryAdd(cap));
        Assert.True(pawn.MealPlan.TryAdd(jam));

        pawn.ApplyBattleStartConsumables();

        Assert.True(pawn.Body.Effects.Has(Defs.BodyEffects.GoldenLips));
        Assert.True(pawn.Body.Effects.Has(Defs.BodyEffects.Fruiting));
        Assert.Equal(1f, pawn.GetStatValue(Defs.Stats.BodyScale), 3);
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
    public void TryBuyEquipsWeaponAndReplacesOccupiedSlot()
    {
        using var scope = CreateArena();
        var context = scope.Context;
        context.ArenaRun!.Gold = 1000;
        var first = Offer("BoneAxe");
        var second = Offer("StoneHammer");
        var third = Offer("IronSword");

        Assert.True(context.ArenaRun.TryBuy(context, first));
        Assert.True(context.ArenaRun.TryBuy(context, second));
        Assert.True(IsEquipped(context.PlayerPawn, first.ItemDef!));
        Assert.True(IsEquipped(context.PlayerPawn, second.ItemDef!));
        Assert.False(context.PlayerPawn.Inventory.Contains(first.ItemDef!));

        Assert.True(context.ArenaRun.TryBuy(context, third));
        Assert.True(IsEquipped(context.PlayerPawn, third.ItemDef!));
        Assert.True(IsEquipped(context.PlayerPawn, second.ItemDef!));
        Assert.False(IsEquipped(context.PlayerPawn, first.ItemDef!));
        Assert.True(context.PlayerPawn.Inventory.Contains(first.ItemDef!));
    }

    [Fact]
    public void TryBuyEquipsArmorAndReplacesOccupiedSlot()
    {
        using var scope = CreateArena();
        var context = scope.Context;
        context.ArenaRun!.Gold = 1000;
        var first = Offer("LeatherHelmet");
        var second = Offer("ClothHelmet");

        Assert.True(context.ArenaRun.TryBuy(context, first));
        Assert.True(IsEquipped(context.PlayerPawn, first.ItemDef!));

        Assert.True(context.ArenaRun.TryBuy(context, second));
        Assert.True(IsEquipped(context.PlayerPawn, second.ItemDef!));
        Assert.False(IsEquipped(context.PlayerPawn, first.ItemDef!));
        Assert.True(context.PlayerPawn.Inventory.Contains(first.ItemDef!));
    }

    [Fact]
    public void TryBuyEquipsPotionAndReplacesWhenBothSlotsFull()
    {
        using var scope = CreateArena();
        var context = scope.Context;
        context.ArenaRun!.Gold = 1000;
        var first = Offer("AcidFlask");
        var second = Offer("AntiStaticFlask");
        var third = Offer("JarOfBlood");

        Assert.True(context.ArenaRun.TryBuy(context, first));
        Assert.True(context.ArenaRun.TryBuy(context, second));
        Assert.True(IsEquipped(context.PlayerPawn, first.ItemDef!));
        Assert.True(IsEquipped(context.PlayerPawn, second.ItemDef!));

        Assert.True(context.ArenaRun.TryBuy(context, third));
        Assert.True(IsEquipped(context.PlayerPawn, third.ItemDef!));
        Assert.True(IsEquipped(context.PlayerPawn, second.ItemDef!));
        Assert.False(IsEquipped(context.PlayerPawn, first.ItemDef!));
        Assert.True(context.PlayerPawn.Inventory.Contains(first.ItemDef!));
    }

    [Fact]
    public void TryBuyAddsFoodToMealAndReplacesOldestWhenFull()
    {
        using var scope = CreateArena();
        var context = scope.Context;
        context.ArenaRun!.Gold = 1000;
        context.PlayerPawn.MealPlan.Capacity = MealPlan.MaxSlots;
        var foods = new[] { "CookedFish", "CookedMeat", "DriedMeat", "CookedCorn", "HeartyStew" }
            .Select(Offer)
            .ToArray();

        foreach (var offer in foods)
        {
            Assert.True(context.ArenaRun.TryBuy(context, offer));
        }

        var meal = context.PlayerPawn.MealPlan.Items.Select(item => item.ItemDef.Moniker).ToArray();
        Assert.Equal(["CookedMeat", "DriedMeat", "CookedCorn", "HeartyStew"], meal);
        Assert.Equal(1, context.PlayerPawn.Inventory.AmountOf(foods[0].ItemDef!));
        Assert.Equal(1, context.PlayerPawn.Inventory.AmountOf(foods[^1].ItemDef!));
    }

    [Fact]
    public void TryBuyLightsIncenseWithoutFlameStick()
    {
        using var scope = CreateArena();
        var pawn = scope.Context.PlayerPawn;
        var offer = Offer("MullinStick");
        Assert.True(scope.Context.ArenaRun!.TryBuy(scope.Context, offer));
        var incense = Assert.Single(pawn.ActiveIncense);
        Assert.Equal("MullinStick", incense.SourceMoniker);
        Assert.False(pawn.HasFlameStick());
        Assert.True(pawn.Inventory.Contains(offer.ItemDef!));
    }

    [Fact]
    public void TryBuyLightsIncenseWithoutFlameStickAndReplacesOldestWhenFull()
    {
        using var scope = CreateArena();
        var context = scope.Context;
        var pawn = context.PlayerPawn;
        context.ArenaRun!.Gold = 1000;
        pawn.IncenseCapacity = IncenseProperties.MaxActive;
        var filler = DefRepository<BodyEffectDef>.GetByMoniker("Fishy")!;
        for (var i = 0; i < IncenseProperties.MaxActive; i++)
        {
            pawn.ActiveIncense.Add(new ActiveIncense
            {
                Def = filler,
                EncountersRemaining = 1,
                SourceMoniker = $"dummy{i}"
            });
        }

        var offer = Offer("MullinStick");
        Assert.True(context.ArenaRun.TryBuy(context, offer));
        Assert.Equal(IncenseProperties.MaxActive, pawn.ActiveIncense.Count);
        Assert.Equal("MullinStick", pawn.ActiveIncense[^1].SourceMoniker);
        Assert.DoesNotContain(pawn.ActiveIncense, incense => incense.SourceMoniker == "dummy0");
        Assert.False(pawn.HasFlameStick());
    }

    [Fact]
    public void TryBuyDoesNotArmMedical()
    {
        using var scope = CreateArena();
        var context = scope.Context;
        var offer = Offer("MedKit");
        Assert.True(context.ArenaRun!.TryBuy(context, offer));
        Assert.True(context.PlayerPawn.Inventory.Contains(offer.ItemDef!));
        Assert.Empty(context.PlayerPawn.MedicalChest.Slots);
    }

    [Fact]
    public void TryBuyTrinketStaysInInventory()
    {
        using var scope = CreateArena();
        var context = scope.Context;
        var offer = Offer("FlameStick");
        context.ArenaRun!.Gold = Math.Max(context.ArenaRun.Gold, offer.ResolveGoldCost());
        Assert.True(context.ArenaRun.TryBuy(context, offer));
        Assert.True(context.PlayerPawn.Inventory.Contains(offer.ItemDef!));
        Assert.True(context.Player.HasTrinket(offer.ItemDef!));
        Assert.False(IsEquipped(context.PlayerPawn, offer.ItemDef!));
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

    private static IReadOnlyList<RolledShelf> OpenVisit(ArenaRun run, GameContext context, MerchantDef merchant)
    {
        run.CurrentMerchant = merchant;
        return run.OpenShopVisit(
            merchant,
            ShopStock.Roll(
                merchant,
                run.RunSeed,
                run.FightsPlayed,
                ShopStock.OwnedUniqueMonikers(context.Player)));
    }

    private static List<string> ShelfKeys(IReadOnlyList<RolledShelf> stock, ShopCategory category) =>
        stock.Single(shelf => shelf.Category == category).Offers.Select(offer => offer.StockKey).ToList();

    private static MerchantOffer Offer(string moniker) =>
        new() { ItemDef = DefRepository<ItemDef>.GetByMoniker(moniker)! };

    private static bool Owns(Pawn pawn, ItemDef def) =>
        pawn.Inventory.Contains(def) || IsEquipped(pawn, def);

    private static bool IsEquipped(Pawn pawn, ItemDef def) =>
        pawn.Equipment.Any(item => item.Def == def && !item.IsDestroyed);

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
