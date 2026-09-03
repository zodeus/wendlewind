using Microsoft.Extensions.DependencyInjection;
using Wendlemire.Definitions;
using Wendlemire.NetCode;
using Wendlemire.Sim;
using Wendlemire.Sim.Combat;
using Wendlemire.Sim.Entities;
using Wendlemire.Sim.Entities.Items;
using Wendlemire.Sim.Entities.Items.Medicinals;
using Wendlemire.Sim.Entities.Pawns;
using Wendlemire.Sim.Entities.Pawns.Modifiers;
using Xunit;

namespace Wendlemire.Tests;

[Collection("Sim")]
public class MedicalTriggerTests
{
    public MedicalTriggerTests()
    {
        TestData.EnsureLoaded();
    }

    [Theory]
    [InlineData("MedKit")]
    [InlineData("Suture")]
    [InlineData("MendersMist")]
    [InlineData("BalmyOintment")]
    [InlineData("AntiNecroticSerum")]
    [InlineData("MendersMix")]
    [InlineData("BoneCleanse")]
    [InlineData("StrengthenBones")]
    [InlineData("Cyberveins")]
    [InlineData("Cauterize")]
    [InlineData("Bandage")]
    [InlineData("BoneGlue")]
    [InlineData("Antidote")]
    [InlineData("ClotPack")]
    public void AuthoredDefaultFiresInCombat(string moniker)
    {
        var def = RequireDef(moniker);
        var (_, log) = CombatReplay.RunWithLog(configure: context =>
        {
            BuildSnapshotFactory.Apply(context.PlayerPawn, BuildTemplates.TankRegen());
            var pawn = context.PlayerPawn;
            pawn.MedicalChest.Clear();
            Assert.True(pawn.MedicalChest.TryInstall(def, 1));
            PrepareFor(moniker, pawn, context);
        });

        var used = log.FirstOrDefault(e => e.Kind == CombatEventKind.MedicalUsed && e.ItemMoniker == moniker);
        Assert.NotNull(used);
        Assert.False(string.IsNullOrWhiteSpace(used.BodyPartLabel));
    }

    [Fact]
    public void UrgencyRisesForAfterSeconds()
    {
        using var root = SimServices.BuildRoot();
        using var scope = root.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<GameContext>();
        context.Initialize(CombatReplay.DefaultRunSeed);
        BuildSnapshotFactory.Apply(context.PlayerPawn, BuildTemplates.TankRegen());

        var trigger = new MedicalTrigger
        {
            Type = MedicalTriggerType.AfterSeconds,
            AfterSeconds = 4f
        };
        var pawn = context.PlayerPawn;
        var def = RequireDef("MedKit");
        var target = 4f * GameContext.TicksPerSecond;
        Assert.Equal(0f, trigger.GetUrgency(pawn, pawn, 0, def), 3);
        Assert.Equal(0.5f, trigger.GetUrgency(pawn, pawn, (int)(target * 0.5f), def), 3);
        Assert.Equal(1f, trigger.GetUrgency(pawn, pawn, (int)target, def), 3);
        Assert.Equal(1f, trigger.GetUrgency(pawn, pawn, (int)target + 30, def), 3);
    }

    [Fact]
    public void UrgencyRisesAsBloodApproachesThreshold()
    {
        using var root = SimServices.BuildRoot();
        using var scope = root.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<GameContext>();
        context.Initialize(CombatReplay.DefaultRunSeed);
        BuildSnapshotFactory.Apply(context.PlayerPawn, BuildTemplates.TankRegen());

        var pawn = context.PlayerPawn;
        var trigger = new MedicalTrigger
        {
            Type = MedicalTriggerType.SelfBloodBelow,
            Threshold = 0.5f
        };
        var def = RequireDef("ClotPack");

        pawn.Body.BloodAmount = pawn.Body.MaxBlood;
        Assert.Equal(0f, trigger.GetUrgency(pawn, pawn, 0, def), 3);

        pawn.Body.BloodAmount = pawn.Body.MaxBlood * 0.75f;
        Assert.Equal(0.5f, trigger.GetUrgency(pawn, pawn, 0, def), 3);

        pawn.Body.BloodAmount = pawn.Body.MaxBlood * 0.5f;
        Assert.Equal(1f, trigger.GetUrgency(pawn, pawn, 0, def), 3);

        pawn.Body.BloodAmount = pawn.Body.MaxBlood * 0.2f;
        Assert.Equal(1f, trigger.GetUrgency(pawn, pawn, 0, def), 3);
    }

