using Wendlemire.Definitions;
using Wendlemire.Sim.Combat;
using Wendlemire.Sim.Entities;
using Wendlemire.Sim.Entities.Pawns;
using Xunit;

namespace Wendlemire.Tests;

[Collection("Sim")]
public class HumanHpScaleTests
{
    public HumanHpScaleTests()
    {
        TestData.EnsureLoaded();
    }

    [Theory]
    [InlineData(BodyPartType.Head)]
    [InlineData(BodyPartType.Neck)]
    [InlineData(BodyPartType.Torso)]
    [InlineData(BodyPartType.Arm)]
    [InlineData(BodyPartType.Hand)]
    [InlineData(BodyPartType.Leg)]
    [InlineData(BodyPartType.Foot)]
    public void HumanExternalHpIsXmlTimesCombatBalance(BodyPartType type)
    {
        using var harness = BodyTestHarness.Human();
        var part = harness.External(type);
        var xmlHp = part.GetStatValue(Defs.Stats.MaxHitPoints);
        var afterSize = Math.Floor(xmlHp * harness.Pawn.Body.BodySizeFactor);
        var expected = Math.Floor(afterSize * CombatBalance.ScaleFor(type));

        Assert.Equal(expected, part.MaxHitPoints);
        Assert.Equal(part.MaxHitPoints, part.HitPoints);
    }

    [Theory]
    [InlineData(BodyPartType.Head)]
    [InlineData(BodyPartType.Torso)]
    [InlineData(BodyPartType.Arm)]
    public void OrcExternalHpIsUnscaledXml(BodyPartType type)
    {
        using var harness = BodyTestHarness.Orc();
        var part = harness.External(type);
        var xmlHp = part.GetStatValue(Defs.Stats.MaxHitPoints);
        var expected = Math.Floor(xmlHp * harness.Pawn.Body.BodySizeFactor);
        var scaled = Math.Floor(expected * CombatBalance.ScaleFor(type));

        Assert.Equal(expected, part.MaxHitPoints);
        Assert.NotEqual(scaled, part.MaxHitPoints);
    }
}
