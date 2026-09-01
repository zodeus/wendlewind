using Wendlemire.Sim.Combat;
using Wendlemire.Sim.Entities.Pawns;
using Xunit;

namespace Wendlemire.Tests;

[Collection("Sim")]
public class BodyDamageTests
{
    public BodyDamageTests()
    {
        TestData.EnsureLoaded();
    }

    [Fact]
    public void ApplyDamageAbsorbsHalfAndReturnsRemainder()
    {
        using var harness = BodyTestHarness.Human();
        var arm = harness.External(BodyPartType.Arm);
        var amount = Math.Min(10, arm.MaxHitPoints);
        var before = arm.HitPoints;

        var remaining = arm.ApplyDamage(BodyTestHarness.Damage(amount), [], cascade: false);

        Assert.Equal(amount * 0.5, before - arm.HitPoints, 5);
        Assert.Equal(amount * 0.5, remaining, 5);
    }

    [Fact]
    public void DestroyedLayerPassesFullDamageThrough()
    {
        using var harness = BodyTestHarness.Human();
        var arm = harness.External(BodyPartType.Arm);
        arm.HitPoints = 0;

        var remaining = arm.ApplyDamage(BodyTestHarness.Damage(10), [], cascade: false);

        Assert.Equal(0, arm.HitPoints);
        Assert.Equal(10, remaining, 5);
    }

    [Fact]
    public void SubstanceModifierScalesAbsorbedDamage()
    {
        using var harness = BodyTestHarness.Human();
        var arm = harness.External(BodyPartType.Arm);
        var amount = Math.Min(8, arm.MaxHitPoints / 4);
        var before = arm.HitPoints;

        arm.ApplyDamage(BodyTestHarness.Damage(amount, _ => 2f), [], cascade: false);

        Assert.Equal(amount, before - arm.HitPoints, 5);
    }

    [Fact]
    public void IntactChitinBlocksInternalCascade()
    {
        using var harness = BodyTestHarness.Human();
        var arm = harness.External(BodyPartType.Arm);
        arm.SetSubstanceOverride(SubstanceType.Chitin);
        arm.IsCracked = false;
        var internalsBefore = arm.InternalParts.ToDictionary(p => p, p => p.HitPoints);

        var remaining = arm.CascadeDamageToInternalParts(BodyTestHarness.Damage(50), []);

        Assert.Equal(0, remaining);
        Assert.All(arm.InternalParts, p => Assert.Equal(internalsBefore[p], p.HitPoints));
    }

    [Fact]
    public void CrackedChitinAllowsInternalHits()
    {
        using var harness = BodyTestHarness.Human();
        var arm = harness.External(BodyPartType.Arm);
        arm.SetSubstanceOverride(SubstanceType.Chitin);
        arm.IsCracked = true;
        var skin = arm.InternalParts.First(p => p.Type == BodyPartType.Skin);
        var skinBefore = skin.HitPoints;

        arm.CascadeDamageToInternalParts(BodyTestHarness.Damage(20), []);

        Assert.True(skin.HitPoints < skinBefore);
    }

    [Fact]
    public void IntactRibCageBlocksOrgans()
    {
        using var harness = BodyTestHarness.Human();
        var ribCage = harness.Part(BodyPartType.RibCage);
        ribCage.HitPoints = ribCage.MaxHitPoints;
        var organs = ribCage.InternalParts.Where(p => p.IsOrgan).ToList();
        Assert.NotEmpty(organs);
        var before = organs.ToDictionary(p => p, p => p.HitPoints);

        ribCage.CascadeDamageToInternalParts(BodyTestHarness.Damage(80), []);

        Assert.All(organs, p => Assert.Equal(before[p], p.HitPoints));
    }

    [Fact]
    public void ShatteredRibCageAllowsOrganHits()
    {
        using var harness = BodyTestHarness.Human();
        var ribCage = harness.Part(BodyPartType.RibCage);
        ribCage.HitPoints = ribCage.MaxHitPoints * 0.05;
        var organs = ribCage.InternalParts.Where(p => p.IsOrgan).ToList();
        var before = organs.Sum(p => p.HitPoints);

        ribCage.CascadeDamageToInternalParts(BodyTestHarness.Damage(80), []);

        Assert.True(organs.Sum(p => p.HitPoints) < before);
    }

    [Fact]
    public void StomachIsSkippedWhenParentHealthIsAboveHalf()
    {
        using var harness = BodyTestHarness.Human();
        var torso = harness.External(BodyPartType.Torso);
        torso.HitPoints = torso.MaxHitPoints * 0.8;
        var stomach = torso.InternalParts.First(p => p.Type == BodyPartType.Stomach);
        var before = stomach.HitPoints;

        torso.CascadeDamageToInternalParts(BodyTestHarness.Damage(100), []);

        Assert.Equal(before, stomach.HitPoints);
    }