    [Fact]
    public void UrgencyRisesAsWatchedPartApproachesHealthThreshold()
    {
        using var root = SimServices.BuildRoot();
        using var scope = root.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<GameContext>();
        context.Initialize(CombatReplay.DefaultRunSeed);
        BuildSnapshotFactory.Apply(context.PlayerPawn, BuildTemplates.TankRegen());

        var pawn = context.PlayerPawn;
        var part = FirstExternal(pawn);
        var trigger = new MedicalTrigger
        {
            Type = MedicalTriggerType.PartBelowHealth,
            TargetSelector = MedicalTargetSelector.SpecificPart,
            TargetPartKey = MedicalTrigger.GroupKey(part),
            HealthThreshold = 0.5f
        };
        var def = RequireDef("MedKit");

        part.HitPoints = part.MaxHitPoints;
        Assert.Equal(0f, trigger.GetUrgency(pawn, pawn, 0, def), 3);

        part.HitPoints = part.MaxHitPoints * 0.75;
        Assert.Equal(0.5f, trigger.GetUrgency(pawn, pawn, 0, def), 3);

        part.HitPoints = part.MaxHitPoints * 0.5;
        Assert.Equal(1f, trigger.GetUrgency(pawn, pawn, 0, def), 3);
    }

    [Fact]
    public void UrgencyIsBinaryForStatusTriggers()
    {
        using var root = SimServices.BuildRoot();
        using var scope = root.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<GameContext>();
        context.Initialize(CombatReplay.DefaultRunSeed);
        BuildSnapshotFactory.Apply(context.PlayerPawn, BuildTemplates.TankRegen());

        var pawn = context.PlayerPawn;
        var def = RequireDef("AntiNecroticSerum");
        var trigger = new MedicalTrigger { Type = MedicalTriggerType.HasNecrosis };
        Assert.Equal(0f, trigger.GetUrgency(pawn, pawn, 0, def), 3);

        Afflict(context, FirstOrgan(pawn), Defs.BodyPartModifiers.Necrosis);
        Assert.Equal(1f, trigger.GetUrgency(pawn, pawn, 0, def), 3);
    }

    [Fact]
    public void ImmediatelyUrgencyIsReadyUntilLockedByChest()
    {
        using var root = SimServices.BuildRoot();
        using var scope = root.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<GameContext>();
        context.Initialize(CombatReplay.DefaultRunSeed);
        BuildSnapshotFactory.Apply(context.PlayerPawn, BuildTemplates.TankRegen());

        var pawn = context.PlayerPawn;
        var def = RequireDef("Cyberveins");
        var trigger = new MedicalTrigger { Type = MedicalTriggerType.Immediately };
        Assert.Equal(1f, trigger.GetUrgency(pawn, pawn, 0, def), 3);

        Assert.True(pawn.MedicalChest.TryInstall(def, 0, trigger));
        var slot = pawn.MedicalChest.Slots[0];
        Assert.False(MedicalChest.IsLockedForRestOfCombat(slot));
        MedicalChest.LockForRestOfCombat(slot);
        Assert.True(MedicalChest.IsLockedForRestOfCombat(slot));
        Assert.Equal(1f, slot.Trigger.GetUrgency(pawn, pawn, 0, def), 3);
    }

