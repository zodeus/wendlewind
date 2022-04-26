using System;
using System.Reflection;
using Grafted.Sim;
using Grafted.Sim.Entities;
using Grafted.Sim.Entities.Items;
using Grafted.Sim.Entities.Pawns;
using Grafted.Utils;

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
        public static StatDef SequencePoints = null!;
        public static StatDef NutritionalValue = null!;
        public static StatDef HealingValue = null!;
        public static StatDef MeleeChanceToHit = null!;
        public static StatDef PhysicalResistance = null!;
        public static StatDef MaxDurability = null!;
        public static StatDef MaxCarryWeight = null!;
        public static StatDef Weight = null!;
        public static StatDef CurrencyValue = null!;
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
    public static class Zones {
        public static ZoneDef Intro = null!;
        public static ZoneDef VillageOfTheDamned = null!;
        public static ZoneDef TheOutskirts = null!;
    }
}

public static partial class Defs {
    [DefLocator]
    public static class Items {
        public static ItemDef MedKit = null!;
        public static ItemDef Cauterize = null!;
        public static ItemDef ArterialThreads = null!;
        public static ItemDef MendersMist = null!;
        public static ItemDef RepairKit = null!;
        public static ItemDef JarOfBlood = null!;
        public static ItemDef AcidFlask = null!;
        public static ItemDef PumpinJuice = null!;
        public static ItemDef SoulCoin = null!;
        public static ItemDef ShortSword = null!;
    }
}

public static partial class Defs {
    [DefLocator]
    public static class Species {
        public static PawnDef Human = null!;
    }
}

public static partial class Defs {
    [DefLocator]
    public static class Races {
        public static RaceDef Glump = null!;
    }
}

public static partial class Defs {
    [DefLocator]
    public static class BodyParts {
        public static BodyPartDef HumanHead = null!;
        public static BodyPartDef HumanNeck = null!;
        public static BodyPartDef HumanEye = null!;
        public static BodyPartDef HumanSkin = null!;
        public static BodyPartDef HumanBone = null!;
        public static BodyPartDef HumanSkull = null!;
        public static BodyPartDef HumanTorso = null!;
        public static BodyPartDef HumanArm = null!;

        public static BodyPartDef HumanBrain = null!;
        public static BodyPartDef HumanHeart = null!;
        public static BodyPartDef HumanLung = null!;
        public static BodyPartDef HumanStomach = null!;
        public static BodyPartDef HumanArtery = null!;
        public static BodyPartDef HumanRibCage = null!;
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
    }

    [DefLocator]
    public static class BodyPartSockets {
        public static BodyPartSocketDef HeadSocket = null!;
        public static BodyPartSocketDef TorsoSocket = null!;
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