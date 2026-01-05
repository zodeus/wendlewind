using System.Reflection;

namespace Grafted.Definitions;

[AttributeUsage(AttributeTargets.Class)]
public class DefLocator : Attribute { }

public static partial class Defs
{
    [DefLocator]
    public static class Stats
    {
        public static StatDef MaxHitPoints = null!;
        public static StatDef WeaponPower = null!;
        public static StatDef Strength = null!;
        public static StatDef AttackSpeed = null!;
        public static StatDef HealingValue = null!;
        public static StatDef Accuracy = null!;
        public static StatDef PhysicalResistance = null!;
        public static StatDef MaxDurability = null!;
        public static StatDef MoveSpeed = null!;
        public static StatDef NutritionalValue = null!;
        public static StatDef Evasion = null!;
        public static StatDef AttackSpeedModifier = null!;
        public static StatDef PotionDuration = null!;
    }
}

public static partial class Defs
{
    [DefLocator]
    public static class BodyStances
    {
        public static BodyStanceDef Comfortable = null!;
    }
}

public static partial class Defs
{
    [DefLocator]
    public static class PawnLoadouts
    {
        public static PawnLoadoutDef DefaultStarterLoadout = null!;
    }
}

public static partial class Defs
{
    [DefLocator]
    public static class BodyPartModifiers
    {
        public static BodyPartModifierDef Acid = null!;
        public static BodyPartModifierDef RotLung = null!;
        public static BodyPartModifierDef SoothingBalm = null!;
        public static BodyPartModifierDef PurpleRegeneration = null!;
        public static BodyPartModifierDef LifeRegeneration = null!;
        public static BodyPartModifierDef RhinoRestoration = null!;
        public static BodyPartModifierDef Necrosis = null!;
        public static BodyPartModifierDef NecrosisSerum = null!;
    }
}

public static partial class Defs
{
    [DefLocator]
    public static class BodyEffects
    {
        public static BodyEffectDef FoodPoisoning = null!;
        public static BodyEffectDef BeefedUp = null!;
        public static BodyEffectDef FeelingThePurple = null!;
        public static BodyEffectDef SmokeyHaze = null!;
        public static BodyEffectDef GoldenSmoke = null!;
        public static BodyEffectDef Psychedelic = null!;
        public static BodyEffectDef GoldenLips = null!;
        public static BodyEffectDef Fruiting = null!;
    }
}

public static partial class Defs
{
    [DefLocator]
    public static class Zones
    {
        public static ZoneDef PeacefulMeadow = null!;
        public static ZoneDef TheOutskirts = null!;
        public static ZoneDef Thicket = null!;
        public static ZoneDef GrainMill = null!;
        public static ZoneDef FrogPond = null!;
        public static ZoneDef ForgottenForest = null!;
        public static ZoneDef DampCave = null!;
        public static ZoneDef StoneGarden = null!;
        public static ZoneDef Mineshaft = null!;
        public static ZoneDef AbandonedStables = null!;
        public static ZoneDef WearbearDen = null!;
        public static ZoneDef Swamp = null!;
        public static ZoneDef Cemetery = null!;
    }
}