    [Fact]
    public void SanitizeRejectsDisallowedTrigger()
    {
        using var root = SimServices.BuildRoot();
        using var scope = root.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<GameContext>();
        context.Initialize(CombatReplay.DefaultRunSeed);
        BuildSnapshotFactory.Apply(context.PlayerPawn, BuildTemplates.TankRegen());

        var def = RequireDef("MedKit");
        Assert.True(context.PlayerPawn.MedicalChest.TryInstall(def, 1, new MedicalTrigger
        {
            Type = MedicalTriggerType.PartSevered,
            TargetSelector = MedicalTargetSelector.SeveredOrUnsealedSocket
        }));

        var slot = context.PlayerPawn.MedicalChest.Slots[0];
        Assert.Equal(MedicalTriggerType.PartBelowHealth, slot.Trigger.Type);
        Assert.Equal(MedicalTargetSelector.Auto, slot.Trigger.TargetSelector);
    }

    [Fact]
    public void MedKitSelectablePartsIncludeOrgans()
    {
        using var root = SimServices.BuildRoot();
        using var scope = root.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<GameContext>();
        context.Initialize(CombatReplay.DefaultRunSeed);
        BuildSnapshotFactory.Apply(context.PlayerPawn, BuildTemplates.TankRegen());

        var def = RequireDef("MedKit");
        var parts = MedicalTrigger.ListSelectableParts(context.PlayerPawn, def.MedicinalProperties);
        Assert.Contains(parts, p => p.IsOrgan);
        Assert.Contains(parts, p => p.IsExternal);
        Assert.DoesNotContain(parts, p => p.Type is BodyPartType.Finger or BodyPartType.Thumb);
        Assert.True(MedicalTrigger.UsesRegionGroups(def.MedicinalProperties));
    }

    [Fact]
    public void SutureSelectablePartsAreArteries()
    {
        using var root = SimServices.BuildRoot();
        using var scope = root.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<GameContext>();
        context.Initialize(CombatReplay.DefaultRunSeed);
        BuildSnapshotFactory.Apply(context.PlayerPawn, BuildTemplates.TankRegen());

        var def = RequireDef("Suture");
        var parts = MedicalTrigger.ListSelectableParts(context.PlayerPawn, def.MedicinalProperties);
        Assert.NotEmpty(parts);
        Assert.All(parts, p => Assert.Equal(BodyPartType.Artery, p.Type));
    }

    [Fact]
    public void RegionGroupLabelUsesLimbAndTorsoRoots()
    {
        using var root = SimServices.BuildRoot();
        using var scope = root.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<GameContext>();
        context.Initialize(CombatReplay.DefaultRunSeed);
        BuildSnapshotFactory.Apply(context.PlayerPawn, BuildTemplates.TankRegen());

        var pawn = context.PlayerPawn;
        var brain = pawn.Body.AllParts.First(p => p.Type == BodyPartType.Brain);
        var heart = pawn.Body.AllParts.First(p => p.Type == BodyPartType.Heart);
        var thumb = pawn.Body.AllParts.First(p => p.Type == BodyPartType.Thumb);
        var foot = pawn.Body.AllParts.First(p => p.Type == BodyPartType.Foot);

        Assert.Equal("Head", MedicalTrigger.RegionGroupLabel(brain));
        Assert.Equal("Torso", MedicalTrigger.RegionGroupLabel(heart));
        Assert.Equal("Arms", MedicalTrigger.RegionGroupLabel(thumb));
        Assert.Equal("Legs", MedicalTrigger.RegionGroupLabel(foot));
    }

    [Fact]
    public void MendersMistSelectablePartsAreExternal()
    {
        using var root = SimServices.BuildRoot();
        using var scope = root.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<GameContext>();
        context.Initialize(CombatReplay.DefaultRunSeed);
        BuildSnapshotFactory.Apply(context.PlayerPawn, BuildTemplates.TankRegen());

        var def = RequireDef("MendersMist");
        var parts = MedicalTrigger.ListSelectableParts(context.PlayerPawn, def.MedicinalProperties);
        Assert.NotEmpty(parts);
        Assert.All(parts, p => Assert.True(p.IsExternal));
        Assert.DoesNotContain(parts, p => p.IsOrgan);
        Assert.DoesNotContain(parts, p => p.Type is BodyPartType.Finger or BodyPartType.Thumb);
        Assert.False(MedicalTrigger.UsesRegionGroups(def.MedicinalProperties));
    }

