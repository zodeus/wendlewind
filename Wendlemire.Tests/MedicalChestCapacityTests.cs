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
        "Suture",
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
    public void CampaignStartsWithAllMedicalSlots()
    {
        using var root = SimServices.BuildRoot();
        using var scope = root.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<GameContext>();
        context.Initialize(CombatReplay.DefaultRunSeed);

        Assert.Equal(MedicalChest.MaxSlots, context.PlayerPawn.MedicalChest.Capacity);
    }

    [Fact]
    public void ArenaStartsWithThreeMedicalSlots()
    {
        using var root = SimServices.BuildRoot();
        using var scope = root.CreateScope();
        var context = StartArena(scope);

        Assert.Equal(MedicalChest.BaseSlots, context.PlayerPawn.MedicalChest.Capacity);
    }

    [Fact]
    public void LaterRoundsOpenMoreMedicalSlots()
    {
        using var root = SimServices.BuildRoot();
        using var scope = root.CreateScope();
        var context = StartArena(scope);
        context.ArenaRun!.Wins = 3;
        context.RefreshPlayerConsumableSlots();

        Assert.Equal(6, context.PlayerPawn.MedicalChest.Capacity);
    }

    [Fact]
    public void TryInstallFailsWhenFull()
    {
        using var root = SimServices.BuildRoot();
        using var scope = root.CreateScope();
        var context = StartArena(scope);
        var chest = context.PlayerPawn.MedicalChest;

        Assert.True(chest.TryInstall(RequireDef(MedicalMonikers[0]), 1));
        Assert.True(chest.TryInstall(RequireDef(MedicalMonikers[1]), 1));
        Assert.True(chest.TryInstall(RequireDef(MedicalMonikers[2]), 1));
        Assert.False(chest.TryInstall(RequireDef(MedicalMonikers[3]), 1));
        Assert.Equal(3, chest.Slots.Count);
    }

    [Fact]
    public void NextRoundOpensASlotImmediately()
    {
        using var root = SimServices.BuildRoot();
        using var scope = root.CreateScope();
        var context = StartArena(scope);
        var chest = context.PlayerPawn.MedicalChest;

        Assert.True(chest.TryInstall(RequireDef(MedicalMonikers[0]), 1));
        Assert.True(chest.TryInstall(RequireDef(MedicalMonikers[1]), 1));
        Assert.True(chest.TryInstall(RequireDef(MedicalMonikers[2]), 1));
        Assert.False(chest.TryInstall(RequireDef(MedicalMonikers[3]), 1));

        context.ArenaRun!.Wins = 1;
        context.RefreshPlayerConsumableSlots();

        Assert.Equal(4, chest.Capacity);
        Assert.True(chest.TryInstall(RequireDef(MedicalMonikers[3]), 1));
        Assert.Equal(4, chest.Slots.Count);
    }

    [Fact]
    public void RefreshPrunesExcessSlotsToInventory()
    {
        using var root = SimServices.BuildRoot();
        using var scope = root.CreateScope();
        var context = StartArena(scope);
        var pawn = context.PlayerPawn;
        var chest = pawn.MedicalChest;
        chest.EnsureCapacity(6);
        for (var i = 0; i < 5; i++)
        {
            Assert.True(chest.TryInstall(RequireDef(MedicalMonikers[i]), 2));
        }

        Assert.Equal(5, chest.Slots.Count);
        chest.RefreshCapacity(MedicalChest.BaseSlots);

        Assert.Equal(MedicalChest.BaseSlots, chest.Capacity);
        Assert.Equal(MedicalChest.BaseSlots, chest.Slots.Count);
        Assert.True(pawn.Inventory.AmountOf(RequireDef(MedicalMonikers[3])) >= 2);
        Assert.True(pawn.Inventory.AmountOf(RequireDef(MedicalMonikers[4])) >= 2);
    }

    private static GameContext StartArena(IServiceScope scope)
    {
        var context = scope.ServiceProvider.GetRequiredService<GameContext>();
        context.InitializeArena("tester", "Tester", CombatReplay.DefaultRunSeed);
        return context;
    }

    private static ItemDef RequireDef(string moniker)
    {
        return DefRepository<ItemDef>.GetByMoniker(moniker)
               ?? throw new InvalidOperationException($"Missing medical def '{moniker}'.");
    }
}
