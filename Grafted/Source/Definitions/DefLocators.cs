using System.Reflection;
using Grafted.Sim.Entities;
using Grafted.Sim.Entities.Pawns.Modifiers;

namespace Grafted.Definitions;

[AttributeUsage(AttributeTargets.Class)]
public class DefLocator : Attribute { }

public static partial class Defs {
    [DefLocator]
    public static class Stats {
        public static StatDef MaxHitPoints = null!;
        public static StatDef MeleePower = null!;
        public static StatDef MeleeStrength = null!;
        public static StatDef AttackSpeed = null!;
        public static StatDef HealingValue = null!;
        public static StatDef MeleeAccuracy = null!;
        public static StatDef PhysicalResistance = null!;
        public static StatDef MaxDurability = null!;
        public static StatDef MoveSpeed = null!;
        public static StatDef NutritionalValue = null!;
        public static StatDef Evasion = null!;
        public static StatDef AttackSpeedModifier = null!;
        public static StatDef WeaponSpeed  = null!;
        public static StatDef PotionDuration  = null!;
    }
}

public static partial class Defs {
    [DefLocator]
    public static class PawnConfigs {
        public static PawnConfigDef PlayerPawn = null!;
    }
}

public static partial class Defs {
    [DefLocator]
    public static class BodyPartModifiers {
        public static BodyPartModifierDef BurningAcid = null!;
        public static BodyPartModifierDef RotLung = null!;
        public static BodyPartModifierDef SoothingBalm = null!;
        public static BodyPartModifierDef PurpleRegeneration = null!;
        public static BodyPartModifierDef LifeRegeneration = null!;
    }
}

public static partial class Defs {
    [DefLocator]
    public static class BodyEffects {
        public static BodyEffectDef BeefedUp = null!;
        public static BodyEffectDef FeelingThePurple = null!;
        public static BodyEffectDef SmokeyHaze = null!;
        public static BodyEffectDef Psychedelic = null!;
        public static BodyEffectDef DeathToll = null!;
    }
}

public static partial class Defs {
    [DefLocator]
    public static class Zones {
        public static BiomeDef PeacefulMeadow = null!;
        public static BiomeDef TheOutskirts = null!;
        public static BiomeDef GrainMill = null!;
        public static BiomeDef FesterpusSwamp = null!;
        public static BiomeDef ForgottenForest = null!;
        public static BiomeDef DampCave = null!;
    }
}

public static partial class Defs {
    [DefLocator]
    public static class Items {
        public static ItemDef RawCorn = null!;
        public static ItemDef RawMeat = null!;
        public static ItemDef CookedCorn = null!;
        public static ItemDef CookedMeat = null!;
        public static ItemDef DriedMeat = null!;
        public static ItemDef MedKit = null!;
        public static ItemDef Cauterize = null!;
        public static ItemDef ArterialThreads = null!;
        public static ItemDef MendersMist = null!;
        public static ItemDef RepairKit = null!;
        public static ItemDef JarOfBlood = null!;
        public static ItemDef AcidFlask = null!;
        public static ItemDef PurpleJuice = null!;
        public static ItemDef ShortSword = null!;
        public static ItemDef Firewood = null!;
        public static ItemDef WoodBoard = null!;
        public static ItemDef EndlessWaterBucket = null!;
        public static ItemDef CookingPot = null!;
        public static ItemDef BalmyOintment = null!;
        public static ItemDef TheDreamingPowder = null!;
        public static ItemDef DreamBerry = null!;
        public static ItemDef CowardsFlag = null!;
        public static ItemDef EssenceShard = null!;
        public static ItemDef EncasedFire = null!;
        public static ItemDef MortarAndPestle = null!;
        public static ItemDef VialOfDuplicity = null!;
        public static ItemDef HealingRoot = null!;
        public static ItemDef GlitteringLog = null!;
        public static ItemDef ShimmeringBark = null!;
        public static ItemDef SoothingVibrations = null!;
        public static ItemDef SpidersBite = null!;
    }
}

public static partial class Defs {
    [DefLocator]
    public static class Species {
        public static PawnDef Human = null!;
        public static PawnDef Frog = null!;
        public static PawnDef Rabbit = null!;
        public static PawnDef Wolf = null!;
        public static PawnDef Glump = null!;
        public static PawnDef Ghoul = null!;
        public static PawnDef Pig = null!;
    }
}

public static partial class Defs {
    [DefLocator]
    public static class Races {
        public static RaceDef Glump = null!;
        public static RaceDef Bunny = null!;
        public static RaceDef FieldHound = null!;
        public static RaceDef TruffleBoar = null!;
        public static RaceDef BlisteringToad = null!;
        public static RaceDef Journeyman = null!;
        public static RaceDef Ghoul = null!;
    }
}