    [Fact]
    public void PairedOrgansCollapseInPicker()
    {
        using var root = SimServices.BuildRoot();
        using var scope = root.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<GameContext>();
        context.Initialize(CombatReplay.DefaultRunSeed);
        BuildSnapshotFactory.Apply(context.PlayerPawn, BuildTemplates.TankRegen());

        var def = RequireDef("MedKit");
        var picks = MedicalTrigger.ListSelectableParts(context.PlayerPawn, def.MedicinalProperties);
        Assert.Equal(1, picks.Count(p => p.Type == BodyPartType.Lung));
        Assert.Equal(1, picks.Count(p => p.Type == BodyPartType.Kidney));
        Assert.Equal("Lungs", MedicalTrigger.GroupLabel(picks.First(p => p.Type == BodyPartType.Lung)));
        Assert.Equal("Kidneys", MedicalTrigger.GroupLabel(picks.First(p => p.Type == BodyPartType.Kidney)));
    }

    [Fact]
    public void PairedOrganGroupFiresOnEitherMember()
    {
        using var root = SimServices.BuildRoot();
        using var scope = root.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<GameContext>();
        context.Initialize(CombatReplay.DefaultRunSeed);
        BuildSnapshotFactory.Apply(context.PlayerPawn, BuildTemplates.TankRegen());

        var pawn = context.PlayerPawn;
        var lungs = pawn.Body.AllParts.Where(p => p.Type == BodyPartType.Lung).ToList();
        Assert.True(lungs.Count >= 2);
        lungs[0].HitPoints = lungs[0].MaxHitPoints;
        lungs[1].HitPoints = lungs[1].MaxHitPoints * 0.2;

        var trigger = new MedicalTrigger
        {
            Type = MedicalTriggerType.PartBelowHealth,
            TargetSelector = MedicalTargetSelector.SpecificPart,
            TargetPartKey = nameof(BodyPartType.Lung),
            HealthThreshold = 0.5f
        };
        Assert.True(trigger.ShouldFire(pawn, pawn, 0, RequireDef("MedKit")));

        lungs[1].HitPoints = lungs[1].MaxHitPoints;
        Assert.False(trigger.ShouldFire(pawn, pawn, 0, RequireDef("MedKit")));
    }

    [Fact]
    public void PairedOrganApplyTriesWorstFirst()
    {
        using var root = SimServices.BuildRoot();
        using var scope = root.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<GameContext>();
        context.Initialize(CombatReplay.DefaultRunSeed);
        BuildSnapshotFactory.Apply(context.PlayerPawn, BuildTemplates.TankRegen());

        var pawn = context.PlayerPawn;
        var lungs = pawn.Body.AllParts.Where(p => p.Type == BodyPartType.Lung).ToList();
        Assert.True(lungs.Count >= 2);
        lungs[0].HitPoints = lungs[0].MaxHitPoints * 0.8;
        lungs[1].HitPoints = lungs[1].MaxHitPoints * 0.2;

        var trigger = new MedicalTrigger
        {
            Type = MedicalTriggerType.PartBelowHealth,
            TargetSelector = MedicalTargetSelector.SpecificPart,
            TargetPartKey = lungs[0].InternalLabel,
            HealthThreshold = 0.5f
        };
        var def = RequireDef("AntiNecroticSerum");
        var first = trigger.EnumerateApplyTargets(pawn, def).First();
        Assert.Equal(lungs[1], first);
    }

    [Fact]
    public void CyberveinsTriplesArteryMaxHitPoints()
    {
        using var root = SimServices.BuildRoot();
        using var scope = root.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<GameContext>();
        context.Initialize(CombatReplay.DefaultRunSeed);
        BuildSnapshotFactory.Apply(context.PlayerPawn, BuildTemplates.TankRegen());

        var pawn = context.PlayerPawn;
        var arteries = pawn.Body.AllParts.Where(p => p.Type == BodyPartType.Artery).ToList();
        Assert.NotEmpty(arteries);
        var baselines = arteries.Select(a => a.MaxHitPoints).ToList();

        var def = RequireDef("Cyberveins");
        var item = context.Factory.CreateEntity<Item>(def, 1);
        Assert.True(item.MedicinalHandler!.ApplyToPart(item, pawn.Body.RootSocket.AttachedPart!));

        for (var i = 0; i < arteries.Count; i++)
        {
            Assert.Equal(baselines[i] * 3, arteries[i].MaxHitPoints);
            Assert.Equal(arteries[i].MaxHitPoints, arteries[i].HitPoints);
        }
    }

