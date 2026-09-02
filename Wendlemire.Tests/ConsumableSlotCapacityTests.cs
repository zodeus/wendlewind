using Microsoft.Extensions.DependencyInjection;
using Wendlemire.Definitions;
using Wendlemire.Sim;
using Wendlemire.Sim.Combat;
using Wendlemire.Sim.Entities.Items;
using Wendlemire.Sim.Entities.Items.Medicinals;
using Wendlemire.Sim.Entities.Items.Potions;
using Wendlemire.Sim.Entities.Pawns;
using Xunit;

namespace Wendlemire.Tests;

[Collection("Sim")]
public class ConsumableSlotCapacityTests
{
    public ConsumableSlotCapacityTests()
    {
        TestData.EnsureLoaded();
    }

    [Fact]
    public void NewGameStartsWithBaseConsumableSlots()
    {
        using var root = SimServices.BuildRoot();
        using var scope = root.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<GameContext>();
        context.Initialize(CombatReplay.DefaultRunSeed);
        var pawn = context.PlayerPawn;

        Assert.Equal(10, MedicalChest.SlotUnlockDefs().Count());
        Assert.Equal(2, PotionSlots.SlotUnlockDefs().Count());
        Assert.Equal(2, IncenseProperties.SlotUnlockDefs().Count());
        Assert.Equal(3, MealPlan.SlotUnlockDefs().Count());

        Assert.Equal(MedicalChest.BaseSlots, pawn.MedicalChest.Capacity);
        Assert.Equal(PotionSlots.BaseSlots, pawn.PotionCapacity);
        Assert.Equal(IncenseProperties.BaseSlots, pawn.IncenseCapacity);
        Assert.Equal(MealPlan.BaseSlots, pawn.MealPlan.Capacity);
        Assert.Equal(MealPlan.BaseSlots, pawn.CombatStomach.Capacity);
    }

    [Fact]
    public void EachCategoryUnlockBumpsOnlyItsCap()
    {
        using var root = SimServices.BuildRoot();
        using var scope = root.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<GameContext>();
        context.Initialize(CombatReplay.DefaultRunSeed);
        var pawn = context.PlayerPawn;

        context.Achievements.Unlock(PotionSlots.SlotUnlockDefs().First());
        Assert.Equal(PotionSlots.BaseSlots + 1, pawn.PotionCapacity);
        Assert.Equal(IncenseProperties.BaseSlots, pawn.IncenseCapacity);
        Assert.Equal(MealPlan.BaseSlots, pawn.MealPlan.Capacity);

        context.Achievements.Unlock(IncenseProperties.SlotUnlockDefs().First());
        Assert.Equal(IncenseProperties.BaseSlots + 1, pawn.IncenseCapacity);
        Assert.Equal(PotionSlots.BaseSlots + 1, pawn.PotionCapacity);

        context.Achievements.Unlock(MealPlan.SlotUnlockDefs().First());
        Assert.Equal(MealPlan.BaseSlots + 1, pawn.MealPlan.Capacity);
        Assert.Equal(MealPlan.BaseSlots + 1, pawn.CombatStomach.Capacity);
        Assert.Equal(MedicalChest.BaseSlots, pawn.MedicalChest.Capacity);
    }

    [Fact]
    public void CapsClampAtMax()
    {
        using var root = SimServices.BuildRoot();
        using var scope = root.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<GameContext>();
        context.Initialize(CombatReplay.DefaultRunSeed);
        var pawn = context.PlayerPawn;

        foreach (var def in PotionSlots.SlotUnlockDefs())
        {
            context.Achievements.Unlock(def);
        }

        foreach (var def in IncenseProperties.SlotUnlockDefs())
        {
            context.Achievements.Unlock(def);
        }

        foreach (var def in MealPlan.SlotUnlockDefs())
        {
            context.Achievements.Unlock(def);
        }

        Assert.Equal(PotionSlots.MaxSlots, pawn.PotionCapacity);
        Assert.Equal(IncenseProperties.MaxActive, pawn.IncenseCapacity);
        Assert.Equal(MealPlan.MaxSlots, pawn.MealPlan.Capacity);
    }

    [Fact]
    public void RefreshPrunesOverflowingFoodAndIncense()
    {
        using var root = SimServices.BuildRoot();
        using var scope = root.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<GameContext>();
        context.Initialize(CombatReplay.DefaultRunSeed);
        var pawn = context.PlayerPawn;

        pawn.MealPlan.Capacity = MealPlan.MaxSlots;
        pawn.IncenseCapacity = IncenseProperties.MaxActive;

        var foods = new[] { "CookedMeat", "DriedMeat", "CookedCorn", "HeartyStew" };
        foreach (var moniker in foods)
        {
            var item = context.Factory.CreateEntity<Item>(RequireDef(moniker), 1);
            Assert.True(pawn.Inventory.TryAdd(item));
            Assert.True(pawn.MealPlan.TryAdd(item));
        }

        var incenseDefs = new[] { "MullinStick", "ShadeWood", "DippedMullinStick" };
        foreach (var moniker in incenseDefs)
        {
            var item = context.Factory.CreateEntity<Item>(RequireDef(moniker), 1);
            Assert.True(pawn.Inventory.TryAdd(item));
            Assert.True(pawn.TryLightIncense(item, requireFlameStick: false));
        }

        Assert.Equal(4, pawn.MealPlan.Items.Count);
        Assert.Equal(3, pawn.ActiveIncense.Count);

        pawn.RefreshConsumableSlots(context.Achievements);

        Assert.Equal(MealPlan.BaseSlots, pawn.MealPlan.Capacity);
        Assert.Single(pawn.MealPlan.Items);
        Assert.Equal(IncenseProperties.BaseSlots, pawn.IncenseCapacity);
        Assert.Single(pawn.ActiveIncense);
    }

    private static ItemDef RequireDef(string moniker)
    {
        return DefRepository<ItemDef>.GetByMoniker(moniker)
               ?? throw new InvalidOperationException($"Missing def '{moniker}'.");
    }
}