public static partial class Defs {
    [DefLocator]
    public static class BodyParts {
        public static BodyPartDef Brain = null!;
        public static BodyPartDef Eye = null!;
        public static BodyPartDef Skin = null!;
        public static BodyPartDef Bone = null!;
        public static BodyPartDef Skull = null!;
        public static BodyPartDef Heart = null!;
        public static BodyPartDef Lung = null!;
        public static BodyPartDef Stomach = null!;
        public static BodyPartDef Artery = null!;
        public static BodyPartDef RibCage = null!;

        public static BodyPartDef HumanHead = null!;
        public static BodyPartDef HumanNeck = null!;
        public static BodyPartDef HumanTorso = null!;
        public static BodyPartDef HumanArm = null!;
        public static BodyPartDef HumanHand = null!;
        public static BodyPartDef HumanThumb = null!;
        public static BodyPartDef HumanFinger = null!;
        public static BodyPartDef HumanLeg = null!;
        public static BodyPartDef HumanFoot = null!;

        public static BodyPartDef GlumpTorso = null!;
        public static BodyPartDef GlumpRibCage = null!;
        public static BodyPartDef GlumpArm = null!;
        public static BodyPartDef GlumpHand = null!;
        public static BodyPartDef GlumpLeg = null!;
        public static BodyPartDef GlumpFoot = null!;

        public static BodyPartDef RabbitHead = null!;
        public static BodyPartDef RabbitNeck = null!;
        public static BodyPartDef RabbitTorso = null!;
        public static BodyPartDef RabbitLeg = null!;
        public static BodyPartDef RabbitPaw = null!;


        public static BodyPartDef FrogHead = null!;
        public static BodyPartDef FrogTorso = null!;
        public static BodyPartDef FrogLeg = null!;
        public static BodyPartDef FrogFoot = null!;

        public static BodyPartDef WolfHead = null!;
        public static BodyPartDef WolfNeck = null!;
        public static BodyPartDef WolfTorso = null!;
        public static BodyPartDef WolfLeg = null!;
        public static BodyPartDef WolfPaw = null!;
        public static BodyPartDef WolfTail = null!;

        public static BodyPartDef PigHead = null!;
        public static BodyPartDef PigNeck = null!;
        public static BodyPartDef PigTorso = null!;
        public static BodyPartDef PigLeg = null!;
        public static BodyPartDef PigHoof = null!;
        public static BodyPartDef PigTail = null!;

        public static BodyPartDef GhoulHead = null!;
        public static BodyPartDef GhoulNeck = null!;
        public static BodyPartDef GhoulTorso = null!;
        public static BodyPartDef GhoulArm = null!;
        public static BodyPartDef GhoulHand = null!;
        public static BodyPartDef GhoulThumb = null!;
        public static BodyPartDef GhoulFinger = null!;
        public static BodyPartDef GhoulLeg = null!;
        public static BodyPartDef GhoulFoot = null!;
        
        public static BodyPartDef TreeTrunk = null!;
        public static BodyPartDef TreeInnerCore = null!;
        public static BodyPartDef TreeStump = null!;
        
        public static BodyPartDef MushroomCap = null!;
        public static BodyPartDef MushroomStump = null!;
        public static BodyPartDef MushroomArm = null!;
        public static BodyPartDef MushroomHand = null!;
        public static BodyPartDef MushroomLeg = null!;
        public static BodyPartDef MushroomFoot = null!;
        
    }

    [DefLocator]
    public static class BodyPartSockets {
        public static BodyPartSocketDef HeadSocket = null!;
        public static BodyPartSocketDef TorsoSocket = null!;
        public static BodyPartSocketDef HandSocket = null!;
        
        //Treeborn
        public static BodyPartSocketDef TreeTrunkSocket = null!;
    }
}

public static class DefsBinder {
    public static void BindLocators() {
        foreach (Type item in GenTypes.AllTypesWithAttribute<DefLocator>()) {
            BindDefsToStaticClass(item);
        }
    }

    private static void BindDefsToStaticClass(IReflect type) {
        FieldInfo[] fields = type.GetFields(BindingFlags.Static | BindingFlags.Public);
        foreach (FieldInfo fieldInfo in fields) {
            Def def = (Def) typeof(DefRepository<>)
                .MakeGenericType(fieldInfo.FieldType)
                .GetMethod("GetByMoniker")!
                .Invoke(null, new object[] { fieldInfo.Name, true })!;
            fieldInfo.SetValue(null, def);
        }
    }
}