public static partial class Defs
{
    [DefLocator]
    public static class Items
    {
        public static ItemDef SteroidInjector = null!;
        public static ItemDef PlagueMask = null!;
        public static ItemDef Walnut = null!;
        public static ItemDef Monocle = null!;
        public static ItemDef TinkersToolbox = null!;
        public static ItemDef BoneShard = null!;
        public static ItemDef GoldenBean = null!;
        public static ItemDef LeatherScraps = null!;
        public static ItemDef Fang = null!;
        public static ItemDef FishBowlHelmet = null!;
        public static ItemDef ClothHelmet = null!;
        public static ItemDef ClothTunic = null!;
        public static ItemDef ClothGorget = null!;
        public static ItemDef StoneHammer = null!;
        public static ItemDef BoneKnife = null!;
        public static ItemDef BoneSpear = null!;
        public static ItemDef BoneAxe = null!;
        public static ItemDef WoodClub = null!;
        public static ItemDef RawCorn = null!;
        public static ItemDef RawGrain = null!;
        public static ItemDef RawMeat = null!;
        public static ItemDef RawFish = null!;
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
        public static ItemDef SpicedChurni = null!;
        public static ItemDef Firewood = null!;
        public static ItemDef WoodBoard = null!;
        public static ItemDef WeepingBucket = null!;
        public static ItemDef CookingPot = null!;
        public static ItemDef BalmyOintment = null!;
        public static ItemDef TheDreamingPowder = null!;
        public static ItemDef DreamBerry = null!;
        public static ItemDef FlameStick = null!;
        public static ItemDef MortarAndPestle = null!;
        public static ItemDef VialOfDuplicity = null!;
        public static ItemDef HealingRoot = null!;
        public static ItemDef GlitteringLog = null!;
        public static ItemDef ShimmeringBark = null!;
        public static ItemDef GoldenWood = null!;
        public static ItemDef SoothingVibrations = null!;
        public static ItemDef SpidersBite = null!;
        public static ItemDef ThirdEye = null!;
        public static ItemDef EnchantmentExpander = null!;
        public static ItemDef HeartyStew = null!;
        public static ItemDef GoldCapMushroom = null!;

        public static ItemDef LeatherGlove = null!;
        public static ItemDef LeatherVambrace = null!;
        public static ItemDef LeatherBoot = null!;
        public static ItemDef BucketHelmet = null!;

        public static ItemDef Grimoire = null!;
        public static ItemDef BloodBath = null!;
        public static ItemDef RejuvenationCloak = null!;
        public static ItemDef StrengthCloak = null!;
        
        // Enchantments used as upgrade materials
        public static ItemDef ElvishLeaf = null!;
        public static ItemDef RhinoSkin = null!;
    }
}

public static partial class Defs
{
    [DefLocator]
    public static class BodyParts
    {
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
        public static BodyPartDef Spleen = null!;
        public static BodyPartDef Kidney = null!;
        public static BodyPartDef Liver = null!;
        public static BodyPartDef Intestines = null!;

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

        public static BodyPartDef TurtleHead = null!;
        public static BodyPartDef TurtleNeck = null!;
        public static BodyPartDef TurtleTorso = null!;
        public static BodyPartDef TurtleFlipper = null!;
        public static BodyPartDef TurtleLeg = null!;
        public static BodyPartDef TurtleTail = null!;

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
        public static BodyPartDef TreeLegStump = null!;
        public static BodyPartDef TreeArmStump = null!;

        public static BodyPartDef MushroomCap = null!;
        public static BodyPartDef MushroomStump = null!;
        public static BodyPartDef MushroomArm = null!;
        public static BodyPartDef MushroomHand = null!;
        public static BodyPartDef MushroomLeg = null!;
        public static BodyPartDef MushroomFoot = null!;

        public static BodyPartDef OrcHead = null!;
        public static BodyPartDef OrcTorso = null!;
        public static BodyPartDef OrcArm = null!;
        public static BodyPartDef OrcLeg = null!;
        public static BodyPartDef OrcHand = null!;
        public static BodyPartDef OrcThumb = null!;
        public static BodyPartDef OrcFinger = null!;
        public static BodyPartDef OrcFoot = null!;

        public static BodyPartDef MosquitoHead = null!;
        public static BodyPartDef MosquitoProboscis = null!;
        public static BodyPartDef MosquitoAntenna = null!;
        public static BodyPartDef MosquitoThorax = null!;
        public static BodyPartDef MosquitoWing = null!;
        public static BodyPartDef MosquitoLeg = null!;
        public static BodyPartDef MosquitoAbdomen = null!;

        public static BodyPartDef BeeHead = null!;
        public static BodyPartDef BeeAntenna = null!;
        public static BodyPartDef BeeThorax = null!;
        public static BodyPartDef BeeWing = null!;
        public static BodyPartDef BeeLeg = null!;
        public static BodyPartDef BeeAbdomen = null!;
        public static BodyPartDef BeeDrone = null!;
        public static BodyPartDef HorseHead = null!;
        public static BodyPartDef HorseNeck = null!;
        public static BodyPartDef HorseTorso = null!;
        public static BodyPartDef HorseLeg = null!;
        public static BodyPartDef HorseHoof = null!;
        public static BodyPartDef HorseTail = null!;

