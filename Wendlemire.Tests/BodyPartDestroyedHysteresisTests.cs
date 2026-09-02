using Wendlemire.Definitions;
using Wendlemire.Sim.Entities.Pawns;
using Xunit;

namespace Wendlemire.Tests;

[Collection("Sim")]
public class BodyPartDestroyedHysteresisTests
{
    public BodyPartDestroyedHysteresisTests()
    {
        TestData.EnsureLoaded();
    }

    [Fact]
    public void OscillatingHitPointsDoNotFlipDestroyedEveryTick()
    {
        using var harness = BodyTestHarness.Human();
        var part = harness.External(BodyPartType.Eye);
        part.HitPoints = 0;
        Assert.True(part.IsDestroyed);

        var flips = 0;
        var wasDestroyed = part.IsDestroyed;
        for (var i = 0; i < 40; i++)
        {
            part.HitPoints = i % 2 == 0 ? 0 : Math.Min(1, part.MaxHitPoints);
            part.Tick();
            if (part.IsDestroyed != wasDestroyed)
            {
                flips++;
                wasDestroyed = part.IsDestroyed;
            }
        }

        Assert.True(part.IsDestroyed);
        Assert.Equal(0, flips);
    }

    [Fact]
    public void StableHealingRecoversAfterHold()
    {
        using var harness = BodyTestHarness.Human();
        var part = harness.External(BodyPartType.Eye);
        part.HitPoints = 0;
        Assert.True(part.IsDestroyed);

        part.HitPoints = Math.Min(part.MaxHitPoints, Math.Max(1, part.DestroyedRecoverHitPoints));
        for (var i = 0; i < BodyPart.DestroyedRecoverHoldTicks; i++)
        {
            Assert.True(part.IsDestroyed);
            part.Tick();
        }

        Assert.False(part.IsDestroyed);
    }

    [Fact]
    public void FullHealClearsDestroyedImmediately()
    {
        using var harness = BodyTestHarness.Human();
        var part = harness.External(BodyPartType.Eye);
        part.HitPoints = 0;
        Assert.True(part.IsDestroyed);

        part.HitPoints = part.MaxHitPoints;
        Assert.False(part.IsDestroyed);
    }

    [Fact]
    public void RegenVersusStrongDotStaysDestroyed()
    {
        using var harness = BodyTestHarness.Human();
        var part = harness.External(BodyPartType.Eye);
        part.HitPoints = 0;
        part.TryAddModifier(harness.Context.Factory.CreateModifier(Defs.BodyPartModifiers.HealthRegeneration, 2000, 1));
        part.TryAddModifier(harness.Context.Factory.CreateModifier(Defs.BodyPartModifiers.Acid, 2000, 30));

        var flips = 0;
        var wasDestroyed = part.IsDestroyed;
        for (var i = 0; i < 90; i++)
        {
            part.Tick();
            if (part.IsDestroyed != wasDestroyed)
            {
                flips++;
                wasDestroyed = part.IsDestroyed;
            }
        }

        Assert.True(part.IsDestroyed);
        Assert.Equal(0, flips);
    }

    [Fact]
    public void GradualClimbToMaxDoesNotRecoverUntilHold()
    {
        using var harness = BodyTestHarness.Human();
        var part = harness.External(BodyPartType.Eye);
        part.HitPoints = 0;
        while (part.HitPoints < part.MaxHitPoints)
        {
            part.HitPoints = Math.Min(part.MaxHitPoints, part.HitPoints + 0.1);
        }

        Assert.True(part.IsDestroyed);
        for (var i = 0; i < BodyPart.DestroyedRecoverHoldTicks; i++)
        {
            part.Tick();
        }

        Assert.False(part.IsDestroyed);
    }

    [Fact]
    public void ChildStaysNonFunctionalWhileParentOscillates()
    {
        using var harness = BodyTestHarness.Human();
        var parent = harness.External(BodyPartType.Head);
        var child = harness.External(BodyPartType.Eye);
        parent.HitPoints = 0;
        Assert.True(parent.IsDestroyed);
        Assert.False(child.IsFunctional);

        var flips = 0;
        var wasFunctional = child.IsFunctional;
        for (var i = 0; i < 40; i++)
        {
            parent.HitPoints = i % 2 == 0 ? 0 : Math.Min(1, parent.MaxHitPoints);
            parent.Tick();
            if (child.IsFunctional != wasFunctional)
            {
                flips++;
                wasFunctional = child.IsFunctional;
            }
        }

        Assert.False(child.IsFunctional);
        Assert.Equal(0, flips);
    }
}
