using Wendlemire.Definitions;
using Wendlemire.Sim.Combat;
using Wendlemire.Sim.Entities;
using Wendlemire.Sim.Entities.Items;
using Wendlemire.Sim.Entities.Items.Trinkets;
using Wendlemire.Sim.Entities.Items.Weapons;
using Wendlemire.Sim.Entities.Pawns;
using Wendlemire.Sim.Entities.Pawns.Modifiers;
using Xunit;

namespace Wendlemire.Tests;

[Collection("Sim")]
public class ItemSynergyTests
{
    public ItemSynergyTests()
    {
        TestData.EnsureLoaded();
    }

    [Fact]
    public void BoneDecayNeedsExposedBoneUnlessStripDoTIsEating()
    {
        using var harness = BodyTestHarness.Human();
        var arm = harness.External(BodyPartType.Arm);
        var bone = arm.AllInternalParts.First(p => p.Substance == SubstanceType.Bone);
        Assert.NotNull(arm.Skin);
        Assert.False(arm.Skin!.IsDestroyed);

        var blocked = CreateMod(harness, Defs.BodyPartModifiers.BoneDecay);
        Assert.False(blocked.ApplyToPart(bone));

        arm.Skin.TryAddModifier(CreateMod(harness, Defs.BodyPartModifiers.Acid, 200));
        var opened = CreateMod(harness, Defs.BodyPartModifiers.BoneDecay);
        Assert.True(opened.ApplyToPart(bone));
        Assert.True(bone.HasModifier(Defs.BodyPartModifiers.BoneDecay));
    }

    [Fact]
    public void BoneDecayTicksHarderWhileAcidIsOnTheWound()
    {
        using var control = BodyTestHarness.Human();
        using var stripped = BodyTestHarness.Human();
        var controlBone = BoneOn(control);
        var strippedBone = BoneOn(stripped);
        stripped.External(BodyPartType.Arm).Skin!.TryAddModifier(CreateMod(stripped, Defs.BodyPartModifiers.Acid, 200));

        controlBone.TryAddModifier(CreateMod(control, Defs.BodyPartModifiers.BoneDecay));
        strippedBone.TryAddModifier(CreateMod(stripped, Defs.BodyPartModifiers.BoneDecay));

        var controlBefore = controlBone.HitPoints;
        var strippedBefore = strippedBone.HitPoints;
        TickModifier(controlBone, Defs.BodyPartModifiers.BoneDecay);
        TickModifier(strippedBone, Defs.BodyPartModifiers.BoneDecay);

        var controlLoss = controlBefore - controlBone.HitPoints;
        var strippedLoss = strippedBefore - strippedBone.HitPoints;
        Assert.True(controlLoss > 0);
        Assert.Equal(controlLoss * ItemSynergies.StripDecayDamage, strippedLoss, 3);
    }

    [Fact]
    public void TallowMakesBurningHitHarder()
    {
        using var plain = BodyTestHarness.Human();
        using var greased = BodyTestHarness.Human();
        var plainSkin = ApplyBurn(plain);
        var greasedSkin = ApplyBurn(greased);
        greased.Pawn.Body.Effects.TryApplyEffect(new BodyEffect
        {
            Def = Defs.BodyEffects.Tallowed,
            TicksLeft = 200
        });

        var plainBefore = plainSkin.HitPoints;
        var greasedBefore = greasedSkin.HitPoints;
        TickModifier(plainSkin, Defs.BodyPartModifiers.Burning);
        TickModifier(greasedSkin, Defs.BodyPartModifiers.Burning);

        var plainLoss = plainBefore - plainSkin.HitPoints;
        var greasedLoss = greasedBefore - greasedSkin.HitPoints;
        Assert.True(plainLoss > 0);
        Assert.Equal(plainLoss * ItemSynergies.TallowBurnDamage, greasedLoss, 3);
    }

    [Fact]
    public void PoisonLetsFesteringPenetrateSooner()
    {
        using var harness = BodyTestHarness.Human();
        var arm = harness.External(BodyPartType.Arm);
        var skin = arm.Skin!;
        CreateMod(harness, Defs.BodyPartModifiers.Festering, 200).ApplyToPart(arm);
        skin.HitPoints = skin.MaxHitPoints * 0.82f;

        TickModifier(skin, Defs.BodyPartModifiers.Festering);
        Assert.False(InternalsHave(arm, Defs.BodyPartModifiers.Festering));

        var artery = arm.AllInternalParts.First(p => p.Type == BodyPartType.Artery);
        artery.TryAddModifier(CreateMod(harness, Defs.BodyPartModifiers.Poison, 200));
        TickModifier(skin, Defs.BodyPartModifiers.Festering);
        Assert.True(InternalsHave(arm, Defs.BodyPartModifiers.Festering));
    }