        public static BodyPartDef WearbearHead = null!;
        public static BodyPartDef WearbearTorso = null!;
        public static BodyPartDef WearbearArm = null!;
        public static BodyPartDef WearbearHand = null!;
        public static BodyPartDef WearbearLeg = null!;
        public static BodyPartDef WearbearFoot = null!;
        public static BodyPartDef WearbearTail = null!;

        public static BodyPartDef InukshukHead = null!;
        public static BodyPartDef InukshukTorso = null!;
        public static BodyPartDef InukshukArm = null!;
        public static BodyPartDef InukshukLeg = null!;

        public static BodyPartDef TinManHead = null!;
        public static BodyPartDef TinManTorso = null!;
        public static BodyPartDef TinManArm = null!;
        public static BodyPartDef TinManHand = null!;
        public static BodyPartDef TinManLeg = null!;
        public static BodyPartDef TinManFoot = null!;

        public static BodyPartDef BatHead = null!;
        public static BodyPartDef BatTorso = null!;
        public static BodyPartDef BatWing = null!;
        public static BodyPartDef BatClaw = null!;

        public static BodyPartDef UndeadMonkeyHead = null!;
        public static BodyPartDef UndeadMonkeyTorso = null!;
        public static BodyPartDef UndeadMonkeyArm = null!;
        public static BodyPartDef UndeadMonkeyHand = null!;
        public static BodyPartDef UndeadMonkeyFinger = null!;
        public static BodyPartDef UndeadMonkeyThumb = null!;
        public static BodyPartDef UndeadMonkeyLeg = null!;
        public static BodyPartDef UndeadMonkeyFoot = null!;
        public static BodyPartDef UndeadMonkeyTail = null!;

        public static BodyPartDef GameBoxHead = null!;
        public static BodyPartDef GameBoxTorso = null!;
        public static BodyPartDef GameBoxControls = null!;
        public static BodyPartDef GameBoxArm = null!;
        public static BodyPartDef GameBoxHand = null!;
        public static BodyPartDef GameBoxLeg = null!;
    }

    [DefLocator]
    public static class BodyPartSockets
    {
        public static BodyPartSocketDef HeadSocket = null!;
        public static BodyPartSocketDef TorsoSocket = null!;
        public static BodyPartSocketDef HandSocket = null!;

        //Treeborn
        public static BodyPartSocketDef TreeTrunkSocket = null!;
    }

    [DefLocator]
    public static class Traits
    {
        public static TraitDef PotBellied = null!;
        public static TraitDef ThickBlooded = null!;
        public static TraitDef GutMicroacrobatics = null!;
    }

    [DefLocator]
    public static class Weathers
    {
        public static WeatherDef Neutral = null!;
        public static WeatherDef Showers = null!;
        public static WeatherDef Storm = null!;
        public static WeatherDef Snow = null!;
        public static WeatherDef SmokeEmbers = null!;
        public static WeatherDef BloodRain = null!;
        public static WeatherDef Fireflies = null!;
        public static WeatherDef FallingLeaves = null!;
        public static WeatherDef HallowedRain = null!;
        public static WeatherDef AcidDrips = null!;
    }
}

public static class DefsBinder
{
    public static void BindLocators()
    {
        foreach (Type item in GenTypes.AllTypesWithAttribute<DefLocator>())
        {
            BindDefsToStaticClass(item);
        }
    }

    private static void BindDefsToStaticClass(IReflect type)
    {
        FieldInfo[] fields = type.GetFields(BindingFlags.Static | BindingFlags.Public);
        foreach (FieldInfo fieldInfo in fields)
        {
            Def def = (Def)typeof(DefRepository<>)
                .MakeGenericType(fieldInfo.FieldType)
                .GetMethod("GetByMoniker")!
                .Invoke(null, new object[] { fieldInfo.Name, true })!;
            fieldInfo.SetValue(null, def);
        }
    }
}