using Wendlemire.Definitions;
using Wendlemire.Sim.Entities.Pawns;
using Xunit;

namespace Wendlemire.Tests;

[Collection("Sim")]
public class BloodLossTests
{
    public BloodLossTests()
    {
        TestData.EnsureLoaded();
    }

    [Fact]
    public void HealthyBodyDoesNotLoseBlood()
    {
        using var harness = BodyTestHarness.Human();
        var before = harness.Pawn.Body.BloodAmount;

        harness.TickBody();

        Assert.Equal(before, harness.Pawn.Body.BloodAmount);
        Assert.Equal(0, harness.Pawn.Body.BloodChangeLastFrame);
    }

    [Fact]
    public void FleshWoundDrainsBlood()
    {
        using var harness = BodyTestHarness.Human();
        var arm = harness.External(BodyPartType.Arm);
        arm.HitPoints = arm.MaxHitPoints * 0.5;
        var before = harness.Pawn.Body.BloodAmount;

        harness.TickBody();

        Assert.True(harness.Pawn.Body.BloodAmount < before);
    }

    [Fact]
    public void WorseWoundDrainsFaster()
    {
        using var mild = BodyTestHarness.Human();
        using var severe = BodyTestHarness.Human();
        mild.External(BodyPartType.Arm).HitPoints = mild.External(BodyPartType.Arm).MaxHitPoints * 0.5;
        severe.External(BodyPartType.Arm).HitPoints = severe.External(BodyPartType.Arm).MaxHitPoints * 0.1;
        var mildBefore = mild.Pawn.Body.BloodAmount;
        var severeBefore = severe.Pawn.Body.BloodAmount;

        mild.TickBody();
        severe.TickBody();

        var mildLoss = mildBefore - mild.Pawn.Body.BloodAmount;
        var severeLoss = severeBefore - severe.Pawn.Body.BloodAmount;
        Assert.True(severeLoss > mildLoss);
    }

    [Fact]
    public void DestroyedArteryAddsLossAndStopsBranchTraversal()
    {
        using var withHandWound = BodyTestHarness.Human();
        using var arteryOnly = BodyTestHarness.Human();

        WoundArmAndDestroyArtery(withHandWound, woundHand: true);
        WoundArmAndDestroyArtery(arteryOnly, woundHand: false);

        var withHandBefore = withHandWound.Pawn.Body.BloodAmount;
        var arteryOnlyBefore = arteryOnly.Pawn.Body.BloodAmount;
        withHandWound.TickBody();
        arteryOnly.TickBody();

        var withHandLoss = withHandBefore - withHandWound.Pawn.Body.BloodAmount;
        var arteryOnlyLoss = arteryOnlyBefore - arteryOnly.Pawn.Body.BloodAmount;
        Assert.Equal(arteryOnlyLoss, withHandLoss, 3);
        Assert.True(arteryOnlyLoss > 0);
    }

    [Fact]
    public void IntactArteryAllowsHandWoundToDrain()
    {
        using var healthyHand = BodyTestHarness.Human();
        using var woundedHand = BodyTestHarness.Human();
        WoundArmKeepArtery(healthyHand, woundHand: false);
        WoundArmKeepArtery(woundedHand, woundHand: true);

        var healthyBefore = healthyHand.Pawn.Body.BloodAmount;
        var woundedBefore = woundedHand.Pawn.Body.BloodAmount;
        healthyHand.TickBody();
        woundedHand.TickBody();

        var healthyLoss = healthyBefore - healthyHand.Pawn.Body.BloodAmount;
        var woundedLoss = woundedBefore - woundedHand.Pawn.Body.BloodAmount;
        Assert.True(woundedLoss > healthyLoss);
    }

    [Fact]
    public void UnsealedSocketHemorrhagesMoreThanSealed()
    {
        using var unsealed = BodyTestHarness.Human();
        using var sealedSocket = BodyTestHarness.Human();
        var unsealedArm = unsealed.External(BodyPartType.Arm);
        var sealedArm = sealedSocket.External(BodyPartType.Arm);
        var unsealedParent = unsealedArm.Socket!;
        var sealedParent = sealedArm.Socket!;
        unsealedArm.Severe();
        sealedArm.Severe();
        sealedParent.IsSealed = true;
        Assert.False(unsealedParent.IsSealed);
        Assert.True(sealedParent.IsSealed);

        var unsealedBefore = unsealed.Pawn.Body.BloodAmount;
        var sealedBefore = sealedSocket.Pawn.Body.BloodAmount;
        unsealed.TickBody();
        sealedSocket.TickBody();

        var unsealedLoss = unsealedBefore - unsealed.Pawn.Body.BloodAmount;
        var sealedLoss = sealedBefore - sealedSocket.Pawn.Body.BloodAmount;
        Assert.True(unsealedLoss > sealedLoss);
        Assert.Equal(0, sealedLoss, 3);
    }