    [Fact]
    public void DuplicateCyberveinsSlotsDoNotStackInCombat()
    {
        AssertInstallDoesNotStack("Cyberveins", p => p.Type == BodyPartType.Artery, before => before * 3);
    }

    [Fact]
    public void DuplicateStrengthenBonesSlotsDoNotStackInCombat()
    {
        AssertInstallDoesNotStack("StrengthenBones", p => p.Substance == SubstanceType.Bone, before => before * 1.40);
    }

    [Fact]
    public void BandageHealsFleshAndSkinOnly()
    {
        using var root = SimServices.BuildRoot();
        using var scope = root.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<GameContext>();
        context.Initialize(CombatReplay.DefaultRunSeed);
        BuildSnapshotFactory.Apply(context.PlayerPawn, BuildTemplates.TankRegen());

        var pawn = context.PlayerPawn;
        var limb = pawn.Body.AllExternalParts.First(p =>
            p.Substance == SubstanceType.Flesh && p.Type != BodyPartType.Eye && p.HasBones);
        var bone = limb.AllInternalParts.First(p => p.Substance == SubstanceType.Bone);
        var organ = pawn.Body.AllParts.First(p => p.IsOrgan);
        var skin = limb.AllInternalParts.FirstOrDefault(p => p.Type == BodyPartType.Skin);

        limb.HitPoints = limb.MaxHitPoints * 0.3;
        bone.HitPoints = bone.MaxHitPoints * 0.3;
        organ.HitPoints = organ.MaxHitPoints * 0.3;
        if (skin != null)
        {
            skin.HitPoints = skin.MaxHitPoints * 0.3;
        }

        var item = context.Factory.CreateEntity<Item>(RequireDef("Bandage"), 1);
        Assert.True(item.MedicinalHandler!.ApplyToPart(item, limb));
        Assert.Equal(limb.MaxHitPoints, limb.HitPoints);
        Assert.Equal(bone.MaxHitPoints * 0.3, bone.HitPoints, 3);
        Assert.Equal(organ.MaxHitPoints * 0.3, organ.HitPoints, 3);
        if (skin != null)
        {
            Assert.Equal(skin.MaxHitPoints, skin.HitPoints);
        }
    }

    [Fact]
    public void BoneGlueHealsOnlyBonesOnTargetLimb()
    {
        using var root = SimServices.BuildRoot();
        using var scope = root.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<GameContext>();
        context.Initialize(CombatReplay.DefaultRunSeed);
        BuildSnapshotFactory.Apply(context.PlayerPawn, BuildTemplates.TankRegen());

        var pawn = context.PlayerPawn;
        var limbs = pawn.Body.AllExternalParts
            .Where(p => p.HasBones && p.Type != BodyPartType.Eye)
            .Take(2)
            .ToList();
        Assert.True(limbs.Count >= 2);

        var targetBones = limbs[0].AllInternalParts.Where(p => p.Substance == SubstanceType.Bone).ToList();
        var otherBones = limbs[1].AllInternalParts.Where(p => p.Substance == SubstanceType.Bone).ToList();
        Assert.NotEmpty(targetBones);
        Assert.NotEmpty(otherBones);

        foreach (var bone in targetBones.Concat(otherBones))
        {
            bone.HitPoints = bone.MaxHitPoints * 0.25;
        }

        var flesh = limbs[0];
        if (flesh.Substance == SubstanceType.Flesh)
        {
            flesh.HitPoints = flesh.MaxHitPoints * 0.25;
        }

        var item = context.Factory.CreateEntity<Item>(RequireDef("BoneGlue"), 1);
        Assert.True(item.MedicinalHandler!.ApplyToPart(item, limbs[0]));
        Assert.All(targetBones, b => Assert.Equal(b.MaxHitPoints, b.HitPoints));
        Assert.All(otherBones, b => Assert.Equal(b.MaxHitPoints * 0.25, b.HitPoints, 3));
        if (flesh.Substance == SubstanceType.Flesh)
        {
            Assert.Equal(flesh.MaxHitPoints * 0.25, flesh.HitPoints, 3);
        }
    }

