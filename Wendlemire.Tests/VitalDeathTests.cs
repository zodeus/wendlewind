using Wendlemire.Definitions;
using Wendlemire.Sim.Combat;
using Wendlemire.Sim.Entities.Pawns;
using Xunit;

namespace Wendlemire.Tests;

[Collection("Sim")]
public class VitalDeathTests
{
    public VitalDeathTests()
    {
        TestData.EnsureLoaded();
    }

    [Fact]
    public void OneDestroyedLungDoesNotKill()
    {
        using var harness = BodyTestHarness.Human();
        var lungs = harness.Parts(BodyPartType.Lung);
        Assert.True(lungs.Count >= 2);
        lungs[0].HitPoints = 0;

        Assert.Null(harness.Pawn.IsDeadFromPartFailure());
        Assert.False(harness.Pawn.IsDead);
    }

    [Fact]
    public void OneDestroyedKidneyDoesNotKill()
    {
        using var harness = BodyTestHarness.Human();
        var kidneys = harness.Parts(BodyPartType.Kidney);
        Assert.True(kidneys.Count >= 2);
        kidneys[0].HitPoints = 0;

        Assert.Null(harness.Pawn.IsDeadFromPartFailure());
    }

    [Fact]
    public void BothLungsDestroyedIsOrganFailure()
    {
        using var harness = BodyTestHarness.Human();
        foreach (var lung in harness.Parts(BodyPartType.Lung))
        {
            lung.HitPoints = 0;
        }

        var death = harness.Pawn.IsDeadFromPartFailure();
        Assert.NotNull(death);
        Assert.Contains("Lung", death.CauseOfDeath);
        Assert.Equal("Organ failure", death.KillingWeapon);
    }

    [Fact]
    public void DestroyedHeartIsOrganFailure()
    {
        using var harness = BodyTestHarness.Human();
        harness.Part(BodyPartType.Heart).HitPoints = 0;

        var death = harness.Pawn.IsDeadFromPartFailure();
        Assert.NotNull(death);
        Assert.Contains("Heart", death.CauseOfDeath);
    }

    [Fact]
    public void DestroyedNonVitalPartDoesNotKill()
    {
        using var harness = BodyTestHarness.Human();
        harness.Part(BodyPartType.Liver).HitPoints = 0;
        harness.Part(BodyPartType.Spleen).HitPoints = 0;
        foreach (var kidney in harness.Parts(BodyPartType.Kidney))
        {
            kidney.HitPoints = 0;
        }

        Assert.Null(harness.Pawn.IsDeadFromPartFailure());
        Assert.False(harness.Pawn.IsDead);
    }

    [Fact]
    public void DestroyedLiverKillsAfterDelay()
    {
        using var harness = BodyTestHarness.Human();
        harness.Part(BodyPartType.Liver).HitPoints = 0;

        harness.TickBody(CombatBalance.DelayedOrganFailureTicks - 1);
        Assert.False(harness.Pawn.IsDead);

        harness.TickBody();
        Assert.True(harness.Pawn.IsDead);
    }

    [Fact]
    public void DestroyedLiverDeathNamesTheOrgan()
    {
        using var harness = BodyTestHarness.Human();
        DeathRecord? death = null;
        harness.Pawn.Died += e => death = e.Record;
        harness.Part(BodyPartType.Liver).HitPoints = 0;
        harness.TickBody(CombatBalance.DelayedOrganFailureTicks);

        Assert.NotNull(death);
        Assert.Equal("Liver", death.FailedOrgan);
        Assert.Contains("Liver failed", death.CauseOfDeath);
    }

    [Fact]
    public void OneDestroyedKidneyIsPendingCrisis()
    {
        using var harness = BodyTestHarness.Human();
        var kidneys = harness.Parts(BodyPartType.Kidney);
        kidneys[0].HitPoints = 0;
        harness.TickBody();

        Assert.True(harness.Pawn.Body.OrganCrisis.IsPending(kidneys[0]));
        Assert.False(harness.Pawn.Body.OrganCrisis.IsActive(BodyPartType.Kidney));
        Assert.False(harness.Pawn.Body.OrganCrisis.TryGetImminent(harness.Pawn.Body, out _));
        OrganCrisis.CountType(harness.Pawn.Body, BodyPartType.Kidney, out var failed, out var total);
        Assert.Equal(1, failed);
        Assert.Equal(2, total);
    }