    [Fact]
    public void ThickBloodedSlowsBloodLoss()
    {
        using var control = BodyTestHarness.Human();
        using var thick = BodyTestHarness.Human();
        thick.Pawn.Traits.Add(Defs.Traits.ThickBlooded);
        Assert.Equal(1.2f, thick.Pawn.Body.Handler.ViscosityModifier);

        control.External(BodyPartType.Arm).HitPoints = control.External(BodyPartType.Arm).MaxHitPoints * 0.4;
        thick.External(BodyPartType.Arm).HitPoints = thick.External(BodyPartType.Arm).MaxHitPoints * 0.4;

        var controlBefore = control.Pawn.Body.BloodAmount;
        var thickBefore = thick.Pawn.Body.BloodAmount;
        control.TickBody();
        thick.TickBody();

        var controlLoss = controlBefore - control.Pawn.Body.BloodAmount;
        var thickLoss = thickBefore - thick.Pawn.Body.BloodAmount;
        Assert.True(thickLoss > 0);
        Assert.Equal(controlLoss / 1.2f, thickLoss, 3);
    }

    [Fact]
    public void GhoulDoesNotLoseBloodFromWounds()
    {
        using var harness = BodyTestHarness.Ghoul();
        var arm = harness.Pawn.Body.AllExternalParts.First(p => p.Type == BodyPartType.Arm);
        arm.HitPoints = arm.MaxHitPoints * 0.2;
        var before = harness.Pawn.Body.BloodAmount;

        harness.TickBody(10);

        Assert.Equal(before, harness.Pawn.Body.BloodAmount);
        Assert.False(harness.Pawn.IsDead);
    }

    [Fact]
    public void SeveringArmDropsBloodBySubtreeFraction()
    {
        using var harness = BodyTestHarness.Human();
        var arm = harness.External(BodyPartType.Arm);
        var bodyWeight = harness.Pawn.Body.AllParts.Sum(p => p.BloodAmount);
        var subtreeWeight = arm.GetSubtreeBloodWeight();
        var before = harness.Pawn.Body.BloodAmount;
        var expectedLoss = before * (subtreeWeight / bodyWeight);

        arm.Severe();

        Assert.Equal(before - expectedLoss, harness.Pawn.Body.BloodAmount, 3);
        Assert.True(expectedLoss > 0);
    }

    [Fact]
    public void SeveringFingerDropsLessBloodThanArm()
    {
        using var armHarness = BodyTestHarness.Human();
        using var fingerHarness = BodyTestHarness.Human();
        var armBefore = armHarness.Pawn.Body.BloodAmount;
        var fingerBefore = fingerHarness.Pawn.Body.BloodAmount;

        armHarness.External(BodyPartType.Arm).Severe();
        fingerHarness.External(BodyPartType.Finger).Severe();

        var armLoss = armBefore - armHarness.Pawn.Body.BloodAmount;
        var fingerLoss = fingerBefore - fingerHarness.Pawn.Body.BloodAmount;
        Assert.True(fingerLoss > 0);
        Assert.True(fingerLoss < armLoss);
    }

    [Fact]
    public void SeveringArmAtHalfBloodLosesHalfAsMuch()
    {
        using var full = BodyTestHarness.Human();
        using var half = BodyTestHarness.Human();
        half.Pawn.Body.BloodAmount = half.Pawn.Body.MaxBlood * 0.5f;
        var fullBefore = full.Pawn.Body.BloodAmount;
        var halfBefore = half.Pawn.Body.BloodAmount;

        full.External(BodyPartType.Arm).Severe();
        half.External(BodyPartType.Arm).Severe();

        var fullLoss = fullBefore - full.Pawn.Body.BloodAmount;
        var halfLoss = halfBefore - half.Pawn.Body.BloodAmount;
        Assert.Equal(fullLoss / 2f, halfLoss, 3);
    }

    [Fact]
    public void GhoulDoesNotLoseBloodOnSever()
    {
        using var harness = BodyTestHarness.Ghoul();
        var before = harness.Pawn.Body.BloodAmount;
        harness.Pawn.Body.AllExternalParts.First(p => p.Type == BodyPartType.Arm).Severe();

        Assert.Equal(before, harness.Pawn.Body.BloodAmount);
    }

    [Fact]
    public void BloodAtOrBelowOneKillsFromBloodLoss()
    {
        using var harness = BodyTestHarness.Human();
        DeathRecord? death = null;
        harness.Pawn.Died += e => death = e.Record;
        harness.Pawn.Body.BloodAmount = 1;

        harness.TickBody();

        Assert.True(harness.Pawn.IsDead);
        Assert.NotNull(death);
        Assert.Equal("Blood loss", death.CauseOfDeath);
    }

    private static void WoundArmAndDestroyArtery(BodyTestHarness harness, bool woundHand)
    {
        var arm = harness.External(BodyPartType.Arm);
        arm.HitPoints = arm.MaxHitPoints * 0.5;
        var artery = arm.InternalParts.First(p => p.Type == BodyPartType.Artery);
        artery.HitPoints = 0;
        var hand = arm.ExternalParts.First(p => p.Type == BodyPartType.Hand);
        hand.HitPoints = woundHand ? hand.MaxHitPoints * 0.2 : hand.MaxHitPoints;
    }

    private static void WoundArmKeepArtery(BodyTestHarness harness, bool woundHand)
    {
        var arm = harness.External(BodyPartType.Arm);
        arm.HitPoints = arm.MaxHitPoints * 0.5;
        var hand = arm.ExternalParts.First(p => p.Type == BodyPartType.Hand);
        hand.HitPoints = woundHand ? hand.MaxHitPoints * 0.2 : hand.MaxHitPoints;
    }
}