    [Fact]
    public void AntidoteClearsPoisonAndNoopsWhenClean()
    {
        using var root = SimServices.BuildRoot();
        using var scope = root.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<GameContext>();
        context.Initialize(CombatReplay.DefaultRunSeed);
        BuildSnapshotFactory.Apply(context.PlayerPawn, BuildTemplates.TankRegen());

        var pawn = context.PlayerPawn;
        var artery = pawn.Body.AllParts.First(p => p.Type == BodyPartType.Artery);
        Afflict(context, artery, Defs.BodyPartModifiers.Poison);
        Assert.True(artery.HasModifier(Defs.BodyPartModifiers.Poison));

        var item = context.Factory.CreateEntity<Item>(RequireDef("Antidote"), 1);
        Assert.True(item.MedicinalHandler!.ApplyToPart(item, pawn.Body.RootSocket.AttachedPart!));
        Assert.DoesNotContain(pawn.Body.AllParts, p => p.HasModifier(Defs.BodyPartModifiers.Poison));
        Assert.False(item.MedicinalHandler.ApplyToPart(item, pawn.Body.RootSocket.AttachedPart!));
    }

    [Fact]
    public void ClotPackRestoresQuarterBloodAndNoopsWhenFull()
    {
        using var root = SimServices.BuildRoot();
        using var scope = root.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<GameContext>();
        context.Initialize(CombatReplay.DefaultRunSeed);
        BuildSnapshotFactory.Apply(context.PlayerPawn, BuildTemplates.TankRegen());

        var pawn = context.PlayerPawn;
        pawn.Body.BloodAmount = pawn.Body.MaxBlood * 0.5f;
        var item = context.Factory.CreateEntity<Item>(RequireDef("ClotPack"), 1);

        Assert.True(item.MedicinalHandler!.ApplyToPart(item, pawn.Body.RootSocket.AttachedPart!));
        Assert.Equal(pawn.Body.MaxBlood * 0.75f, pawn.Body.BloodAmount, 2);

        pawn.Body.BloodAmount = pawn.Body.MaxBlood;
        Assert.False(item.MedicinalHandler.ApplyToPart(item, pawn.Body.RootSocket.AttachedPart!));
        Assert.Equal(pawn.Body.MaxBlood, pawn.Body.BloodAmount);
    }

    [Fact]
    public void MedKitCannotSealSockets()
    {
        using var root = SimServices.BuildRoot();
        using var scope = root.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<GameContext>();
        context.Initialize(CombatReplay.DefaultRunSeed);
        BuildSnapshotFactory.Apply(context.PlayerPawn, BuildTemplates.TankRegen());

        var pawn = context.PlayerPawn;
        pawn.MedicalChest.Clear();
        var def = RequireDef("MedKit");
        Assert.True(pawn.MedicalChest.TryInstall(def, 1));
        Assert.False(MedicalTrigger.CanSealSocket(def));
    }