    [Fact]
    public void OneDestroyedKidneyDoesNotKillAfterDelay()
    {
        using var harness = BodyTestHarness.Human();
        var kidneys = harness.Parts(BodyPartType.Kidney);
        kidneys[0].HitPoints = 0;

        harness.TickBody(CombatBalance.DelayedOrganFailureTicks);
        Assert.False(harness.Pawn.IsDead);
        Assert.Equal(0, harness.Pawn.Body.OrganCrisis.TicksInCrisis(BodyPartType.Kidney));
    }

    [Fact]
    public void BothKidneysDestroyedKillAfterDelay()
    {
        using var harness = BodyTestHarness.Human();
        DeathRecord? death = null;
        harness.Pawn.Died += e => death = e.Record;
        foreach (var kidney in harness.Parts(BodyPartType.Kidney))
        {
            kidney.HitPoints = 0;
        }

        harness.TickBody(CombatBalance.DelayedOrganFailureTicks);
        Assert.True(harness.Pawn.IsDead);
        Assert.NotNull(death);
        Assert.Contains("Kidney", death.CauseOfDeath);
    }

    [Fact]
    public void DestroyedSpleenDoesNotKillAfterDelay()
    {
        using var harness = BodyTestHarness.Human();
        harness.Part(BodyPartType.Spleen).HitPoints = 0;

        harness.TickBody(CombatBalance.DelayedOrganFailureTicks);
        Assert.False(harness.Pawn.IsDead);
    }

    [Fact]
    public void HealedLiverCancelsCrisis()
    {
        using var harness = BodyTestHarness.Human();
        var liver = harness.Part(BodyPartType.Liver);
        liver.HitPoints = 0;
        harness.TickBody(CombatBalance.DelayedOrganFailureTicks / 2);
        Assert.True(harness.Pawn.Body.OrganCrisis.TicksInCrisis(BodyPartType.Liver) > 0);

        liver.HitPoints = liver.MaxHitPoints;
        harness.TickBody(CombatBalance.DelayedOrganFailureTicks);
        Assert.False(harness.Pawn.IsDead);
        Assert.Equal(0, harness.Pawn.Body.OrganCrisis.TicksInCrisis(BodyPartType.Liver));
        Assert.False(harness.Pawn.Body.OrganCrisis.TryGetImminent(harness.Pawn.Body, out _));
    }

    [Fact]
    public void DestroyedLiverReportsImminentCrisis()
    {
        using var harness = BodyTestHarness.Human();
        harness.Part(BodyPartType.Liver).HitPoints = 0;
        harness.TickBody();

        Assert.True(harness.Pawn.Body.OrganCrisis.TryGetImminent(harness.Pawn.Body, out var crisis));
        Assert.Equal("Liver", crisis.Label);
        Assert.Equal(BodyPartType.Liver, crisis.Type);
        Assert.True(crisis.TicksRemaining < CombatBalance.DelayedOrganFailureTicks);
        Assert.True(crisis.TicksRemaining > 0);
    }

    [Fact]
    public void HeavilyFesteredLiverKillsAfterDelay()
    {
        using var harness = BodyTestHarness.Human();
        DeathRecord? death = null;
        harness.Pawn.Died += e => death = e.Record;
        var liver = harness.Part(BodyPartType.Liver);
        liver.HitPoints = liver.MaxHitPoints * CombatBalance.DelayedOrganFesterHealth;
        liver.TryAddModifier(harness.Context.Factory.CreateModifier(Defs.BodyPartModifiers.Festering, 2000, 1));

        harness.TickBody(CombatBalance.DelayedOrganFailureTicks);
        Assert.True(harness.Pawn.IsDead);
        Assert.NotNull(death);
        Assert.Contains("Liver", death.CauseOfDeath);
    }

