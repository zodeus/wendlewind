using Wendlemire.Definitions;
using Wendlemire.Sim.Combat;
using Wendlemire.Sim.Entities;
using Wendlemire.Sim.Entities.Pawns;
using Xunit;

namespace Wendlemire.Tests;

/// <summary>
/// Armor is a flat <see cref="Defs.Stats.PhysicalResistance"/> subtract on the hit part.
/// Basic primitive/iron weapons land ~20 (glance) to ~58 (full swing); mid hits sit near 40.
/// </summary>
[Collection("Sim")]
public class ArmorCombatTests
{
    private const double BasicMidHit = 40;
    private const double BasicGlance = 20;
    private const double BasicFullSwing = 58;

    public ArmorCombatTests()
    {
        TestData.EnsureLoaded();
    }

    [Theory]
    [InlineData("ClothVambrace", 14f)]
    [InlineData("LeatherVambrace", 22f)]
    [InlineData("ChainVambrace", 32f)]
    [InlineData("WitchDoctorVambrace", 36f)]
    [InlineData("ClothTunic", 14f)]
    [InlineData("LeatherTunic", 24f)]
    [InlineData("ChainTunic", 32f)]
    [InlineData("BucketHelmet", 18f)]
    [InlineData("FishBowlHelmet", 12f)]
    [InlineData("PlagueMask", 22f)]
    public void ArmorPiecesHaveExpectedPhysicalResistance(string moniker, float expected)
    {
        using var harness = BodyTestHarness.Human();
        var item = harness.CreateItem(moniker);
        Assert.Equal(expected, item.GetStatValue(Defs.Stats.PhysicalResistance));
    }

    [Fact]
    public void UnarmoredArmTakesFullBasicMidHit()
    {
        using var harness = BodyTestHarness.Human();
        var attacker = harness.CreatePawn("HumanA", "Attacker");
        harness.UseAlwaysHitRng();
        var arm = harness.External(BodyPartType.Arm);
        var hpBefore = arm.HitPoints;

        var response = harness.Strike(attacker, arm, BasicMidHit);

        var record = Assert.Single(response.Damages);
        Assert.Equal(0, record.AmountBlocked);
        Assert.True(record.ActualAmount > 0);
        Assert.True(arm.HitPoints < hpBefore);
        Assert.Null(record.BlockingItemMoniker);
    }

    [Theory]
    [InlineData("ClothVambrace", 14)]
    [InlineData("LeatherVambrace", 22)]
    [InlineData("ChainVambrace", 32)]
    [InlineData("WitchDoctorVambrace", 36)]
    public void ArmoredArmBlocksPhysicalResistanceFromBasicMidHit(string armor, double expectedBlock)
    {
        using var harness = BodyTestHarness.Human();
        var attacker = harness.CreatePawn("HumanA", "Attacker");
        harness.UseAlwaysHitRng();
        var arm = harness.External(BodyPartType.Arm);
        harness.EquipArmor(armor, harness.Pawn);

        var response = harness.Strike(attacker, arm, BasicMidHit);

        var record = Assert.Single(response.Damages);
        Assert.Equal(expectedBlock, record.AmountBlocked);
        Assert.Equal(armor, record.BlockingItemMoniker);
        Assert.Equal(BasicMidHit - expectedBlock, record.TotalDamage - record.AmountBlocked);
    }

    [Fact]
    public void ChainArmTakesFarLessBodyDamageThanUnarmoredAgainstBasicMidHit()
    {
        using var bare = BodyTestHarness.Human();
        using var armored = BodyTestHarness.Human();
        var bareAttacker = bare.CreatePawn("HumanA", "Attacker");
        var armoredAttacker = armored.CreatePawn("HumanA", "Attacker");
        bare.UseAlwaysHitRng();
        armored.UseAlwaysHitRng();
        armored.EquipArmor("ChainVambrace");

        var bareArm = bare.External(BodyPartType.Arm);
        var armoredArm = armored.External(BodyPartType.Arm);
        var bareHp = bareArm.HitPoints;
        var armoredHp = armoredArm.HitPoints;

        var bareHit = bare.Strike(bareAttacker, bareArm, BasicMidHit);
        var armoredHit = armored.Strike(armoredAttacker, armoredArm, BasicMidHit);

        var bareLoss = bareHp - bareArm.HitPoints;
        var armoredLoss = armoredHp - armoredArm.HitPoints;
        var bareBody = Assert.Single(bareHit.Damages).ActualAmount;
        var armoredBody = Assert.Single(armoredHit.Damages).ActualAmount;

        Assert.True(bareLoss > 0);
        Assert.True(armoredLoss < bareLoss * 0.35, $"Chain arm lost {armoredLoss:0.##} vs unarmored {bareLoss:0.##}");
        Assert.True(armoredBody < bareBody * 0.35, $"Chain body damage {armoredBody:0.##} vs unarmored {bareBody:0.##}");
        Assert.Equal(32, Assert.Single(armoredHit.Damages).AmountBlocked);
    }