    private static void AssertInstallDoesNotStack(
        string moniker,
        Func<BodyPart, bool> match,
        Func<double, double> expectedMax)
    {
        using var root = SimServices.BuildRoot();
        using var scope = root.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<GameContext>();
        context.Initialize(CombatReplay.DefaultRunSeed);

        var pawn = context.PlayerPawn;
        pawn.MedicalChest.Clear();
        var def = RequireDef(moniker);
        Assert.True(pawn.MedicalChest.TryInstall(def, 1));
        Assert.True(pawn.MedicalChest.TryInstall(def, 1));

        var parts = pawn.Body.AllParts.Where(match).ToList();
        Assert.NotEmpty(parts);
        var baselines = parts.Select(p => p.MaxHitPoints).ToList();
        var scale = pawn.GetStatValue(Defs.Stats.BodyScale);

        var zone = context.World.Zones.OrderBy(z => z.ZoneDef.Stage).First();
        context.EnterZone(zone.ZoneDef);
        context.CurrentZone!.NextEncounter();
        context.Tick();

        var used = context.CurrentZone.ActiveEncounter!.CombatHandler!.Log
            .Count(e => e.Kind == CombatEventKind.MedicalUsed && e.ItemMoniker == moniker);
        Assert.Equal(1, used);

        for (var i = 0; i < parts.Count; i++)
        {
            Assert.Equal(expectedMax(baselines[i] * scale), parts[i].MaxHitPoints, 3);
        }
    }

    private static void PrepareFor(string moniker, Pawn pawn, GameContext context)
    {
        switch (moniker)
        {
            case "Suture":
                DamageFirst(pawn, p => p.Type == BodyPartType.Artery, 0.2);
                break;
            case "BalmyOintment":
                Afflict(context, FirstExternal(pawn), Defs.BodyPartModifiers.Burning);
                break;
            case "AntiNecroticSerum":
                Afflict(context, FirstOrgan(pawn), Defs.BodyPartModifiers.Necrosis);
                break;
            case "Cauterize":
                SeverLimb(pawn);
                break;
            case "BoneCleanse":
            case "BoneGlue":
                DamageFirst(pawn, p => p.Substance == SubstanceType.Bone, 0.4);
                break;
            case "Antidote":
                foreach (var artery in pawn.Body.AllParts.Where(p => p.Type == BodyPartType.Artery && !p.IsDestroyed))
                {
                    Afflict(context, artery, Defs.BodyPartModifiers.Poison);
                }

                Disarm(pawn);
                break;
            case "ClotPack":
                pawn.Body.BloodAmount = pawn.Body.MaxBlood * 0.2f;
                Disarm(pawn);
                break;
            case "StrengthenBones":
            case "Cyberveins":
                break;
            default:
                DamageFirst(pawn, p => p.IsExternal && p.Type != BodyPartType.Eye, 0.2);
                DamageFirst(pawn, p => p.IsOrgan, 0.2);
                break;
        }
    }

    private static void Disarm(Pawn pawn)
    {
        foreach (var (weapon, _) in pawn.Equipment.Weapons.ToList())
        {
            pawn.Equipment.UnEquip(weapon);
        }
    }

    private static void DamageFirst(Pawn pawn, Func<BodyPart, bool> match, double healthPercent)
    {
        var part = pawn.Body.AllParts.FirstOrDefault(match);
        Assert.NotNull(part);
        part.HitPoints = Math.Max(1, part.MaxHitPoints * healthPercent);
    }

    private static BodyPart FirstExternal(Pawn pawn)
    {
        return pawn.Body.AllExternalParts.First(p => p.Type != BodyPartType.Eye && !p.IsDestroyed);
    }

    private static BodyPart FirstOrgan(Pawn pawn)
    {
        return pawn.Body.AllParts.First(p => p.IsOrgan && !p.IsDestroyed);
    }

    private static void Afflict(GameContext context, BodyPart part, BodyPartModifierDef def)
    {
        part.TryAddModifier(context.Factory.CreateModifier(def, 2000, 1));
    }

    private static void SeverLimb(Pawn pawn)
    {
        var limb = pawn.Body.AllExternalParts.First(p =>
            p.Socket != pawn.Body.RootSocket && p.Type is BodyPartType.Finger or BodyPartType.Hand or BodyPartType.Arm);
        limb.Severe();
        Assert.NotNull(MedicalTrigger.FindUnsealedSocket(pawn));
    }

    private static ItemDef RequireDef(string moniker)
    {
        return DefRepository<ItemDef>.GetByMoniker(moniker)
               ?? throw new InvalidOperationException($"Missing medical def '{moniker}'.");
    }
}
