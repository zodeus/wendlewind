using Wendlemire.Definitions;
using Wendlemire.Sim.Combat;
using Wendlemire.Sim.Entities;
using Wendlemire.Sim.Entities.Items.Equipment;
using Wendlemire.Sim.Entities.Pawns;
using Xunit;

namespace Wendlemire.Tests;

[Collection("Sim")]
public class SetBonusTests
{
    private const double BasicMidHit = 40;
    private const int BlockPrecision = 2;
    private const float WitchDoctorPhys = 32f;
    private const float WitchDoctorMagic = 10f;

    private static readonly string[] WitchDoctorPieces =
    [
        "WitchDoctorHelmet", "WitchDoctorGorget", "WitchDoctorTunic",
        "WitchDoctorVambrace", "WitchDoctorGreave", "WitchDoctorBoot"
    ];

    public SetBonusTests()
    {
        TestData.EnsureLoaded();
    }

    [Fact]
    public void ArmorFamiliesDeclareTheirSet()
    {
        using var harness = BodyTestHarness.Human();
        Assert.Equal(SetBonuses.WitchDoctor, harness.CreateItem("WitchDoctorVambrace").ItemDef.EquipmentProperties?.ArmorSet);
        Assert.Equal(SetBonuses.Plate, harness.CreateItem("PlateHelmet").ItemDef.EquipmentProperties?.ArmorSet);
        Assert.Equal(SetBonuses.Chain, harness.CreateItem("ChainTunic").ItemDef.EquipmentProperties?.ArmorSet);
        Assert.Equal(SetBonuses.Leather, harness.CreateItem("LeatherBoot").ItemDef.EquipmentProperties?.ArmorSet);
    }

    [Fact]
    public void WitchDoctorPiecesHaveChainTierPhysAndMagicWard()
    {
        using var harness = BodyTestHarness.Human();
        var item = harness.CreateItem("WitchDoctorVambrace");
        Assert.Equal(WitchDoctorPhys, item.GetStatValue(Defs.Stats.PhysicalResistance));
        Assert.Equal(WitchDoctorMagic, item.GetStatValue(Defs.Stats.MagicResistance));
    }

    [Theory]
    [InlineData(1, 0f)]
    [InlineData(2, 0.12f)]
    [InlineData(4, 0.24f)]
    [InlineData(6, 0.40f)]
    public void WitchDoctorSetAddsMagicAtEachTier(int pieces, float magicBonus)
    {
        using var harness = BodyTestHarness.Human();
        EquipSet(harness, WitchDoctorPieces, pieces);

        Assert.Equal(pieces, SetBonuses.CountWorn(harness.Pawn, SetBonuses.WitchDoctor));
        Assert.Equal(1f + magicBonus, harness.Pawn.GetStatValue(Defs.Stats.Magic), 3);
    }

    [Fact]
    public void WitchDoctorFourPieceRegensOnTick()
    {
        using var harness = BodyTestHarness.Human();
        EquipSet(harness, WitchDoctorPieces, 4);
        var arm = harness.External(BodyPartType.Arm);
        arm.HitPoints -= 5;
        var before = arm.HitPoints;

        SetBonuses.Tick(harness.Pawn);

        Assert.True(arm.HitPoints > before);
        var expected = before + SetBonuses.WitchDoctorTiers[1].RegenPerTick * harness.Pawn.GetStatValue(Defs.Stats.Magic);
        Assert.Equal(expected, arm.HitPoints, 4);
    }

    [Fact]
    public void PlateTwoPieceAddsPhysicalResistanceOnUncoveredHits()
    {
        using var harness = BodyTestHarness.Human();
        harness.EquipArmor("PlateHelmet");
        harness.EquipArmor("PlateTunic");
        Assert.Equal(2, SetBonuses.CountWorn(harness.Pawn, SetBonuses.Plate));
        Assert.Equal(6f, harness.Pawn.GetStatValue(Defs.Stats.PhysicalResistance));

        var attacker = harness.CreatePawn("HumanA", "Attacker");
        harness.UseAlwaysHitRng();
        var record = Assert.Single(harness.Strike(attacker, harness.External(BodyPartType.Arm), BasicMidHit).Damages);
        Assert.Equal(CombatBalance.BlockedAmount(BasicMidHit, 6f), record.AmountBlocked, BlockPrecision);
    }

    [Fact]
    public void PlateFourPieceRaisesAttackSpeed()
    {
        using var harness = BodyTestHarness.Human();
        var baseSpeed = harness.Pawn.GetStatValue(Defs.Stats.AttackSpeed);
        EquipSet(harness, ["PlateHelmet", "PlateGorget", "PlateTunic", "PlateVambrace"], 4);

        var expected = (baseSpeed + SetBonuses.PlateTiers[1].AttackSpeed)
                       / (1f + harness.Pawn.Body.EquippedWeight * CombatBalance.WeightAttackSpeedFactor);
        Assert.Equal(expected, harness.Pawn.GetStatValue(Defs.Stats.AttackSpeed), 4);
    }

