using Microsoft.Extensions.DependencyInjection;
using Wendlemire.Definitions;
using Wendlemire.Sim;
using Wendlemire.Sim.Arena;
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
    public void CampaignStartsWithFullConsumableSlots()
    {
        using var root = SimServices.BuildRoot();
        using var scope = root.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<GameContext>();
        context.Initialize(CombatReplay.DefaultRunSeed);
        var pawn = context.PlayerPawn;

        Assert.Equal(MedicalChest.MaxSlots, pawn.MedicalChest.Capacity);
        Assert.Equal(PotionSlots.MaxSlots, pawn.PotionCapacity);
        Assert.Equal(IncenseProperties.MaxActive, pawn.IncenseCapacity);
        Assert.Equal(MealPlan.MaxSlots, pawn.MealPlan.Capacity);
        Assert.Equal(MealPlan.MaxSlots, pawn.CombatStomach.Capacity);
    }

    [Fact]
    public void ArenaStartsWithBaseConsumableSlots()
    {
        using var root = SimServices.BuildRoot();
        using var scope = root.CreateScope();
        var context = StartArena(scope);

        AssertCaps(context, PrepSlotUnlocks.ForRound(1));
    }

    [Theory]
    [InlineData(1, 3, 2, 1, 1)]
    [InlineData(2, 4, 2, 1, 2)]
    [InlineData(3, 5, 3, 1, 2)]
    [InlineData(4, 6, 3, 2, 2)]
    [InlineData(5, 8, 3, 2, 3)]
    [InlineData(6, 10, 4, 2, 3)]
    [InlineData(7, 11, 4, 3, 3)]
    [InlineData(8, 12, 4, 3, 4)]
    [InlineData(10, 12, 4, 3, 4)]
    public void UpcomingRoundUnlocksPrepTiles(int upcomingRound, int medical, int potion, int incense, int food)
    {
        using var root = SimServices.BuildRoot();
        using var scope = root.CreateScope();
        var context = StartArena(scope);
        context.ArenaRun!.Wins = upcomingRound - 1;
        context.RefreshPlayerConsumableSlots();

        Assert.Equal(upcomingRound, context.PrepUnlockRound);
        AssertCaps(context, new PrepSlotCaps(medical, potion, incense, food));
    }

    [Fact]
    public void RefreshPrunesOverflowingFoodAndIncense()
    {
        using var root = SimServices.BuildRoot();
        using var scope = root.CreateScope();
        var context = StartArena(scope);
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

        pawn.RefreshConsumableSlots(1);

        Assert.Equal(MealPlan.BaseSlots, pawn.MealPlan.Capacity);
        Assert.Single(pawn.MealPlan.Items);
        Assert.Equal(IncenseProperties.BaseSlots, pawn.IncenseCapacity);
        Assert.Single(pawn.ActiveIncense);
    }

    [Fact]
    public void UnlockRoundMatchesFirstRoundThatOpensTheSlot()
    {
        Assert.Equal(1, PrepSlotUnlocks.UnlockRound(PrepSlotKind.Medical, 3));
        Assert.Equal(2, PrepSlotUnlocks.UnlockRound(PrepSlotKind.Medical, 4));
        Assert.Equal(8, PrepSlotUnlocks.UnlockRound(PrepSlotKind.Medical, 12));
        Assert.Equal(3, PrepSlotUnlocks.UnlockRound(PrepSlotKind.Potion, 3));
        Assert.Equal(6, PrepSlotUnlocks.UnlockRound(PrepSlotKind.Potion, 4));
        Assert.Equal(4, PrepSlotUnlocks.UnlockRound(PrepSlotKind.Incense, 2));
        Assert.Equal(7, PrepSlotUnlocks.UnlockRound(PrepSlotKind.Incense, 3));
        Assert.Equal(2, PrepSlotUnlocks.UnlockRound(PrepSlotKind.Food, 2));
        Assert.Equal(8, PrepSlotUnlocks.UnlockRound(PrepSlotKind.Food, 4));
    }

    private static GameContext StartArena(IServiceScope scope)
    {
        var context = scope.ServiceProvider.GetRequiredService<GameContext>();
        context.InitializeArena("tester", "Tester", CombatReplay.DefaultRunSeed);
        return context;
    }

    private static void AssertCaps(GameContext context, PrepSlotCaps caps)
    {
        var pawn = context.PlayerPawn;
        Assert.Equal(caps.Medical, pawn.MedicalChest.Capacity);
        Assert.Equal(caps.Potion, pawn.PotionCapacity);
        Assert.Equal(caps.Incense, pawn.IncenseCapacity);
        Assert.Equal(caps.Food, pawn.MealPlan.Capacity);
        Assert.Equal(caps.Food, pawn.CombatStomach.Capacity);
    }

    private static ItemDef RequireDef(string moniker)
    {
        return DefRepository<ItemDef>.GetByMoniker(moniker)
               ?? throw new InvalidOperationException($"Missing def '{moniker}'.");
    }
}