    [Fact]
    public void LeatherAndAboveShrugOffABasicGlance()
    {
        using var harness = BodyTestHarness.Human();
        var attacker = harness.CreatePawn("HumanA", "Attacker");
        harness.UseAlwaysHitRng();
        var arm = harness.External(BodyPartType.Arm);
        var hpBefore = arm.HitPoints;
        harness.EquipArmor("LeatherVambrace");

        var response = harness.Strike(attacker, arm, BasicGlance);

        var record = Assert.Single(response.Damages);
        Assert.Equal(BasicGlance, record.AmountBlocked);
        Assert.Equal(0, record.ActualAmount);
        Assert.Equal(hpBefore, arm.HitPoints);
    }

    [Fact]
    public void ChainStillLetsAFullBasicSwingThrough()
    {
        using var harness = BodyTestHarness.Human();
        var attacker = harness.CreatePawn("HumanA", "Attacker");
        harness.UseAlwaysHitRng();
        var arm = harness.External(BodyPartType.Arm);
        harness.EquipArmor("ChainVambrace");

        var response = harness.Strike(attacker, arm, BasicFullSwing);

        var record = Assert.Single(response.Damages);
        Assert.Equal(32, record.AmountBlocked);
        Assert.True(record.ActualAmount > 0);
        Assert.True(record.ActualAmount < BasicFullSwing * 0.5);
    }

    [Fact]
    public void ArmorProtectsOnlyTheCoveredPart()
    {
        using var harness = BodyTestHarness.Human();
        var attacker = harness.CreatePawn("HumanA", "Attacker");
        harness.EquipArmor("ChainTunic");
        var torso = harness.External(BodyPartType.Torso);
        var head = harness.External(BodyPartType.Head);

        harness.UseAlwaysHitRng();
        var torsoHit = harness.Strike(attacker, torso, BasicMidHit);
        harness.UseAlwaysHitRng();
        var headHit = harness.Strike(attacker, head, BasicMidHit);

        Assert.Equal(32, Assert.Single(torsoHit.Damages).AmountBlocked);
        Assert.Equal(0, Assert.Single(headHit.Damages).AmountBlocked);
        Assert.True(Assert.Single(headHit.Damages).ActualAmount > Assert.Single(torsoHit.Damages).ActualAmount);
    }

    [Fact]
    public void HeavierTiersBlockMoreThanLighterTiers()
    {
        var blocked = new Dictionary<string, double>();
        foreach (var armor in new[] { "ClothVambrace", "LeatherVambrace", "ChainVambrace", "WitchDoctorVambrace" })
        {
            using var harness = BodyTestHarness.Human();
            var attacker = harness.CreatePawn("HumanA", "Attacker");
            harness.UseAlwaysHitRng();
            harness.EquipArmor(armor);
            var hit = harness.Strike(attacker, harness.External(BodyPartType.Arm), BasicMidHit);
            blocked[armor] = Assert.Single(hit.Damages).AmountBlocked;
        }

        Assert.True(blocked["ClothVambrace"] < blocked["LeatherVambrace"]);
        Assert.True(blocked["LeatherVambrace"] < blocked["ChainVambrace"]);
        Assert.True(blocked["ChainVambrace"] < blocked["WitchDoctorVambrace"]);
    }

    [Theory]
    [InlineData("BoneKnife")]
    [InlineData("WoodClub")]
    [InlineData("IronSword")]
    public void ChainCutsBasicWeaponBodyDamageToAMinorityOfUnarmored(string weapon)
    {
        var rawHits = SampleBasicWeaponDamage(weapon, seed: 11, count: 8);
        Assert.All(rawHits, raw => Assert.True(raw > 0));

        using var bare = BodyTestHarness.Human();
        using var armored = BodyTestHarness.Human();
        var bareAttacker = bare.CreatePawn("HumanA", "Attacker");
        var armoredAttacker = armored.CreatePawn("HumanA", "Attacker");
        var bareArm = bare.External(BodyPartType.Arm);
        var armoredArm = armored.External(BodyPartType.Arm);
        armored.EquipArmor("ChainVambrace");

        double bareTotal = 0;
        double armoredTotal = 0;
        foreach (var raw in rawHits)
        {
            bare.UseAlwaysHitRng();
            armored.UseAlwaysHitRng();
            bareTotal += Assert.Single(bare.Strike(bareAttacker, bareArm, raw, weapon).Damages).ActualAmount;
            armoredTotal += Assert.Single(armored.Strike(armoredAttacker, armoredArm, raw, weapon).Damages).ActualAmount;
            RestorePartTree(bareArm);
            RestorePartTree(armoredArm);
        }

        Assert.True(bareTotal > 0, $"{weapon} dealt no unarmored body damage");
        Assert.True(
            armoredTotal < bareTotal * 0.4,
            $"{weapon}: chain arm took {armoredTotal:0.##} vs unarmored {bareTotal:0.##}");
    }

    private static void RestorePartTree(BodyPart part)
    {
        part.HitPoints = part.MaxHitPoints;
        foreach (var inner in part.AllInternalParts)
        {
            inner.HitPoints = inner.MaxHitPoints;
        }
    }

    private static List<double> SampleBasicWeaponDamage(string weaponMoniker, int seed, int count)
    {
        using var harness = BodyTestHarness.Human(seed);
        var attacker = harness.CreatePawn("HumanA", "Attacker");
        var weapon = harness.CreateWeapon(weaponMoniker);
        var raw = new List<double>(count);
        for (var i = 0; i < count; i++)
        {
            raw.Add(DamageRequest.Create(attacker, weapon).TotalRawDamage);
        }

        return raw;
    }
}