    [Fact]
    public void LeafHealsHarderOnSoothedFlesh()
    {
        using var harness = BodyTestHarness.Human();
        var helm = harness.EquipArmor("LeatherHelmet");
        var leaf = harness.SocketEnchant(helm, "ElvishLeaf");
        var head = harness.External(BodyPartType.Head);
        head.HitPoints = head.MaxHitPoints * 0.4f;

        var before = head.HitPoints;
        leaf.EnchantmentHandler!.TickForPawn(harness.Pawn, head);
        var plainHeal = head.HitPoints - before;

        head.TryAddModifier(CreateMod(harness, Defs.BodyPartModifiers.HealthRegeneration, 80));
        var mid = head.HitPoints;
        leaf.EnchantmentHandler.TickForPawn(harness.Pawn, head);
        var soothedHeal = head.HitPoints - mid;

        Assert.True(plainHeal > 0);
        Assert.Equal(plainHeal * ItemSynergies.LeafSoothingHeal, soothedHeal, 5);
    }

    [Fact]
    public void CollarLeafAndBathRaiseMagic()
    {
        using var harness = BodyTestHarness.Human();
        var baseline = harness.Pawn.GetStatValue(Defs.Stats.Magic);
        var collar = harness.EquipArmor("BlessedIronCollar");
        harness.SocketEnchant(collar, "ElvishLeaf", 0);
        harness.SocketEnchant(collar, "BloodBath", 1);

        Assert.Equal(baseline + ItemSynergies.CollarPairMagic, harness.Pawn.GetStatValue(Defs.Stats.Magic), 3);
    }

    [Fact]
    public void BiteChanceScalesWithRhinoOnTheSamePiece()
    {
        Assert.Equal(1.15f, ItemSynergies.BiteChanceFromRhino(1), 3);
        Assert.Equal(1.23f, ItemSynergies.BiteChanceFromRhino(2), 3);
        Assert.Equal(1.39f, ItemSynergies.BiteChanceFromRhino(4), 3);
    }

    [Fact]
    public void BloodSucklerDrinksDeeperWithBloodBath()
    {
        using var harness = BodyTestHarness.Human();
        var victim = harness.CreatePawn("HumanA", "Victim");
        var glove = harness.EquipArmor("LeatherGlove");
        harness.SocketEnchant(glove, "BloodBath");

        var blade = harness.CreateItem("BloodSuckler");
        var handler = (BloodSucklerHandler)blade.WeaponHandler!;
        DrainBlood(harness.Pawn, 0.5f);
        DrainBlood(victim, 0.5f);

        var attackerBlood = harness.Pawn.Body.BloodAmount;
        var victimBlood = victim.Body.BloodAmount;
        var record = HitRecord(victim);
        handler.OnHit(harness.Pawn, victim, Request(harness.Pawn, blade, victim.Body.AllExternalParts.First(p => p.Type == BodyPartType.Arm)), record);

        var stolen = victimBlood - victim.Body.BloodAmount;
        var gained = harness.Pawn.Body.BloodAmount - attackerBlood;
        var expected = harness.Pawn.Body.MaxBlood * 0.03f * ItemSynergies.SucklerBathBlood;
        Assert.Equal(expected, stolen, 2);
        Assert.Equal(expected, gained, 2);
    }

    [Fact]
    public void BloodyBellTollsOnTheNextPlayerHit()
    {
        using var harness = BodyTestHarness.Human();
        harness.Pawn.PawnType = PawnType.Player;
        var victim = harness.CreatePawn("HumanA", "Victim");
        var bell = harness.CreateItem("BloodyBell");
        harness.Pawn.Inventory.TryAdd(bell);
        var handler = (BloodyBellHandler)bell.TrinketHandler!;
        Assert.True(handler.Activate());

        DrainBlood(victim, 0.5f);
        DrainBlood(harness.Pawn, 0.5f);
        var victimBefore = victim.Body.BloodAmount;
        var attackerBefore = harness.Pawn.Body.BloodAmount;
        var arm = victim.Body.AllExternalParts.First(p => p.Type == BodyPartType.Arm);
        var sword = harness.CreateWeapon();
        var request = Request(harness.Pawn, sword, arm);
        var response = new DamageResponse();
        response.Damages.Add(new DamageRecord(sword.Label, "Swing", DamageType.Sharp, arm, 40, 0) { ActualAmount = 40 });

        var toll = handler.PostAttackHandler(victim, request, response);

        Assert.NotNull(toll);
        Assert.False(handler.IsActive);
        Assert.Equal(1, handler.TotalRings);
        Assert.True(handler.Cooldown > 0);
        Assert.True(victim.Body.BloodAmount < victimBefore);
        Assert.True(harness.Pawn.Body.BloodAmount > attackerBefore);
    }