    [Fact]
    public void StomachCanBeHitWhenParentHealthIsAtOrBelowHalf()
    {
        var hit = false;
        for (var seed = 1; seed <= 40 && !hit; seed++)
        {
            using var harness = BodyTestHarness.Human(seed);
            var torso = harness.External(BodyPartType.Torso);
            torso.HitPoints = torso.MaxHitPoints * 0.4;
            var stomach = torso.InternalParts.First(p => p.Type == BodyPartType.Stomach);
            var before = stomach.HitPoints;
            torso.CascadeDamageToInternalParts(BodyTestHarness.Damage(200), []);
            hit = stomach.HitPoints < before;
        }

        Assert.True(hit);
    }

    [Fact]
    public void HealthyParentMakesArteryUnhittable()
    {
        using var harness = BodyTestHarness.Human();
        var arm = harness.External(BodyPartType.Arm);
        arm.HitPoints = arm.MaxHitPoints;
        var artery = arm.InternalParts.First(p => p.Type == BodyPartType.Artery);
        var before = artery.HitPoints;

        arm.CascadeDamageToInternalParts(BodyTestHarness.Damage(80), []);

        Assert.Equal(before, artery.HitPoints);
    }

    [Fact]
    public void NearDestroyedParentAllowsArteryHit()
    {
        using var harness = BodyTestHarness.Human();
        var arm = harness.External(BodyPartType.Arm);
        arm.HitPoints = arm.MaxHitPoints * 0.01;
        var artery = arm.InternalParts.First(p => p.Type == BodyPartType.Artery);
        var before = artery.HitPoints;

        arm.CascadeDamageToInternalParts(BodyTestHarness.Damage(80), []);

        Assert.True(artery.HitPoints < before);
    }

    [Fact]
    public void CascadeHitsAtMostFourOrgansOnTorso()
    {
        using var harness = BodyTestHarness.Human();
        var torso = harness.External(BodyPartType.Torso);
        var damaged = new List<DamagedBodyPartRecord>();

        torso.CascadeDamageToInternalParts(BodyTestHarness.Damage(500), damaged);

        var torsoOrgansHit = damaged.Count(r => r.BodyPart.IsOrgan && r.BodyPart.Socket?.ParentPart == torso);
        Assert.InRange(torsoOrgansHit, 0, 4);
    }

    [Fact]
    public void LimbDoesNotSeverWhileInternalsRemain()
    {
        using var harness = BodyTestHarness.Human();
        harness.UseChanceSuccessRng();
        var finger = harness.External(BodyPartType.Finger);
        finger.HitPoints = 0;
        Assert.Contains(finger.AllInternalParts, p => !p.IsDestroyed);

        finger.PotentiallySevereLimb();

        Assert.False(finger.IsSevered);
        Assert.NotNull(finger.Socket);
    }

    [Fact]
    public void EyeNeverSevers()
    {
        using var harness = BodyTestHarness.Human();
        harness.UseChanceSuccessRng();
        var eye = harness.External(BodyPartType.Eye);
        eye.HitPoints = 0;
        foreach (var internalPart in eye.AllInternalParts)
        {
            internalPart.HitPoints = 0;
        }

        eye.PotentiallySevereLimb();

        Assert.False(eye.IsSevered);
        Assert.NotNull(eye.Socket);
    }

    [Fact]
    public void DestroyedLimbSeversWhenRngSucceeds()
    {
        using var harness = BodyTestHarness.Human();
        harness.UseChanceSuccessRng();
        var finger = harness.External(BodyPartType.Finger);
        DestroyPartAndInternals(finger);

        finger.PotentiallySevereLimb();

        Assert.True(finger.IsSevered);
        Assert.Null(finger.Socket);
    }

    [Fact]
    public void DestroyedLimbDoesNotSeverWhenRngFails()
    {
        using var harness = BodyTestHarness.Human();
        harness.UseChanceFailRng();
        var finger = harness.External(BodyPartType.Finger);
        DestroyPartAndInternals(finger);

        finger.PotentiallySevereLimb();

        Assert.False(finger.IsSevered);
        Assert.NotNull(finger.Socket);
    }

    [Fact]
    public void RootSocketNeverSevers()
    {
        using var harness = BodyTestHarness.Human();
        harness.UseChanceSuccessRng();
        var head = harness.Pawn.Body.RootSocket.AttachedPart!;
        DestroyPartAndInternals(head);

        head.PotentiallySevereLimb();
        head.Severe();

        Assert.Same(harness.Pawn.Body.RootSocket, head.Socket);
        Assert.False(head.IsSevered);
    }

    private static void DestroyPartAndInternals(BodyPart part)
    {
        part.HitPoints = 0;
        foreach (var internalPart in part.AllInternalParts)
        {
            internalPart.HitPoints = 0;
        }
    }
}