    [Fact]
    public void PlateSixPieceAddsStrength()
    {
        using var harness = BodyTestHarness.Human();
        EquipSet(harness, ["PlateHelmet", "PlateGorget", "PlateTunic", "PlateVambrace", "PlateGreave", "PlateBoot"], 6);
        Assert.Equal(1f + SetBonuses.PlateTiers[2].Strength, harness.Pawn.GetStatValue(Defs.Stats.Strength), 3);
    }

    [Fact]
    public void WitchDoctorMagicWardAddsMagicResistanceOnStormStaff()
    {
        using var harness = BodyTestHarness.Human();
        var attacker = harness.CreatePawn("HumanA", "Attacker");
        harness.EquipArmor("WitchDoctorVambrace");
        harness.UseAlwaysHitRng();

        var magicHit = Assert.Single(harness.Strike(attacker, harness.External(BodyPartType.Arm), BasicMidHit, "StormStaff").Damages);
        Assert.Equal(
            CombatBalance.BlockedAmount(BasicMidHit, WitchDoctorPhys + WitchDoctorMagic),
            magicHit.AmountBlocked,
            BlockPrecision);

        harness.UseAlwaysHitRng();
        var physicalHit = Assert.Single(harness.Strike(attacker, harness.External(BodyPartType.Arm), BasicMidHit, "IronSword").Damages);
        Assert.Equal(CombatBalance.BlockedAmount(BasicMidHit, WitchDoctorPhys), physicalHit.AmountBlocked, BlockPrecision);
        Assert.True(magicHit.AmountBlocked > physicalHit.AmountBlocked);
    }

    [Fact]
    public void DisplayHelpersDescribeTiersAndProgress()
    {
        using var harness = BodyTestHarness.Human();
        Assert.Equal("Witch Doctor", SetBonuses.DisplayName(SetBonuses.WitchDoctor));
        Assert.Equal("+0.24 Magic, regen", SetBonuses.DescribeTier(SetBonuses.WitchDoctorTiers[1]));
        Assert.Equal("+6 Physical Resistance", SetBonuses.DescribeTier(SetBonuses.PlateTiers[0]));
        Assert.Null(SetBonuses.DescribeActive(harness.Pawn, SetBonuses.WitchDoctor));
        Assert.Null(SetBonuses.NextTierHint(harness.Pawn, SetBonuses.WitchDoctor));

        EquipSet(harness, WitchDoctorPieces, 1);
        Assert.Equal("Witch Doctor 1/6", SetBonuses.DescribeActive(harness.Pawn, SetBonuses.WitchDoctor));
        Assert.Equal("1 more for 2-piece", SetBonuses.NextTierHint(harness.Pawn, SetBonuses.WitchDoctor));

        EquipSet(harness, WitchDoctorPieces[1..], 3);
        Assert.Equal("Witch Doctor 4/6: +0.24 Magic, regen", SetBonuses.DescribeActive(harness.Pawn, SetBonuses.WitchDoctor));
        Assert.Equal("2 more for 6-piece", SetBonuses.NextTierHint(harness.Pawn, SetBonuses.WitchDoctor));
    }

    [Fact]
    public void RageCloakScalesStrengthWithDestroyedParts()
    {
        using var harness = BodyTestHarness.Human();
        harness.EquipArmor("RageCloak");
        Assert.Equal(1f, harness.Pawn.GetStatValue(Defs.Stats.Strength), 3);

        var hand = harness.External(BodyPartType.Hand);
        hand.HitPoints = 0;
        Assert.True(hand.IsDestroyed);
        Assert.Equal(1, SetBonuses.CountBrokenParts(harness.Pawn));
        Assert.Equal(RageCloakHandler.MultiplierFor(1), harness.Pawn.GetStatValue(Defs.Stats.Strength), 3);

        var otherHand = harness.Pawn.Body.AllExternalParts.First(p => p.Type == BodyPartType.Hand && !p.IsDestroyed);
        otherHand.HitPoints = 0;
        Assert.Equal(2, SetBonuses.CountBrokenParts(harness.Pawn));
        Assert.Equal(RageCloakHandler.MultiplierFor(2), harness.Pawn.GetStatValue(Defs.Stats.Strength), 3);
    }

    private static void EquipSet(BodyTestHarness harness, string[] pieces, int count)
    {
        for (var i = 0; i < count; i++)
        {
            harness.EquipArmor(pieces[i]);
        }
    }
}
