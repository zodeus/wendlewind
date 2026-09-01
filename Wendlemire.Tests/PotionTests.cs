using Wendlemire.Definitions;
using Wendlemire.Sim.Arena;
using Wendlemire.Sim.Entities;
using Wendlemire.Sim.Entities.Items;
using Wendlemire.Sim.Entities.Pawns;
using Wendlemire.Sim.Entities.Pawns.Bodies.Handlers;
using Xunit;

namespace Wendlemire.Tests;

[Collection("Sim")]
public class PotionTests
{
    public PotionTests()
    {
        TestData.EnsureLoaded();
    }

    [Fact]
    public void PitchbloodThickensBloodAndSlowsLoss()
    {
        using var control = BodyTestHarness.Human();
        using var treated = BodyTestHarness.Human();
        DestroyArmArtery(control);
        DestroyArmArtery(treated);

        var potion = treated.Context.Factory.CreateEntity<Item>(Defs.Items.Pitchblood);
        var result = potion.PotionHandler!.UseInCombat(treated.Pawn);

        Assert.True(result.Success);
        Assert.True(treated.Pawn.Body.Effects.Has(Defs.BodyEffects.Pitchblood));
        Assert.Equal(
            DefaultBodyHandler.PitchbloodViscosityFactor,
            treated.Pawn.Body.Handler.ViscosityModifier);

        var controlBefore = control.Pawn.Body.BloodAmount;
        var treatedBefore = treated.Pawn.Body.BloodAmount;
        control.TickBody();
        treated.TickBody();

        var controlLoss = controlBefore - control.Pawn.Body.BloodAmount;
        var treatedLoss = treatedBefore - treated.Pawn.Body.BloodAmount;
        Assert.True(treatedLoss > 0);
        Assert.Equal(controlLoss / DefaultBodyHandler.PitchbloodViscosityFactor, treatedLoss, 3);
    }

    [Fact]
    public void TallowFlaskGumsLegsAndSlowsStrikes()
    {
        using var harness = BodyTestHarness.Human();
        var target = harness.CreatePawn("HumanA", "Target");
        var mobilityBefore = target.Body.Capabilities.Mobility;
        var attackSpeedBefore = target.GetStatValue(Defs.Stats.AttackSpeed);

        var potion = harness.Context.Factory.CreateEntity<Item>(Defs.Items.TallowFlask);
        var result = potion.PotionHandler!.UseInCombat(harness.Pawn, target);

        Assert.True(result.Success);
        Assert.True(target.Body.Effects.Has(Defs.BodyEffects.Tallowed));
        Assert.Equal(mobilityBefore * PawnCapabilities.TallowedMobilityFactor, target.Body.Capabilities.Mobility, 3);
        Assert.Equal(attackSpeedBefore * 0.75f, target.GetStatValue(Defs.Stats.AttackSpeed), 3);
    }

    [Theory]
    [InlineData("GeneralStore")]
    [InlineData("Blacksmith")]
    public void EarlyShopsStockPitchbloodAndTallow(string merchantMoniker)
    {
        var merchant = DefRepository<MerchantDef>.GetByMoniker(merchantMoniker)!;
        var offers = merchant.AllOffers.Where(o => !o.IsSet).Select(o => o.ItemDef!.Moniker).ToHashSet();

        Assert.Contains("Pitchblood", offers);
        Assert.Contains("TallowFlask", offers);
        Assert.Contains(ShopStock.AvailableOffers(merchant, 0), o => o.ItemDef?.Moniker == "Pitchblood");
        Assert.Contains(ShopStock.AvailableOffers(merchant, 0), o => o.ItemDef?.Moniker == "TallowFlask");
    }

    private static void DestroyArmArtery(BodyTestHarness harness)
    {
        var arm = harness.External(BodyPartType.Arm);
        arm.HitPoints = arm.MaxHitPoints * 0.4;
        arm.InternalParts.First(p => p.Type == BodyPartType.Artery).HitPoints = 0;
    }
}
