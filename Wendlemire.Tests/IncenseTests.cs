using Wendlemire.Definitions;
using Wendlemire.Sim;
using Wendlemire.Sim.Arena;
using Wendlemire.Sim.Entities;
using Wendlemire.Sim.Entities.Items;
using Wendlemire.Sim.Entities.Pawns;
using Wendlemire.Sim.Entities.Pawns.Bodies.Handlers;
using Xunit;

namespace Wendlemire.Tests;

[Collection("Sim")]
public class IncenseTests
{
    public IncenseTests()
    {
        TestData.EnsureLoaded();
    }

    [Fact]
    public void BattleStartDoesNotIgniteIncense()
    {
        using var harness = BodyTestHarness.Human();
        Light(harness, Defs.Items.MullinStick);

        harness.Pawn.ApplyBattleStartConsumables();

        Assert.False(harness.Pawn.Body.Effects.Has(Defs.BodyEffects.Mulled));
        Assert.Equal(1f, harness.Pawn.GetStatValue(Defs.Stats.Magic), 3);
    }

    [Fact]
    public void MullinFiresAfterDelayAndExpires()
    {
        using var harness = BodyTestHarness.Human();
        Light(harness, Defs.Items.MullinStick);
        var slot = Assert.Single(harness.Pawn.ActiveIncense);

        Assert.False(slot.ShouldFire(IncenseProperties.GetIgniteTick(0) - 1, 0));
        Assert.True(slot.ShouldFire(IncenseProperties.GetIgniteTick(0), 0));
        Assert.True(harness.Pawn.TryIgniteIncense(slot));
        Assert.True(harness.Pawn.Body.Effects.Has(Defs.BodyEffects.Mulled));
        Assert.Equal(1.15f, harness.Pawn.GetStatValue(Defs.Stats.Magic), 3);

        for (var i = 0; i < slot.GetDurationInTicks(); i++)
        {
            harness.Pawn.Body.Effects.Tick();
        }

        Assert.False(harness.Pawn.Body.Effects.Has(Defs.BodyEffects.Mulled));
    }

    [Fact]
    public void ThreeSticksFireBySlotOrder()
    {
        using var harness = BodyTestHarness.Human();
        Light(harness, Defs.Items.MullinStick);
        Light(harness, Defs.Items.ShadeWood);
        Light(harness, Defs.Items.Clotcedar);

        Assert.Equal(3, harness.Pawn.ActiveIncense.Count);
        Assert.False(harness.Pawn.ActiveIncense[0].ShouldFire(119, 0));
        Assert.True(harness.Pawn.ActiveIncense[0].ShouldFire(120, 0));
        Assert.False(harness.Pawn.ActiveIncense[1].ShouldFire(239, 1));
        Assert.True(harness.Pawn.ActiveIncense[1].ShouldFire(240, 1));
        Assert.False(harness.Pawn.ActiveIncense[2].ShouldFire(359, 2));
        Assert.True(harness.Pawn.ActiveIncense[2].ShouldFire(360, 2));
    }

    [Fact]
    public void ClotcedarSlowsArteryBleed()
    {
        using var control = BodyTestHarness.Human();
        using var treated = BodyTestHarness.Human();
        DestroyArmArtery(control);
        DestroyArmArtery(treated);
        Light(treated, Defs.Items.Clotcedar);
        Assert.True(treated.Pawn.TryIgniteIncense(treated.Pawn.ActiveIncense[0]));

        Assert.Equal(DefaultBodyHandler.ClottedViscosityFactor, treated.Pawn.Body.Handler.ViscosityModifier);

        var controlBefore = control.Pawn.Body.BloodAmount;
        var treatedBefore = treated.Pawn.Body.BloodAmount;
        control.TickBody();
        treated.TickBody();

        var controlLoss = controlBefore - control.Pawn.Body.BloodAmount;
        var treatedLoss = treatedBefore - treated.Pawn.Body.BloodAmount;
        Assert.True(treatedLoss > 0);
        Assert.Equal(controlLoss / DefaultBodyHandler.ClottedViscosityFactor, treatedLoss, 3);
    }

    [Fact]
    public void LungwortFloorsOneLungBreathing()
    {
        using var harness = BodyTestHarness.Human();
        var lungs = harness.Parts(BodyPartType.Lung);
        Assert.True(lungs.Count >= 2);
        lungs[0].HitPoints = 0;
        Assert.Equal(0.5f, harness.Pawn.Body.Capabilities.Breathing, 3);

        Light(harness, Defs.Items.LungwortBraid);
        Assert.True(harness.Pawn.TryIgniteIncense(harness.Pawn.ActiveIncense[0]));
        Assert.Equal(PawnCapabilities.LungwortedBreathingFloor, harness.Pawn.Body.Capabilities.Breathing, 3);
    }

    [Theory]
    [InlineData("GeneralStore", 0, true)]
    [InlineData("Alchemist", 0, true)]
    [InlineData("Alchemist", 0, false)]
    [InlineData("Alchemist", 2, true)]
    public void ShopsStockNewIncense(string merchantMoniker, int round, bool clotcedar)
    {
        var merchant = DefRepository<MerchantDef>.GetByMoniker(merchantMoniker)!;
        var moniker = clotcedar ? "Clotcedar" : "LungwortBraid";
        if (merchantMoniker == "Alchemist" && !clotcedar && round < 2)
        {
            Assert.DoesNotContain(ShopStock.AvailableOffers(merchant, round), o => o.ItemDef?.Moniker == moniker);
            return;
        }

        Assert.Contains(ShopStock.AvailableOffers(merchant, round), o => o.ItemDef?.Moniker == moniker);
    }

    private static void Light(BodyTestHarness harness, ItemDef def)
    {
        var props = def.IncenseProperties!;
        harness.Pawn.ActiveIncense.Add(new ActiveIncense
        {
            Def = props.Effect.Def,
            EncountersRemaining = props.GetDurationInEncounters(),
            SourceMoniker = def.Moniker
        });
    }

    private static void DestroyArmArtery(BodyTestHarness harness)
    {
        var arm = harness.External(BodyPartType.Arm);
        arm.HitPoints = arm.MaxHitPoints * 0.4;
        arm.InternalParts.First(p => p.Type == BodyPartType.Artery).HitPoints = 0;
    }
}
