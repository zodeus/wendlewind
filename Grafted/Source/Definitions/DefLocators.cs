using System;
using System.Reflection;
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
        public static StatDef Durability = null!;
    }
}

public static partial class Defs {
    [DefLocator]
    public static class Items {
        public static ItemDef MedKit = null!;
        public static ItemDef Cauterize = null!;
        public static ItemDef ArterialThreads = null!;
        public static ItemDef MendersMist = null!;
    }
}

public static partial class Defs {
    [DefLocator]
    public static class Species {
        public static PawnDef Human = null!;
        public static PawnDef Skeleton = null!;
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
        public static BodyPartDef HumanStomach = null!;
        public static BodyPartDef HumanArtery = null!;
        public static BodyPartDef HumanRibCage = null!;
        public static BodyPartDef HumanHand = null!;
        public static BodyPartDef HumanThumb = null!;
        public static BodyPartDef HumanFinger = null!;
        public static BodyPartDef HumanLeg = null!;
        public static BodyPartDef HumanFoot = null!;
    }

    [DefLocator]
    public static class BodyPartSockets {
        public static BodyPartSocketDef HeadSocket = null!;
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