    [Fact]
    public void PlagueMaskMakesTwigAfflictionsLastLonger()
    {
        using var harness = BodyTestHarness.Human();
        harness.EquipArmor("PlagueMask");
        var victim = harness.CreatePawn("HumanA", "Victim");
        var twig = harness.CreateItem("StrangeWitheredTwig");
        var handler = (StrangeWitheredTwigHandler)twig.WeaponHandler!;
        var arm = victim.Body.AllExternalParts.First(p => p.Type == BodyPartType.Arm);
        handler.OnHit(harness.Pawn, victim, Request(harness.Pawn, twig, arm), HitRecord(victim));

        var durations = arm.Modifiers
            .Concat(arm.AllInternalParts.SelectMany(p => p.Modifiers))
            .Select(m => m.DurationInTicks)
            .ToList();
        Assert.True(durations.Count >= 3);
        Assert.True(durations.Min() >= (int)(60 * ItemSynergies.TwigMaskDuration));
    }

    [Fact]
    public void ThornCloakStingsWhenBiteIsSocketed()
    {
        using var harness = BodyTestHarness.Human();
        var attacker = harness.CreatePawn("HumanA", "Attacker");
        harness.EquipArmor("ThornCloak");
        var glove = harness.EquipArmor("LeatherGlove");
        harness.SocketEnchant(glove, "SpidersBite");
        harness.UseAlwaysHitRng();

        var arm = harness.External(BodyPartType.Arm);
        harness.Strike(attacker, arm, 40);

        Assert.Contains(
            attacker.Body.AllParts,
            p => p.HasModifier(Defs.BodyPartModifiers.Acid));
    }

    [Fact]
    public void BurningAndAcidReactOnTheSameWound()
    {
        using var burnOnly = BodyTestHarness.Human();
        using var both = BodyTestHarness.Human();
        var burnSkin = ApplyBurn(burnOnly);
        var mixedSkin = ApplyBurn(both);
        CreateMod(both, Defs.BodyPartModifiers.Acid, 200).ApplyToPart(both.External(BodyPartType.Arm));

        var burnBefore = burnSkin.HitPoints;
        var mixedBefore = mixedSkin.HitPoints;
        TickModifier(burnSkin, Defs.BodyPartModifiers.Burning);
        TickModifier(mixedSkin, Defs.BodyPartModifiers.Burning);

        var burnLoss = burnBefore - burnSkin.HitPoints;
        var mixedLoss = mixedBefore - mixedSkin.HitPoints;
        Assert.True(burnLoss > 0);
        Assert.Equal(burnLoss * ItemSynergies.CausticFireBurn, mixedLoss, 3);
    }

    private static BodyPartModifier CreateMod(BodyTestHarness harness, BodyPartModifierDef def, int duration = 200) =>
        harness.Context.Factory.CreateModifier(def, duration, 1);

    private static BodyPart BoneOn(BodyTestHarness harness) =>
        harness.External(BodyPartType.Arm).AllInternalParts.First(p => p.Substance == SubstanceType.Bone);

    private static BodyPart ApplyBurn(BodyTestHarness harness)
    {
        var arm = harness.External(BodyPartType.Arm);
        CreateMod(harness, Defs.BodyPartModifiers.Burning, 200).ApplyToPart(arm);
        return arm.Skin!;
    }

    private static void TickModifier(BodyPart part, BodyPartModifierDef def) =>
        part.Modifiers.First(m => m.Def == def).Tick();

    private static bool InternalsHave(BodyPart host, BodyPartModifierDef def) =>
        host.InternalParts.Any(p => p.Type != BodyPartType.Skin && p.HasModifier(def));

    private static void DrainBlood(Pawn pawn, float remainingFraction) =>
        pawn.Body.BloodAmount = pawn.Body.MaxBlood * remainingFraction;

    private static DamageRequest Request(Pawn attacker, Item weapon, BodyPart target)
    {
        var maneuvers = weapon.ItemDef.WeaponProperties!.WeaponManeuvers;
        Assert.True(maneuvers.Count > 0, $"{weapon.ItemDef.Moniker} has no maneuvers");
        return new DamageRequest(attacker, weapon, maneuvers[0]) { TargetedPart = target };
    }

    private static DamageRecord HitRecord(Pawn victim)
    {
        var part = victim.Body.AllExternalParts.First(p => p.Type == BodyPartType.Arm);
        return new DamageRecord("Blood Suckler", "Stab", DamageType.Sharp, part, 10, 0)
        {
            ActualAmount = 10
        };
    }

}
