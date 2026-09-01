using Microsoft.Extensions.DependencyInjection;
using Wendlemire.Definitions;
using Wendlemire.Sim;
using Wendlemire.Sim.Combat;
using Wendlemire.Sim.Entities.Items;
using Wendlemire.Sim.Entities.Items.Medicinals;
using Xunit;

namespace Wendlemire.Tests;

[Collection("Sim")]
public class MedicalChestCapacityTests
{
    private static readonly string[] MedicalMonikers =
    [
        "MedKit",
        "ArterialThreads",
        "MendersMist",
        "BalmyOintment",
        "AntiNecroticSerum",
        "MendersMix"
    ];

    public MedicalChestCapacityTests()
    {
        TestData.EnsureLoaded();
    }

    [Fact]
    public void NewGameStartsWithThreeSlots()
    {
        using var root = SimServices.BuildRoot();
        using var scope = root.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<GameContext>();
        context.Initialize(CombatReplay.DefaultRunSeed);

        Assert.Equal(9, MedicalChest.SlotUnlockDefs().Count());
        Assert.Equal(MedicalChest.BaseSlots, context.PlayerPawn.MedicalChest.Capacity);
    }

    [Fact]
    public void EachSlotAchievementAddsOne()
    {
        using var root = SimServices.BuildRoot();
        using var scope = root.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<GameContext>();
        context.Initialize(CombatReplay.DefaultRunSeed);

        var defs = MedicalChest.SlotUnlockDefs().Take(3).ToList();
        Assert.Equal(3, defs.Count);
        foreach (var def in defs)
        {
            context.Achievements.Unlock(def);
        }

        Assert.Equal(MedicalChest.BaseSlots + 3, context.PlayerPawn.MedicalChest.Capacity);
    }

    [Fact]
    public void TryInstallFailsWhenFull()
    {
        using var root = SimServices.BuildRoot();
        using var scope = root.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<GameContext>();
        context.Initialize(CombatReplay.DefaultRunSeed);
        var chest = context.PlayerPawn.MedicalChest;

        Assert.True(chest.TryInstall(RequireDef(MedicalMonikers[0]), 1));
        Assert.True(chest.TryInstall(RequireDef(MedicalMonikers[1]), 1));
        Assert.True(chest.TryInstall(RequireDef(MedicalMonikers[2]), 1));
        Assert.False(chest.TryInstall(RequireDef(MedicalMonikers[3]), 1));
        Assert.Equal(3, chest.Slots.Count);
    }

    [Fact]
    public void UnlockOpensASlotImmediately()
    {
        using var root = SimServices.BuildRoot();
        using var scope = root.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<GameContext>();
        context.Initialize(CombatReplay.DefaultRunSeed);
        var chest = context.PlayerPawn.MedicalChest;

        Assert.True(chest.TryInstall(RequireDef(MedicalMonikers[0]), 1));
        Assert.True(chest.TryInstall(RequireDef(MedicalMonikers[1]), 1));
        Assert.True(chest.TryInstall(RequireDef(MedicalMonikers[2]), 1));
        Assert.False(chest.TryInstall(RequireDef(MedicalMonikers[3]), 1));

        context.Achievements.Unlock(MedicalChest.SlotUnlockDefs().First());

        Assert.Equal(MedicalChest.BaseSlots + 1, chest.Capacity);
        Assert.True(chest.TryInstall(RequireDef(MedicalMonikers[3]), 1));
        Assert.Equal(4, chest.Slots.Count);
    }

    [Fact]
    public void RefreshPrunesExcessSlotsToInventory()
    {
        using var root = SimServices.BuildRoot();
        using var scope = root.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<GameContext>();
        context.Initialize(CombatReplay.DefaultRunSeed);
        var pawn = context.PlayerPawn;
        var chest = pawn.MedicalChest;
        chest.EnsureCapacity(6);
        for (var i = 0; i < 5; i++)
        {
            Assert.True(chest.TryInstall(RequireDef(MedicalMonikers[i]), 2));
        }

        Assert.Equal(5, chest.Slots.Count);
        chest.RefreshFromAchievements(context.Achievements);

        Assert.Equal(MedicalChest.BaseSlots, chest.Capacity);
        Assert.Equal(MedicalChest.BaseSlots, chest.Slots.Count);
        Assert.True(pawn.Inventory.AmountOf(RequireDef(MedicalMonikers[3])) >= 2);
        Assert.True(pawn.Inventory.AmountOf(RequireDef(MedicalMonikers[4])) >= 2);
    }

    private static ItemDef RequireDef(string moniker)
    {
        return DefRepository<ItemDef>.GetByMoniker(moniker)
               ?? throw new InvalidOperationException($"Missing medical def '{moniker}'.");
    }
}