    [Fact]
    public void TakeDamageAttributesKillingBlowWhenStrikeFinishesLastVital()
    {
        using var harness = BodyTestHarness.Human();
        var attacker = harness.CreatePawn("HumanA", "Attacker");
        var heart = harness.Part(BodyPartType.Heart);
        heart.HitPoints = 0.15;
        var ribCage = harness.Part(BodyPartType.RibCage);
        ribCage.HitPoints = ribCage.MaxHitPoints * 0.05;
        DeathRecord? death = null;
        harness.Pawn.Died += e => death = e.Record;
        harness.UseAlwaysHitRng();

        harness.Strike(attacker, harness.External(BodyPartType.Torso), 500);

        Assert.True(harness.Pawn.IsDead);
        Assert.NotNull(death);
        Assert.False(string.IsNullOrWhiteSpace(death.CauseOfDeath));
        Assert.True(
            death.CauseOfDeath.Contains("Heart", StringComparison.OrdinalIgnoreCase)
            || death.CauseOfDeath.Contains("Lung", StringComparison.OrdinalIgnoreCase)
            || death.CauseOfDeath.Contains("organ", StringComparison.OrdinalIgnoreCase),
            death.CauseOfDeath);
        Assert.False(string.IsNullOrWhiteSpace(death.KillingWeapon));
    }

    [Fact]
    public void TakeDamageKillsWhenLastVitalIsAlreadyFailed()
    {
        using var harness = BodyTestHarness.Human();
        var attacker = harness.CreatePawn("HumanA", "Attacker");
        harness.Part(BodyPartType.Heart).HitPoints = 0;
        DeathRecord? death = null;
        harness.Pawn.Died += e => death = e.Record;
        harness.UseAlwaysHitRng();

        harness.Strike(attacker, harness.External(BodyPartType.Arm), 4);

        Assert.True(harness.Pawn.IsDead);
        Assert.NotNull(death);
        Assert.StartsWith("Heart failed", death.CauseOfDeath);
    }

    [Fact]
    public void DestroyedTorsoKillsOnTickEvenWhenOrgansAreFull()
    {
        using var harness = BodyTestHarness.Human();
        DeathRecord? death = null;
        harness.Pawn.Died += e => death = e.Record;
        harness.External(BodyPartType.Torso).HitPoints = 0;
        Assert.False(harness.Pawn.IsDead);
        Assert.All(harness.Parts(BodyPartType.Heart), h => Assert.True(h.HitPoints > 1));
        Assert.All(harness.Parts(BodyPartType.Lung), l => Assert.True(l.HitPoints > 1));

        harness.TickBody();

        Assert.True(harness.Pawn.IsDead);
        Assert.NotNull(death);
        Assert.Contains("structural collapse", death.CauseOfDeath);
        Assert.False(string.IsNullOrWhiteSpace(death.FailedOrgan));
    }

    [Fact]
    public void DestroyedArmCrushesInternalsAndDoesNotKill()
    {
        using var harness = BodyTestHarness.Human();
        var arm = harness.External(BodyPartType.Arm);
        var internals = arm.InternalParts.Where(p => !p.IsDestroyed).ToList();
        Assert.NotEmpty(internals);
        var starting = internals.ToDictionary(p => p, p => p.HitPoints);
        arm.HitPoints = 0;

        harness.TickBody(60);

        Assert.False(harness.Pawn.IsDead);
        Assert.Contains(internals, p => p.HitPoints < starting[p]);
    }

    [Fact]
    public void NecrosisOnTorsoPenetratesInternals()
    {
        using var harness = BodyTestHarness.Human();
        var torso = harness.External(BodyPartType.Torso);
        var necrosis = harness.Context.Factory.CreateModifier(Defs.BodyPartModifiers.Necrosis, 2000, 1);
        Assert.True(necrosis.ApplyToPart(torso));
        torso.HitPoints = torso.MaxHitPoints * 0.74;

        harness.TickBody();

        Assert.Contains(torso.InternalParts, p => p.HasModifier(Defs.BodyPartModifiers.Necrosis));
    }
}
