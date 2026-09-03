using Wendlemire.Definitions;
using Wendlemire.Sim.Combat;
using Wendlemire.Sim.Entities;
using Xunit;

namespace Wendlemire.Tests;

[Collection("Sim")]
public class ArmorWeightTests
{
    public ArmorWeightTests()
    {
        TestData.EnsureLoaded();
    }

    [Theory]
    [InlineData("ClothVambrace", 1f)]
    [InlineData("LeatherVambrace", 2f)]
    [InlineData("WitchDoctorVambrace", 3f)]
    [InlineData("ChainVambrace", 4f)]
    [InlineData("PlateVambrace", 7f)]
    [InlineData("EvasionCloak", 1f)]
    [InlineData("BucketHelmet", 2f)]
    [InlineData("FishBowlHelmet", 1f)]
    [InlineData("BlessedIronCollar", 2f)]
    public void ArmorPiecesHaveExpectedWeight(string moniker, float expected)
    {
        using var harness = BodyTestHarness.Human();
        var item = harness.CreateItem(moniker);
        Assert.Equal(expected, item.GetStatValue(Defs.Stats.Weight));
    }

    [Fact]
    public void UnarmoredPawnHasZeroWeightAndUnchangedAttackSpeed()
    {
        using var harness = BodyTestHarness.Human();
        var pawn = harness.Pawn;

        Assert.Equal(0f, pawn.Body.EquippedWeight);
        Assert.Equal(0f, pawn.GetStatValue(Defs.Stats.Weight));
        Assert.Equal(1f, pawn.GetStatValue(Defs.Stats.AttackSpeed), 3);
    }

    [Fact]
    public void EquippedWeightSumsMixedPieces()
    {
        using var harness = BodyTestHarness.Human();
        harness.EquipArmor("PlateTunic");
        harness.EquipArmor("ClothHelmet");
        harness.EquipArmor("ClothGorget");

        Assert.Equal(9f, harness.Pawn.Body.EquippedWeight);
        Assert.Equal(9f, harness.Pawn.GetStatValue(Defs.Stats.Weight));
    }

    [Fact]
    public void EquippedWeightSlowsAttackSpeedByFormula()
    {
        using var harness = BodyTestHarness.Human();
        var pawn = harness.Pawn;
        var baseSpeed = pawn.GetStatValue(Defs.Stats.AttackSpeed);

        harness.EquipArmor("PlateTunic");
        harness.EquipArmor("ClothHelmet");
        harness.EquipArmor("ClothGorget");

        var expected = baseSpeed / (1f + 9f * CombatBalance.WeightAttackSpeedFactor);
        Assert.Equal(expected, pawn.GetStatValue(Defs.Stats.AttackSpeed), 4);
    }

    [Fact]
    public void FullClothKitMatchesExpectedPenalty()
    {
        using var harness = BodyTestHarness.Human();
        foreach (var piece in new[]
                 {
                     "ClothHelmet", "ClothGorget", "ClothTunic",
                     "ClothGlove", "ClothGlove",
                     "ClothVambrace", "ClothVambrace",
                     "ClothGreave", "ClothGreave",
                     "ClothBoot", "ClothBoot"
                 })
        {
            harness.EquipArmor(piece);
        }

        Assert.Equal(11f, harness.Pawn.Body.EquippedWeight);
        var expected = 1f / (1f + 11f * CombatBalance.WeightAttackSpeedFactor);
        Assert.Equal(expected, harness.Pawn.GetStatValue(Defs.Stats.AttackSpeed), 4);
    }
}
