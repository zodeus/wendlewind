using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Wendlemire.NetCode;
using Wendlemire.NetCode.Contracts;
using Wendlemire.Sim.Combat;
using Wendlemire.Sim.Entities.Items.Medicinals;
using Wendlemire.Sim.Entities.Items.Potions;
using Wendlemire.Sim.Entities.Pawns;
using Xunit;
using Xunit.Abstractions;

namespace Wendlemire.Tests;

/// <summary>
/// Human-vs-human balance across a ~13-round arena.
/// Shop timing used for loadouts:
///   R1+  primitive/iron, cloth/leather, Festering, Soothing
///   R2+  SpidersBite, ElvishLeaf, WD pieces
///   R4+  chain, WD set, BoneEater, BloodBath, FireStaff
///   R6+  plate, Everburning, RhinoSkin, BlessedIronCollar
/// Consumable kits ride along (meal + incense + medical chest + potions):
///   Early  cooked meat/fish, Mullin, threads+MedKit, JarOfBlood
///   Mid    stew+dried, ShadeWood, MedKit+Balmy+Mist, Jar+Acid
///   Late   stew+honey, Dipped+Shade, Mix+Cauterize+Bone, Jar+Churni
///   Full   stew+honey+walnut, 3 incense, Mix+Cauterize+Serum+Bone, Jar+Acid
/// Extra buckets: KIT (kit vs bare / burst vs sustain), FOOD, MED, INC, METAL, MAGIC, STEEL.
/// Writes balance-report.txt at the repo root.
/// Run: dotnet test --filter FullyQualifiedName~BalanceSimReport
/// Matchups run in parallel (ProcessorCount workers). Each duel still uses its own sim scope.
/// METAL-only: set BALANCE_BAND=METAL (PowerShell: $env:BALANCE_BAND='METAL')
/// MAGIC-only: set BALANCE_BAND=MAGIC
/// STEEL-only: set BALANCE_BAND=STEEL
/// </summary>
[Collection("Sim")]
public class BalanceSimReport
{
    private static readonly JsonSerializerOptions SidecarJson = new()
    {
        NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals
    };

    private const int SeedCount = 24;
    private const int TargetMinTicks = 900;
    private const int TargetMaxTicks = 1500;

    private readonly ITestOutputHelper _output;

    public BalanceSimReport(ITestOutputHelper output)
    {
        _output = output;
        TestData.EnsureLoaded();
    }

    #region Sets

    private static readonly string[] ClothCore = ["ClothHelmet", "ClothTunic"];
    private static readonly string[] LeatherCore = ["LeatherHelmet", "LeatherTunic"];

    private static readonly string[] ClothSet =
    [
        "ClothHelmet", "ClothGorget", "ClothTunic",
        "ClothGlove", "ClothGlove", "ClothVambrace", "ClothVambrace",
        "ClothGreave", "ClothGreave", "ClothBoot", "ClothBoot"
    ];

    private static readonly string[] LeatherSet =
    [
        "LeatherHelmet", "LeatherGorget", "LeatherTunic",
        "LeatherGlove", "LeatherGlove", "LeatherVambrace", "LeatherVambrace",
        "LeatherGreave", "LeatherGreave", "LeatherBoot", "LeatherBoot"
    ];

    private static readonly string[] ChainSet =
    [
        "ChainHelmet", "ChainGorget", "ChainTunic",
        "ChainGlove", "ChainGlove", "ChainVambrace", "ChainVambrace",
        "ChainGreave", "ChainGreave", "ChainBoot", "ChainBoot"
    ];

    private static readonly string[] PlateSet =
    [
        "PlateHelmet", "PlateGorget", "PlateTunic",
        "PlateGlove", "PlateGlove", "PlateVambrace", "PlateVambrace",
        "PlateGreave", "PlateGreave", "PlateBoot", "PlateBoot"
    ];

    private static readonly string[] WitchDoctorSet =
    [
        "WitchDoctorHelmet", "WitchDoctorGorget", "WitchDoctorTunic",
        "WitchDoctorGlove", "WitchDoctorGlove", "WitchDoctorVambrace", "WitchDoctorVambrace",
        "WitchDoctorGreave", "WitchDoctorGreave", "WitchDoctorBoot", "WitchDoctorBoot"
    ];

    private static readonly string[] WdUniqueMix =
    [
        "PlagueMask", "BlessedIronCollar", "WitchDoctorTunic",
        "WitchDoctorGlove", "WitchDoctorGlove", "WitchDoctorVambrace", "WitchDoctorVambrace",
        "WitchDoctorGreave", "WitchDoctorGreave", "WitchDoctorBoot", "WitchDoctorBoot"
    ];

    #endregion

    #region Builder

    private static BuildSnapshot Fighter(
        string id,
        string[] weapons,
        string[]? armor = null,
        SocketedItemConfig[]? sockets = null)
    {
        var items = new List<string>();
        items.AddRange(weapons);
        if (armor != null)
        {
            items.AddRange(armor);
        }

        return new BuildSnapshot
        {
            PlayerId = id,
            BuildId = id,
            PawnDefMoniker = "HumanA",
            EntityDefMonikers = items.ToArray(),
            StanceMoniker = "Offensive",
            Weapons = weapons
                .Select(w => new WeaponConfig { ItemMoniker = w, UseInCombat = true })
                .ToArray(),
            Sockets = sockets ?? []
        };
    }

    private sealed record Kit(
        string[] Meal,
        PotionConfig[] Potions,
        MedicalChestConfig[] Medical,
        IncenseConfig[] Incense)
    {
        public static readonly Kit None = new([], [], [], []);
    }

    private static PotionConfig Pot(
        string moniker,
        PotionTriggerType type,
        float threshold = 0,
        float after = 0,
        float health = 0.6f) =>
        new()
        {
            ItemMoniker = moniker,
            Type = type,
            Threshold = threshold,
            AfterSeconds = after,
            HealthThreshold = health
        };

    private static MedicalChestConfig Med(
        string moniker,
        int charges,
        MedicalTriggerType type,
        float health = 0.5f,
        MedicalTargetSelector sel = MedicalTargetSelector.Auto) =>
        new()
        {
            ItemMoniker = moniker,
            Charges = charges,
            Type = type,
            TargetSelector = sel,
            HealthThreshold = health
        };

    private static IncenseConfig Stick(string moniker, int encounters = 2) =>
        new() { ItemMoniker = moniker, EncountersRemaining = encounters };

    private static readonly Kit Early = new(
        ["CookedMeat", "CookedFish"],
        [Pot("JarOfBlood", PotionTriggerType.SelfBloodBelow, threshold: 0.25f)],
        [
            Med("Suture", 3, MedicalTriggerType.PartBelowHealth, health: 0.6f),
            Med("MedKit", 2, MedicalTriggerType.PartBelowHealth)
        ],
        [Stick("MullinStick")]);

    private static readonly Kit Mid = new(
        ["HeartyStew", "DriedMeat"],
        [
            Pot("JarOfBlood", PotionTriggerType.SelfBloodBelow, threshold: 0.2f),
            Pot("AcidFlask", PotionTriggerType.AfterSeconds, after: 5)
        ],
        [
            Med("MedKit", 3, MedicalTriggerType.PartBelowHealth),
            Med("BalmyOintment", 2, MedicalTriggerType.BurningOrAcid),
            Med("MendersMist", 2, MedicalTriggerType.PartBelowHealth)
        ],
        [Stick("ShadeWood")]);

    private static readonly Kit Late = new(
        ["HeartyStew", "HoneyPot"],
        [
            Pot("JarOfBlood", PotionTriggerType.SelfBloodBelow, threshold: 0.2f),
            Pot("HealingPotion", PotionTriggerType.SelfPartsDamaged, threshold: 0.4f)
        ],
        [
            Med("MendersMix", 2, MedicalTriggerType.PartBelowHealth, health: 0.4f),
            Med("Cauterize", 1, MedicalTriggerType.PartSevered, sel: MedicalTargetSelector.SeveredOrUnsealedSocket),
            Med("BoneCleanse", 1, MedicalTriggerType.PartBelowHealth),
            Med("BalmyOintment", 2, MedicalTriggerType.BurningOrAcid)
        ],
        [Stick("DippedMullinStick", 3)]);

    private static readonly Kit Full = new(
        ["HeartyStew", "Walnut"],
        [
            Pot("JarOfBlood", PotionTriggerType.SelfBloodBelow, threshold: 0.15f),
            Pot("AcidFlask", PotionTriggerType.AfterSeconds, after: 5)
        ],
        [
            Med("MendersMix", 3, MedicalTriggerType.PartBelowHealth, health: 0.4f),
            Med("Cauterize", 1, MedicalTriggerType.PartSevered, sel: MedicalTargetSelector.SeveredOrUnsealedSocket),
            Med("AntiNecroticSerum", 2, MedicalTriggerType.HasNecrosis),
            Med("BoneCleanse", 1, MedicalTriggerType.PartBelowHealth),
            Med("BalmyOintment", 2, MedicalTriggerType.BurningOrAcid)
        ],
        [Stick("DippedMullinStick", 3), Stick("MullinStick")]);

    private static readonly Kit Burst = new(
        ["DriedMeat", "CookedCorn"],
        [
            Pot("AcidFlask", PotionTriggerType.AfterSeconds, after: 5),
            Pot("PussBomb", PotionTriggerType.AfterSeconds, after: 4)
        ],
        [Med("Suture", 2, MedicalTriggerType.PartBelowHealth, health: 0.6f)],
        [Stick("ShadeWood")]);

    private static readonly Kit Sustain = new(
        ["HeartyStew", "CookedMeat"],
        [
            Pot("JarOfBlood", PotionTriggerType.SelfBloodBelow, threshold: 0.2f),
            Pot("HealingPotion", PotionTriggerType.SelfPartsDamaged, threshold: 0.4f)
        ],
        [
            Med("MedKit", 4, MedicalTriggerType.PartBelowHealth),
            Med("MendersMist", 3, MedicalTriggerType.PartBelowHealth),
            Med("Suture", 3, MedicalTriggerType.PartBelowHealth, health: 0.6f)
        ],
        [Stick("MullinStick")]);

    private static Kit MealOnly(params string[] foods) =>
        Early with { Meal = foods };

    private static Kit StickOnly(params IncenseConfig[] sticks) =>
        Mid with { Incense = sticks };

    private static Kit MedOnly(PotionConfig[] potions, MedicalChestConfig[] medical) =>
        Mid with { Potions = potions, Medical = medical };

    private static BuildSnapshot WithKit(BuildSnapshot snap, Kit kit)
    {
        if (kit == Kit.None)
        {
            return snap;
        }

        return snap with
        {
            EntityDefMonikers = [..snap.EntityDefMonikers, ..kit.Potions.Select(p => p.ItemMoniker)],
            Potions = kit.Potions,
            Meal = kit.Meal,
            MedicalChest = kit.Medical,
            Incense = kit.Incense
        };
    }

    private static Kit EraKit(string band) => band switch
    {
        "R4-6" or "METAL" or "MAGIC" or "STEEL" => Mid,
        "R7-9" => Late,
        "R10-13" => Full,
        _ => Early
    };

    private static Matchup EraMatch(string band, string name, BuildSnapshot a, BuildSnapshot b) =>
        new(band, name, WithKit(a, EraKit(band)), WithKit(b, EraKit(band)));

    private static Matchup Split(string band, string name, BuildSnapshot a, Kit aKit, BuildSnapshot b, Kit bKit) =>
        new(band, name, WithKit(a, aKit), WithKit(b, bKit));

    private static SocketedItemConfig Sock(string item, params string[] enchants) =>
        new() { ItemMoniker = item, EnchantmentMonikers = enchants };

    private static SocketedItemConfig[] LeatherLightEnchants() =>
    [
        Sock("LeatherHelmet", "ElvishLeaf"),
        Sock("LeatherGlove", "SoothingVibrations"),
        Sock("LeatherBoot", "ElvishLeaf")
    ];

    private static SocketedItemConfig[] LeatherMidEnchants() =>
    [
        Sock("LeatherHelmet", "ElvishLeaf"),
        Sock("LeatherGlove", "BloodBath"),
        Sock("LeatherGlove", "ElvishLeaf"),
        Sock("LeatherBoot", "SoothingVibrations")
    ];

    private static SocketedItemConfig[] ChainFullEnchants() =>
    [
        Sock("ChainHelmet", "RhinoSkin"),
        Sock("ChainGorget", "ElvishLeaf"),
        Sock("ChainTunic", "RhinoSkin"),
        Sock("ChainGlove", "BloodBath"),
        Sock("ChainGlove", "ElvishLeaf"),
        Sock("ChainVambrace", "SoothingVibrations"),
        Sock("ChainVambrace", "ElvishLeaf"),
        Sock("ChainGreave", "RhinoSkin"),
        Sock("ChainGreave", "BloodBath"),
        Sock("ChainBoot", "SoothingVibrations"),
        Sock("ChainBoot", "ElvishLeaf")
    ];

    private static SocketedItemConfig[] PlateFullEnchants() =>
    [
        Sock("PlateHelmet", "RhinoSkin"),
        Sock("PlateGorget", "ElvishLeaf"),
        Sock("PlateTunic", "RhinoSkin"),
        Sock("PlateGlove", "BloodBath"),
        Sock("PlateGlove", "ElvishLeaf"),
        Sock("PlateVambrace", "SoothingVibrations"),
        Sock("PlateVambrace", "ElvishLeaf"),
        Sock("PlateGreave", "RhinoSkin"),
        Sock("PlateGreave", "BloodBath"),
        Sock("PlateBoot", "SoothingVibrations"),
        Sock("PlateBoot", "ElvishLeaf")
    ];

    private static SocketedItemConfig[] WdHealStack() =>
    [
        Sock("WitchDoctorHelmet", "RhinoSkin", "ElvishLeaf"),
        Sock("WitchDoctorGorget", "ElvishLeaf", "SoothingVibrations"),
        Sock("WitchDoctorTunic", "RhinoSkin", "ElvishLeaf"),
        Sock("WitchDoctorGlove", "BloodBath", "ElvishLeaf"),
        Sock("WitchDoctorGlove", "SoothingVibrations", "ElvishLeaf"),
        Sock("WitchDoctorVambrace", "RhinoSkin", "BloodBath"),
        Sock("WitchDoctorVambrace", "ElvishLeaf", "SoothingVibrations"),
        Sock("WitchDoctorGreave", "RhinoSkin", "ElvishLeaf"),
        Sock("WitchDoctorGreave", "BloodBath", "SoothingVibrations"),
        Sock("WitchDoctorBoot", "RhinoSkin", "ElvishLeaf"),
        Sock("WitchDoctorBoot", "BloodBath", "ElvishLeaf")
    ];

    private static SocketedItemConfig[] WdReflectStack() =>
    [
        Sock("WitchDoctorHelmet", "SpidersBite", "RhinoSkin"),
        Sock("WitchDoctorGorget", "SpidersBite", "ElvishLeaf"),
        Sock("WitchDoctorTunic", "SpidersBite", "BloodBath"),
        Sock("WitchDoctorGlove", "SpidersBite", "ElvishLeaf"),
        Sock("WitchDoctorGlove", "SpidersBite", "SoothingVibrations"),
        Sock("WitchDoctorVambrace", "SpidersBite", "RhinoSkin"),
        Sock("WitchDoctorVambrace", "ElvishLeaf", "BloodBath"),
        Sock("WitchDoctorGreave", "SpidersBite", "ElvishLeaf"),
        Sock("WitchDoctorGreave", "RhinoSkin", "SoothingVibrations"),
        Sock("WitchDoctorBoot", "SpidersBite", "BloodBath"),
        Sock("WitchDoctorBoot", "ElvishLeaf", "RhinoSkin")
    ];

    private static SocketedItemConfig[] UniqueMixEnchants() =>
    [
        Sock("PlagueMask", "RhinoSkin", "SpidersBite"),
        Sock("BlessedIronCollar", "RhinoSkin", "ElvishLeaf", "BloodBath"),
        Sock("WitchDoctorTunic", "RhinoSkin", "ElvishLeaf"),
        Sock("WitchDoctorGlove", "BloodBath", "SpidersBite"),
        Sock("WitchDoctorGlove", "ElvishLeaf", "SoothingVibrations"),
        Sock("WitchDoctorVambrace", "RhinoSkin", "BloodBath"),
        Sock("WitchDoctorVambrace", "ElvishLeaf", "SpidersBite"),
        Sock("WitchDoctorGreave", "RhinoSkin", "SoothingVibrations"),
        Sock("WitchDoctorGreave", "BloodBath", "ElvishLeaf"),
        Sock("WitchDoctorBoot", "RhinoSkin", "SpidersBite"),
        Sock("WitchDoctorBoot", "BloodBath", "ElvishLeaf")
    ];

    private static SocketedItemConfig[] Weapon(string moniker, string enchant) =>
        [Sock(moniker, enchant)];

    private static SocketedItemConfig[] Combine(params SocketedItemConfig[][] groups) =>
        groups.SelectMany(g => g).ToArray();

    #endregion

    #region Loadouts by round band

    // R1-3
    private static BuildSnapshot ClubNaked(string id) => Fighter(id, ["WoodClub"]);
    private static BuildSnapshot AxeNaked(string id) => Fighter(id, ["BoneAxe"]);
    private static BuildSnapshot SpearNaked(string id) => Fighter(id, ["BoneSpear"]);
    private static BuildSnapshot KnifeNaked(string id) => Fighter(id, ["BoneKnife"]);
    private static BuildSnapshot HammerNaked(string id) => Fighter(id, ["StoneHammer"]);
    private static BuildSnapshot AxeCloth(string id) => Fighter(id, ["BoneAxe"], ClothSet);
    private static BuildSnapshot ClubCloth(string id) => Fighter(id, ["WoodClub"], ClothSet);
    private static BuildSnapshot SpearCloth(string id) => Fighter(id, ["BoneSpear"], ClothSet);
    private static BuildSnapshot DualPrimitive(string id) => Fighter(id, ["BoneAxe", "BoneKnife"]);
    private static BuildSnapshot AxeLeatherCore(string id) => Fighter(id, ["BoneAxe"], LeatherCore);
    private static BuildSnapshot SwordFesterCloth(string id) =>
        Fighter(id, ["IronSword"], ClothSet, Weapon("IronSword", "FesteringWounds"));
    private static BuildSnapshot SwordPlainCloth(string id) => Fighter(id, ["IronSword"], ClothSet);
    private static BuildSnapshot DaggerFesterCloth(string id) =>
        Fighter(id, ["IronDagger"], ClothSet, Weapon("IronDagger", "FesteringWounds"));

    // R4-6
    private static BuildSnapshot SwordLeather(string id) => Fighter(id, ["IronSword"], LeatherSet);
    private static BuildSnapshot MaceLeather(string id) => Fighter(id, ["IronMace"], LeatherSet);
    private static BuildSnapshot IronAxeLeather(string id) => Fighter(id, ["IronAxe"], LeatherSet);
    private static BuildSnapshot ClawsLeather(string id) => Fighter(id, ["IronClaws"], LeatherSet);
    private static BuildSnapshot DaggerLeather(string id) => Fighter(id, ["IronDagger"], LeatherSet);
    private static BuildSnapshot HammerLeather(string id) => Fighter(id, ["IronHammer"], LeatherSet);
    private static BuildSnapshot KnucklesLeather(string id) => Fighter(id, ["IronKnuckles"], LeatherSet);
    private static BuildSnapshot DualIronLeather(string id) =>
        Fighter(id, ["IronSword", "IronDagger"], LeatherSet,
            Combine(Weapon("IronDagger", "FesteringWounds"), LeatherLightEnchants()));
    private static BuildSnapshot DualDaggerLeather(string id) =>
        Fighter(id, ["IronDagger", "IronDagger"], LeatherSet,
            Combine(Weapon("IronDagger", "FesteringWounds"), LeatherLightEnchants()));
    private static BuildSnapshot SwordLeatherLeaf(string id) =>
        Fighter(id, ["IronSword"], LeatherSet,
            Combine(Weapon("IronSword", "FesteringWounds"), LeatherMidEnchants()));
    private static BuildSnapshot SwordBloodBathLeather(string id) =>
        Fighter(id, ["IronSword"], LeatherSet,
            Combine(
                Weapon("IronSword", "SpidersBite"),
                [Sock("LeatherTunic", "BloodBath")],
                LeatherLightEnchants()));
    private static BuildSnapshot FireStaffLeather(string id) =>
        Fighter(id, ["FireStaff"], LeatherSet, Weapon("FireStaff", "FesteringWounds"));
    private static BuildSnapshot FireStaffPlainLeather(string id) => Fighter(id, ["FireStaff"], LeatherSet);
    private static BuildSnapshot StormStaffLeather(string id) => Fighter(id, ["StormStaff"], LeatherSet);
    private static BuildSnapshot EmberWandLeather(string id) => Fighter(id, ["EmberWand"], LeatherSet);
    private static BuildSnapshot HexWandLeather(string id) => Fighter(id, ["HexWand"], LeatherSet);
    private static BuildSnapshot DualWandLeather(string id) => Fighter(id, ["EmberWand", "HexWand"], LeatherSet);
    private static BuildSnapshot EmberDaggerLeather(string id) => Fighter(id, ["EmberWand", "IronDagger"], LeatherSet);
    private static BuildSnapshot GreatswordLeather(string id) => Fighter(id, ["Greatsword"], LeatherSet);
    private static BuildSnapshot MaulLeather(string id) => Fighter(id, ["Maul"], LeatherSet);
    private static BuildSnapshot PoleaxeLeather(string id) => Fighter(id, ["Poleaxe"], LeatherSet);
    private static BuildSnapshot SteelSwordLeather(string id) => Fighter(id, ["SteelSword"], LeatherSet);
    private static BuildSnapshot SteelAxeLeather(string id) => Fighter(id, ["SteelAxe"], LeatherSet);
    private static BuildSnapshot SteelSwordDaggerLeather(string id) =>
        Fighter(id, ["SteelSword", "IronDagger"], LeatherSet);
    private static BuildSnapshot DualIronSwordLeather(string id) =>
        Fighter(id, ["IronSword", "IronSword"], LeatherSet);
    private static BuildSnapshot GreatswordChain(string id) => Fighter(id, ["Greatsword"], ChainSet);
    private static BuildSnapshot SteelSwordChain(string id) => Fighter(id, ["SteelSword"], ChainSet);
    private static BuildSnapshot GreatswordPlate(string id) => Fighter(id, ["Greatsword"], PlateSet);
    private static BuildSnapshot SteelSwordPlate(string id) => Fighter(id, ["SteelSword"], PlateSet);
    private static BuildSnapshot ChainPartialSword(string id) =>
        Fighter(id, ["IronSword"],
            ["ChainHelmet", "ChainTunic", "LeatherGorget",
             "LeatherGlove", "LeatherGlove", "LeatherGreave", "LeatherGreave"]);
    private static BuildSnapshot WdPartialBoneEater(string id) =>
        Fighter(id, ["IronSword"],
            ["WitchDoctorHelmet", "WitchDoctorTunic", "LeatherGorget",
             "LeatherGlove", "LeatherGlove", "LeatherGreave", "LeatherGreave"],
            Combine(Weapon("IronSword", "BoneEater"),
                [Sock("WitchDoctorHelmet", "ElvishLeaf"), Sock("WitchDoctorTunic", "ElvishLeaf")]));
    private static BuildSnapshot WdPartialBite(string id) =>
        Fighter(id, ["IronClaws"],
            ["WitchDoctorHelmet", "WitchDoctorTunic", "LeatherGorget",
             "LeatherGlove", "LeatherGlove", "LeatherGreave", "LeatherGreave"],
            Combine(Weapon("IronClaws", "SpidersBite"),
                [Sock("WitchDoctorHelmet", "ElvishLeaf"), Sock("WitchDoctorTunic", "SoothingVibrations")]));

    // R7-9
    private static BuildSnapshot SwordChain(string id) => Fighter(id, ["IronSword"], ChainSet);
    private static BuildSnapshot ChainBurn(string id) =>
        Fighter(id, ["IronSword"], ChainSet, Weapon("IronSword", "EverburningStone"));
    private static BuildSnapshot ChainBone(string id) =>
        Fighter(id, ["IronMace"], ChainSet, Weapon("IronMace", "BoneEater"));
    private static BuildSnapshot ChainSpider(string id) =>
        Fighter(id, ["IronSword"], ChainSet, Weapon("IronSword", "SpidersBite"));
    private static BuildSnapshot ChainDual(string id) =>
        Fighter(id, ["IronSword", "IronDagger"], ChainSet,
            Combine(Weapon("IronDagger", "FesteringWounds")));
    private static BuildSnapshot ChainRhinoLight(string id) =>
        Fighter(id, ["IronSword"], ChainSet,
            Combine(
                Weapon("IronSword", "EverburningStone"),
                [Sock("ChainHelmet", "RhinoSkin"), Sock("ChainTunic", "RhinoSkin"),
                 Sock("ChainBoot", "ElvishLeaf")]));
    private static BuildSnapshot DualDoTLeather(string id) =>
        Fighter(id, ["IronSword", "IronDagger"], LeatherSet,
            Combine(
                [Sock("IronSword", "EverburningStone"), Sock("IronDagger", "FesteringWounds")],
                LeatherMidEnchants()));
    private static BuildSnapshot WdPlainBone(string id) =>
        Fighter(id, ["IronSword"], WitchDoctorSet, Weapon("IronSword", "BoneEater"));
    private static BuildSnapshot WdBurn(string id) =>
        Fighter(id, ["IronSword"], WitchDoctorSet, Weapon("IronSword", "EverburningStone"));
    private static BuildSnapshot WdFester(string id) =>
        Fighter(id, ["IronSword"], WitchDoctorSet, Weapon("IronSword", "FesteringWounds"));
    private static BuildSnapshot WdCollarBone(string id) =>
        Fighter(id, ["IronMace"],
            ["PlagueMask", "BlessedIronCollar", "WitchDoctorTunic",
             "WitchDoctorGlove", "WitchDoctorGlove", "WitchDoctorVambrace", "WitchDoctorVambrace",
             "WitchDoctorGreave", "WitchDoctorGreave", "WitchDoctorBoot", "WitchDoctorBoot"],
            Combine(
                Weapon("IronMace", "BoneEater"),
                [Sock("BlessedIronCollar", "RhinoSkin"), Sock("WitchDoctorTunic", "ElvishLeaf")]));
    private static BuildSnapshot ClawsChainBite(string id) =>
        Fighter(id, ["IronClaws"], ChainSet, Weapon("IronClaws", "SpidersBite"));
    private static BuildSnapshot SwordPlate(string id) => Fighter(id, ["IronSword"], PlateSet);
    private static BuildSnapshot PlateBurn(string id) =>
        Fighter(id, ["IronSword"], PlateSet, Weapon("IronSword", "EverburningStone"));
    private static BuildSnapshot PlateBone(string id) =>
        Fighter(id, ["IronMace"], PlateSet, Weapon("IronMace", "BoneEater"));
    private static BuildSnapshot PlateSpider(string id) =>
        Fighter(id, ["IronSword"], PlateSet, Weapon("IronSword", "SpidersBite"));
    private static BuildSnapshot PlateDual(string id) =>
        Fighter(id, ["IronSword", "IronDagger"], PlateSet,
            Combine(Weapon("IronDagger", "FesteringWounds")));
    private static BuildSnapshot PlateRhinoLight(string id) =>
        Fighter(id, ["IronSword"], PlateSet,
            Combine(
                Weapon("IronSword", "EverburningStone"),
                [Sock("PlateHelmet", "RhinoSkin"), Sock("PlateTunic", "RhinoSkin"),
                 Sock("PlateBoot", "ElvishLeaf")]));
    private static BuildSnapshot ClawsPlateBite(string id) =>
        Fighter(id, ["IronClaws"], PlateSet, Weapon("IronClaws", "SpidersBite"));

    // R10-13
    private static BuildSnapshot ChainStackedBurn(string id) =>
        Fighter(id, ["IronSword", "IronDagger"], ChainSet,
            Combine(
                [Sock("IronSword", "EverburningStone"), Sock("IronDagger", "BoneEater")],
                ChainFullEnchants()));
    private static BuildSnapshot ChainStackedFester(string id) =>
        Fighter(id, ["IronSword", "IronDagger"], ChainSet,
            Combine(
                [Sock("IronSword", "FesteringWounds"), Sock("IronDagger", "SpidersBite")],
                ChainFullEnchants()));
    private static BuildSnapshot PlateStackedBurn(string id) =>
        Fighter(id, ["IronSword", "IronDagger"], PlateSet,
            Combine(
                [Sock("IronSword", "EverburningStone"), Sock("IronDagger", "BoneEater")],
                PlateFullEnchants()));
    private static BuildSnapshot PlateStackedFester(string id) =>
        Fighter(id, ["IronSword", "IronDagger"], PlateSet,
            Combine(
                [Sock("IronSword", "FesteringWounds"), Sock("IronDagger", "SpidersBite")],
                PlateFullEnchants()));
    private static BuildSnapshot WdHealBurn(string id) =>
        Fighter(id, ["IronSword"], WitchDoctorSet,
            Combine(Weapon("IronSword", "EverburningStone"), WdHealStack()));
    private static BuildSnapshot WdHealFester(string id) =>
        Fighter(id, ["IronSword"], WitchDoctorSet,
            Combine(Weapon("IronSword", "FesteringWounds"), WdHealStack()));
    private static BuildSnapshot WdHealBite(string id) =>
        Fighter(id, ["IronClaws"], WitchDoctorSet,
            Combine(Weapon("IronClaws", "SpidersBite"), WdHealStack()));
    private static BuildSnapshot WdHealBone(string id) =>
        Fighter(id, ["IronMace"], WitchDoctorSet,
            Combine(Weapon("IronMace", "BoneEater"), WdHealStack()));
    private static BuildSnapshot WdReflectBite(string id) =>
        Fighter(id, ["IronClaws"], WitchDoctorSet,
            Combine(Weapon("IronClaws", "SpidersBite"), WdReflectStack()));
    private static BuildSnapshot UniqueMixBurn(string id) =>
        Fighter(id, ["IronSword", "IronMace"], WdUniqueMix,
            Combine(
                [Sock("IronSword", "EverburningStone"), Sock("IronMace", "BoneEater")],
                UniqueMixEnchants()));
    private static BuildSnapshot UniqueMixBite(string id) =>
        Fighter(id, ["IronClaws", "IronDagger"], WdUniqueMix,
            Combine(
                [Sock("IronClaws", "SpidersBite"), Sock("IronDagger", "FesteringWounds")],
                UniqueMixEnchants()));
    private static BuildSnapshot FireStaffFester(string id) =>
        Fighter(id, ["FireStaff"], WitchDoctorSet,
            Combine(Weapon("FireStaff", "FesteringWounds"), WdHealStack()));
    private static BuildSnapshot FireStaffBurn(string id) =>
        Fighter(id, ["FireStaff"], WitchDoctorSet,
            Combine(Weapon("FireStaff", "EverburningStone"), WdHealStack()));
    private static BuildSnapshot StormStaffHeal(string id) =>
        Fighter(id, ["StormStaff"], WitchDoctorSet,
            Combine(Weapon("StormStaff", "EverburningStone"), WdHealStack()));
    private static BuildSnapshot EmberDaggerWd(string id) =>
        Fighter(id, ["EmberWand", "IronDagger"], WitchDoctorSet,
            Combine(Weapon("IronDagger", "FesteringWounds"), WdHealStack()));

    #endregion

    private sealed record Matchup(string Band, string Name, BuildSnapshot Attacker, BuildSnapshot Defender);

    // Set to "METAL" for the weapon tweak loop. Empty + no BALANCE_BAND env = all bands.
    private const string ForceBand = "";

    private static string? BandFilter
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(ForceBand))
            {
                return ForceBand;
            }

            return Environment.GetEnvironmentVariable("BALANCE_BAND");
        }
    }

    private static List<Matchup> Matchups()
    {
        var all = AllMatchups();
        var filter = BandFilter;
        if (string.IsNullOrWhiteSpace(filter))
        {
            return all;
        }

        return all.FindAll(m => m.Band.Equals(filter.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    private static List<Matchup> AllMatchups() =>
    [
        // --- R1-3: unarmored / partial + primitive, first iron/enchant + Early kit ---
        EraMatch("R1-3", "Club vs Club (naked)", ClubNaked("A"), ClubNaked("B")),
        EraMatch("R1-3", "Axe vs Axe (naked)", AxeNaked("A"), AxeNaked("B")),
        EraMatch("R1-3", "Spear vs Axe (naked)", SpearNaked("A"), AxeNaked("B")),
        EraMatch("R1-3", "Knife vs Knife (naked)", KnifeNaked("A"), KnifeNaked("B")),
        EraMatch("R1-3", "Hammer vs Club (naked)", HammerNaked("A"), ClubNaked("B")),
        EraMatch("R1-3", "Dual-primitive vs Axe (naked)", DualPrimitive("A"), AxeNaked("B")),
        EraMatch("R1-3", "Axe+cloth vs Axe+cloth", AxeCloth("A"), AxeCloth("B")),
        EraMatch("R1-3", "Club+cloth vs Club+cloth", ClubCloth("A"), ClubCloth("B")),
        EraMatch("R1-3", "Spear+cloth vs Axe+cloth", SpearCloth("A"), AxeCloth("B")),
        EraMatch("R1-3", "Axe+leather-core vs Axe+leather-core", AxeLeatherCore("A"), AxeLeatherCore("B")),
        EraMatch("R1-3", "Sword+Fester+cloth vs Axe+cloth", SwordFesterCloth("A"), AxeCloth("B")),
        EraMatch("R1-3", "Dagger+Fester+cloth vs Sword+cloth", DaggerFesterCloth("A"), SwordPlainCloth("B")),

        // --- R4-6: full leather, first WD/chain pieces, BoneEater + Mid kit ---
        EraMatch("R4-6", "Sword+leather vs Sword+leather", SwordLeather("A"), SwordLeather("B")),
        EraMatch("R4-6", "Mace+leather vs Sword+leather", MaceLeather("A"), SwordLeather("B")),
        EraMatch("R4-6", "IronAxe+leather vs Sword+leather", IronAxeLeather("A"), SwordLeather("B")),
        EraMatch("R4-6", "Claws+leather vs Sword+leather", ClawsLeather("A"), SwordLeather("B")),
        EraMatch("R4-6", "Dual-iron+Fester+leaf vs same", DualIronLeather("A"), DualIronLeather("B")),
        EraMatch("R4-6", "Dual-dagger+Fester+leaf vs Dual-iron", DualDaggerLeather("A"), DualIronLeather("B")),
        EraMatch("R4-6", "Sword+Fester+leather-ench vs same", SwordLeatherLeaf("A"), SwordLeatherLeaf("B")),
        EraMatch("R4-6", "Sword+Bite+BloodBath vs Sword+leather", SwordBloodBathLeather("A"), SwordLeather("B")),
        EraMatch("R4-6", "FireStaff+Fester+leather vs Sword+leather", FireStaffLeather("A"), SwordLeather("B")),
        EraMatch("R4-6", "StormStaff+leather vs Sword+leather", StormStaffLeather("A"), SwordLeather("B")),
        EraMatch("R4-6", "Ember+Dagger+leather vs Sword+leather", EmberDaggerLeather("A"), SwordLeather("B")),
        EraMatch("R4-6", "Chain-partial vs Sword+leather", ChainPartialSword("A"), SwordLeather("B")),
        EraMatch("R4-6", "WD-partial+BoneEater vs Sword+leather", WdPartialBoneEater("A"), SwordLeather("B")),
        EraMatch("R4-6", "WD-partial+Bite vs Mace+leather", WdPartialBite("A"), MaceLeather("B")),

        // --- METAL: leather, no enchants, Mid kit. Isolates iron weapon math. ---
        EraMatch("METAL", "Dagger vs Sword (leather)", DaggerLeather("A"), SwordLeather("B")),
        EraMatch("METAL", "Knuckles vs Sword (leather)", KnucklesLeather("A"), SwordLeather("B")),
        EraMatch("METAL", "Axe vs Sword (leather)", IronAxeLeather("A"), SwordLeather("B")),
        EraMatch("METAL", "Hammer vs Sword (leather)", HammerLeather("A"), SwordLeather("B")),
        EraMatch("METAL", "Mace vs Sword (leather)", MaceLeather("A"), SwordLeather("B")),
        EraMatch("METAL", "Claws vs Sword (leather)", ClawsLeather("A"), SwordLeather("B")),
        EraMatch("METAL", "Sword vs Sword (leather)", SwordLeather("A"), SwordLeather("B")),
        EraMatch("METAL", "Axe vs Axe (leather)", IronAxeLeather("A"), IronAxeLeather("B")),
        EraMatch("METAL", "Mace vs Mace (leather)", MaceLeather("A"), MaceLeather("B")),
        EraMatch("METAL", "Claws vs Claws (leather)", ClawsLeather("A"), ClawsLeather("B")),

        // --- MAGIC: leather, no enchants, Mid kit. Isolates wand/staff math. ---
        EraMatch("MAGIC", "EmberWand vs Sword (leather)", EmberWandLeather("A"), SwordLeather("B")),
        EraMatch("MAGIC", "HexWand vs Sword (leather)", HexWandLeather("A"), SwordLeather("B")),
        EraMatch("MAGIC", "FireStaff vs Sword (leather)", FireStaffPlainLeather("A"), SwordLeather("B")),
        EraMatch("MAGIC", "StormStaff vs Sword (leather)", StormStaffLeather("A"), SwordLeather("B")),
        EraMatch("MAGIC", "Dual-wand vs Sword (leather)", DualWandLeather("A"), SwordLeather("B")),
        EraMatch("MAGIC", "Ember+Dagger vs Sword (leather)", EmberDaggerLeather("A"), SwordLeather("B")),
        EraMatch("MAGIC", "FireStaff vs FireStaff (leather)", FireStaffPlainLeather("A"), FireStaffPlainLeather("B")),
        EraMatch("MAGIC", "StormStaff vs StormStaff (leather)", StormStaffLeather("A"), StormStaffLeather("B")),
        EraMatch("MAGIC", "HexWand vs EmberWand (leather)", HexWandLeather("A"), EmberWandLeather("B")),

        // --- STEEL: leather, no enchants, Mid kit. Isolates R6 martial math. ---
        EraMatch("STEEL", "Greatsword vs Sword (leather)", GreatswordLeather("A"), SwordLeather("B")),
        EraMatch("STEEL", "Maul vs Sword (leather)", MaulLeather("A"), SwordLeather("B")),
        EraMatch("STEEL", "Poleaxe vs Sword (leather)", PoleaxeLeather("A"), SwordLeather("B")),
        EraMatch("STEEL", "SteelSword vs Sword (leather)", SteelSwordLeather("A"), SwordLeather("B")),
        EraMatch("STEEL", "SteelAxe vs Sword (leather)", SteelAxeLeather("A"), SwordLeather("B")),
        EraMatch("STEEL", "Greatsword vs Maul (leather)", GreatswordLeather("A"), MaulLeather("B")),
        EraMatch("STEEL", "Greatsword vs Poleaxe (leather)", GreatswordLeather("A"), PoleaxeLeather("B")),
        EraMatch("STEEL", "Maul vs Poleaxe (leather)", MaulLeather("A"), PoleaxeLeather("B")),
        EraMatch("STEEL", "SteelSword vs SteelAxe (leather)", SteelSwordLeather("A"), SteelAxeLeather("B")),
        EraMatch("STEEL", "SteelSword+Dagger vs Greatsword (leather)", SteelSwordDaggerLeather("A"), GreatswordLeather("B")),
        EraMatch("STEEL", "Dual-IronSword vs Greatsword (leather)", DualIronSwordLeather("A"), GreatswordLeather("B")),
        EraMatch("STEEL", "Greatsword vs Sword (chain)", GreatswordChain("A"), SwordChain("B")),
        EraMatch("STEEL", "SteelSword vs Sword (chain)", SteelSwordChain("A"), SwordChain("B")),
        EraMatch("STEEL", "Greatsword vs Sword (plate)", GreatswordPlate("A"), SwordPlate("B")),
        EraMatch("STEEL", "SteelSword vs Sword (plate)", SteelSwordPlate("A"), SwordPlate("B")),

        // --- R7-9: full chain/WD, Everburning / Rhino + Late kit ---
        EraMatch("R7-9", "Sword+chain vs Sword+chain", SwordChain("A"), SwordChain("B")),
        EraMatch("R7-9", "Chain+Burn vs Chain (plain)", ChainBurn("A"), SwordChain("B")),
        EraMatch("R7-9", "Chain+Burn vs Chain+BoneEater", ChainBurn("A"), ChainBone("B")),
        EraMatch("R7-9", "Chain+Burn vs Chain+SpidersBite", ChainBurn("A"), ChainSpider("B")),
        EraMatch("R7-9", "Dual-iron+chain vs Sword+chain", ChainDual("A"), SwordChain("B")),
        EraMatch("R7-9", "Chain+Rhino-light vs Chain+Burn", ChainRhinoLight("A"), ChainBurn("B")),
        EraMatch("R7-9", "Dual-DoT+leather vs same", DualDoTLeather("A"), DualDoTLeather("B")),
        EraMatch("R7-9", "WD+BoneEater vs Sword+chain", WdPlainBone("A"), SwordChain("B")),
        EraMatch("R7-9", "WD+Burn vs Sword+chain", WdBurn("A"), SwordChain("B")),
        EraMatch("R7-9", "WD+Fester vs WD+BoneEater", WdFester("A"), WdPlainBone("B")),
        EraMatch("R7-9", "WD+collar+Bone vs Chain+Burn", WdCollarBone("A"), ChainBurn("B")),
        EraMatch("R7-9", "Claws+chain+Bite vs Dual-DoT+leather", ClawsChainBite("A"), DualDoTLeather("B")),
        EraMatch("R7-9", "Sword+plate vs Sword+plate", SwordPlate("A"), SwordPlate("B")),
        EraMatch("R7-9", "Plate+Burn vs Plate (plain)", PlateBurn("A"), SwordPlate("B")),
        EraMatch("R7-9", "Plate+Burn vs Plate+BoneEater", PlateBurn("A"), PlateBone("B")),
        EraMatch("R7-9", "Plate+Burn vs Plate+SpidersBite", PlateBurn("A"), PlateSpider("B")),
        EraMatch("R7-9", "Dual-iron+plate vs Sword+plate", PlateDual("A"), SwordPlate("B")),
        EraMatch("R7-9", "Plate+Rhino-light vs Plate+Burn", PlateRhinoLight("A"), PlateBurn("B")),
        EraMatch("R7-9", "WD+BoneEater vs Sword+plate", WdPlainBone("A"), SwordPlate("B")),
        EraMatch("R7-9", "WD+Burn vs Sword+plate", WdBurn("A"), SwordPlate("B")),
        EraMatch("R7-9", "WD+collar+Bone vs Plate+Burn", WdCollarBone("A"), PlateBurn("B")),
        EraMatch("R7-9", "Claws+plate+Bite vs Dual-DoT+leather", ClawsPlateBite("A"), DualDoTLeather("B")),
        EraMatch("R7-9", "Sword+plate vs Sword+chain", SwordPlate("A"), SwordChain("B")),

        // --- R10-13: stacked sockets + Full kit. Heal-mirrors still omitted. ---
        EraMatch("R10-13", "Chain stacked+Burn/Bone vs same", ChainStackedBurn("A"), ChainStackedBurn("B")),
        EraMatch("R10-13", "Chain stacked+Fester/Bite vs Burn/Bone", ChainStackedFester("A"), ChainStackedBurn("B")),
        EraMatch("R10-13", "WD heal+Burn vs Chain stacked", WdHealBurn("A"), ChainStackedBurn("B")),
        EraMatch("R10-13", "WD heal+Fester vs Chain stacked", WdHealFester("A"), ChainStackedBurn("B")),
        EraMatch("R10-13", "WD heal+Bite vs Chain stacked", WdHealBite("A"), ChainStackedBurn("B")),
        EraMatch("R10-13", "WD heal+Bone vs Chain stacked", WdHealBone("A"), ChainStackedBurn("B")),
        EraMatch("R10-13", "WD reflect+Bite vs Chain stacked", WdReflectBite("A"), ChainStackedBurn("B")),
        EraMatch("R10-13", "WD reflect+Bite vs WD heal+Fester", WdReflectBite("A"), WdHealFester("B")),
        EraMatch("R10-13", "Unique-mix+Burn/Bone vs Chain stacked", UniqueMixBurn("A"), ChainStackedBurn("B")),
        EraMatch("R10-13", "Unique-mix+Bite vs Chain stacked", UniqueMixBite("A"), ChainStackedBurn("B")),
        EraMatch("R10-13", "FireStaff-2H+Fester+WD vs Chain stacked", FireStaffFester("A"), ChainStackedBurn("B")),
        EraMatch("R10-13", "FireStaff-2H+Burn+WD vs Chain stacked", FireStaffBurn("A"), ChainStackedBurn("B")),
        EraMatch("R10-13", "StormStaff+Burn+WD vs Chain stacked", StormStaffHeal("A"), ChainStackedBurn("B")),
        EraMatch("R10-13", "Ember+Dagger+WD vs Chain stacked", EmberDaggerWd("A"), ChainStackedBurn("B")),
        EraMatch("R10-13", "WD heal+Burn vs WD+BoneEater (no stack)", WdHealBurn("A"), WdPlainBone("B")),
        EraMatch("R10-13", "Unique-mix vs WD+BoneEater (no stack)", UniqueMixBurn("A"), WdPlainBone("B")),
        EraMatch("R10-13", "WD heal+Bite vs Unique-mix+Burn", WdHealBite("A"), UniqueMixBurn("B")),
        EraMatch("R10-13", "FireStaff-2H+Burn vs WD+collar+Bone", FireStaffBurn("A"), WdCollarBone("B")),
        EraMatch("R10-13", "Plate stacked+Burn/Bone vs same", PlateStackedBurn("A"), PlateStackedBurn("B")),
        EraMatch("R10-13", "Plate stacked+Fester/Bite vs Burn/Bone", PlateStackedFester("A"), PlateStackedBurn("B")),
        EraMatch("R10-13", "WD heal+Burn vs Plate stacked", WdHealBurn("A"), PlateStackedBurn("B")),
        EraMatch("R10-13", "WD heal+Fester vs Plate stacked", WdHealFester("A"), PlateStackedBurn("B")),
        EraMatch("R10-13", "Unique-mix+Burn/Bone vs Plate stacked", UniqueMixBurn("A"), PlateStackedBurn("B")),
        EraMatch("R10-13", "FireStaff-2H+Burn+WD vs Plate stacked", FireStaffBurn("A"), PlateStackedBurn("B")),
        EraMatch("R10-13", "Plate stacked vs Chain stacked", PlateStackedBurn("A"), ChainStackedBurn("B")),

        // --- Cross-band leftovers keep their own era kits ---
        Split("X", "R1 axe vs R5 sword-leather", AxeNaked("A"), Early, SwordLeather("B"), Mid),
        Split("X", "R1 club vs R5 mace-leather", ClubNaked("A"), Early, MaceLeather("B"), Mid),
        Split("X", "R3 Fester-cloth vs R8 chain-plain", SwordFesterCloth("A"), Early, SwordChain("B"), Late),
        Split("X", "R5 sword-leather vs R8 chain-burn", SwordLeather("A"), Mid, ChainBurn("B"), Late),
        Split("X", "R5 leather vs R12 chain stacked", SwordLeather("A"), Mid, ChainStackedBurn("B"), Full),
        Split("X", "R6 dual-iron vs R12 WD-heal-burn", DualIronLeather("A"), Mid, WdHealBurn("B"), Full),
        Split("X", "R8 chain-plain vs R12 unique-mix", SwordChain("A"), Late, UniqueMixBurn("B"), Full),
        Split("X", "R8 WD-bone vs R12 WD-heal-burn", WdPlainBone("A"), Late, WdHealBurn("B"), Full),
        Split("X", "R5 leather vs R12 plate stacked", SwordLeather("A"), Mid, PlateStackedBurn("B"), Full),
        Split("X", "R8 plate-plain vs R12 unique-mix", SwordPlate("A"), Late, UniqueMixBurn("B"), Full),

        // --- KIT: same gear, consumable delta ---
        Split("KIT", "R1 axe Early vs axe bare", AxeNaked("A"), Early, AxeNaked("B"), Kit.None),
        Split("KIT", "R1 axe Early vs same", AxeNaked("A"), Early, AxeNaked("B"), Early),
        Split("KIT", "R5 leather Mid vs leather bare", SwordLeather("A"), Mid, SwordLeather("B"), Kit.None),
        Split("KIT", "R5 leather Burst vs Sustain", SwordLeather("A"), Burst, SwordLeather("B"), Sustain),
        Split("KIT", "R8 chain Late vs chain bare", SwordChain("A"), Late, SwordChain("B"), Kit.None),
        Split("KIT", "R8 chain Burst vs Sustain", SwordChain("A"), Burst, SwordChain("B"), Sustain),
        Split("KIT", "R12 stacked Full vs Late", ChainStackedBurn("A"), Full, ChainStackedBurn("B"), Late),
        Split("KIT", "R12 WD-heal Full vs stacked Mid", WdHealBurn("A"), Full, ChainStackedBurn("B"), Mid),
        Split("KIT", "R8 plate Late vs plate bare", SwordPlate("A"), Late, SwordPlate("B"), Kit.None),
        Split("KIT", "R12 plate stacked Full vs Late", PlateStackedBurn("A"), Full, PlateStackedBurn("B"), Late),

        // --- FOOD: same gear/med/incense, meal swap ---
        Split("FOOD", "R1 meat vs fish (axe+cloth)", AxeCloth("A"), MealOnly("CookedMeat"), AxeCloth("B"), MealOnly("CookedFish")),
        Split("FOOD", "R4 stew vs dried+corn (leather)", SwordLeather("A"), MealOnly("HeartyStew"), SwordLeather("B"), MealOnly("DriedMeat", "CookedCorn")),
        Split("FOOD", "R5 stew+meat vs honey (leather)", SwordLeather("A"), MealOnly("HeartyStew", "CookedMeat"), SwordLeather("B"), MealOnly("HoneyPot")),
        Split("FOOD", "R8 honey vs stew (chain)", SwordChain("A"), MealOnly("HoneyPot"), SwordChain("B"), MealOnly("HeartyStew")),
        Split("FOOD", "R8 honey+stew vs walnut (WD)", WdPlainBone("A"), MealOnly("HoneyPot", "HeartyStew"), WdPlainBone("B"), MealOnly("Walnut")),
        Split("FOOD", "R12 walnut vs honey (stacked)", ChainStackedBurn("A"), MealOnly("Walnut"), ChainStackedBurn("B"), MealOnly("HoneyPot")),
        Split("FOOD", "R8 honey vs stew (plate)", SwordPlate("A"), MealOnly("HoneyPot"), SwordPlate("B"), MealOnly("HeartyStew")),
        Split("FOOD", "R12 walnut vs honey (plate stacked)", PlateStackedBurn("A"), MealOnly("Walnut"), PlateStackedBurn("B"), MealOnly("HoneyPot")),

        // --- MED: same gear/meal, potion + chest swap ---
        Split("MED", "R1 MedKit+suture vs Jar only", AxeNaked("A"),
            MedOnly([], [
                Med("Suture", 3, MedicalTriggerType.PartBelowHealth, health: 0.6f),
                Med("MedKit", 3, MedicalTriggerType.PartBelowHealth)
            ]),
            AxeNaked("B"),
            MedOnly([Pot("JarOfBlood", PotionTriggerType.SelfBloodBelow, threshold: 0.25f)], [])),
        Split("MED", "R4 Mist+Balmy vs Acid flask", SwordLeather("A"),
            MedOnly([], [
                Med("MendersMist", 3, MedicalTriggerType.PartBelowHealth),
                Med("BalmyOintment", 2, MedicalTriggerType.BurningOrAcid)
            ]),
            SwordLeather("B"),
            MedOnly([Pot("AcidFlask", PotionTriggerType.AfterSeconds, after: 5)], [])),
        Split("MED", "R5 Jar+Heal vs Acid+Puss", SwordLeather("A"),
            MedOnly([
                Pot("JarOfBlood", PotionTriggerType.SelfBloodBelow, threshold: 0.2f),
                Pot("HealingPotion", PotionTriggerType.SelfPartsDamaged, threshold: 0.4f)
            ], [Med("MedKit", 2, MedicalTriggerType.PartBelowHealth)]),
            SwordLeather("B"),
            MedOnly([
                Pot("AcidFlask", PotionTriggerType.AfterSeconds, after: 5),
                Pot("PussBomb", PotionTriggerType.AfterSeconds, after: 4)
            ], [])),
        Split("MED", "R8 Mix+Cauterize vs BoneCleanse", ChainBurn("A"),
            MedOnly([], [
                Med("MendersMix", 2, MedicalTriggerType.PartBelowHealth, health: 0.4f),
                Med("Cauterize", 1, MedicalTriggerType.PartSevered, sel: MedicalTargetSelector.SeveredOrUnsealedSocket)
            ]),
            ChainBone("B"),
            MedOnly([], [Med("BoneCleanse", 2, MedicalTriggerType.PartBelowHealth)])),
        Split("MED", "R12 Serum vs Fester heal", WdHealFester("A"),
            MedOnly([], [Med("AntiNecroticSerum", 3, MedicalTriggerType.HasNecrosis)]),
            WdHealFester("B"),
            MedOnly([], [Med("MendersMix", 3, MedicalTriggerType.PartBelowHealth, health: 0.4f)])),
        Split("MED", "R12 Balmy vs FireStaff-2H Burn", ChainStackedBurn("A"),
            MedOnly([], [Med("BalmyOintment", 4, MedicalTriggerType.BurningOrAcid)]),
            FireStaffBurn("B"),
            MedOnly([], [Med("MedKit", 2, MedicalTriggerType.PartBelowHealth)])),

        // --- INC: same gear/meal, smoke swap ---
        Split("INC", "R1 Mullin vs Shade (axe)", AxeNaked("A"), StickOnly(Stick("MullinStick")), AxeNaked("B"), StickOnly(Stick("ShadeWood"))),
        Split("INC", "R5 Shade vs no incense (leather)", SwordLeather("A"), StickOnly(Stick("ShadeWood")), SwordLeather("B"), StickOnly()),
        Split("INC", "R5 Dipped vs Mullin (leather)", SwordLeather("A"), StickOnly(Stick("DippedMullinStick", 3)), SwordLeather("B"), StickOnly(Stick("MullinStick"))),
        Split("INC", "R8 Dipped vs Shade+Mullin (chain)", SwordChain("A"), StickOnly(Stick("DippedMullinStick", 3)), SwordChain("B"), StickOnly(Stick("ShadeWood"), Stick("MullinStick"))),
        Split("INC", "R8 Shade vs Dipped (WD-bone)", WdPlainBone("A"), StickOnly(Stick("ShadeWood")), WdPlainBone("B"), StickOnly(Stick("DippedMullinStick", 3))),
        Split("INC", "R12 full smoke vs Mullin (stacked)", ChainStackedBurn("A"), StickOnly(Stick("DippedMullinStick", 3), Stick("ShadeWood"), Stick("MullinStick")), ChainStackedBurn("B"), StickOnly(Stick("MullinStick"))),
        Split("INC", "R8 Dipped vs Shade+Mullin (plate)", SwordPlate("A"), StickOnly(Stick("DippedMullinStick", 3)), SwordPlate("B"), StickOnly(Stick("ShadeWood"), Stick("MullinStick"))),
        Split("INC", "R12 full smoke vs Mullin (plate stacked)", PlateStackedBurn("A"), StickOnly(Stick("DippedMullinStick", 3), Stick("ShadeWood"), Stick("MullinStick")), PlateStackedBurn("B"), StickOnly(Stick("MullinStick"))),
    ];

    [Fact]
    public void GenerateReport()
    {
        const string path = @"c:\Users\hawkk\dev-personal\wendlewind\Wendlemire\balance-report.txt";
        const string sidecarPath = @"c:\Users\hawkk\dev-personal\wendlewind\Wendlemire\balance-report.blood.jsonl";
        var sb = new StringBuilder();
        void Flush() => File.WriteAllText(path, sb.ToString());
        var done = new HashSet<string>(StringComparer.Ordinal);
        var rows = new List<(Matchup Matchup, MatchupResult Row)>();
        if (File.Exists(sidecarPath))
        {
            foreach (var line in File.ReadAllLines(sidecarPath))
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var stored = JsonSerializer.Deserialize<StoredRow>(line, SidecarJson);
                if (stored == null)
                {
                    continue;
                }

                var matchup = Matchups().FirstOrDefault(m => m.Name == stored.Name);
                if (matchup == null)
                {
                    continue;
                }

                done.Add(stored.Name);
                rows.Add((matchup, stored.Row));
            }
        }

        sb.AppendLine("=== Wendlemire Human-vs-Human Balance (12-win curve) ===");
        if (!string.IsNullOrWhiteSpace(BandFilter))
        {
            sb.AppendLine($"Band filter: {BandFilter.Trim()}");
        }

        sb.AppendLine($"Seeds/matchup: {SeedCount}   Target: {TargetMinTicks / 60}-{TargetMaxTicks / 60}s @ 60tps");
        sb.AppendLine($"Knobs this pass: CombatBalance VitalHpScale={CombatBalance.VitalHpScale} LimbHpScale={CombatBalance.LimbHpScale} ArmorK={CombatBalance.ArmorK} (ElvishLeaf diminishing, on-hit stacks free)");
        sb.AppendLine("Sever dump: currentBlood * (subtree BloodAmount / body BloodAmount) on Severe()");
        sb.AppendLine();
        AppendHumanBloodShares(sb);
        sb.AppendLine();
        sb.AppendLine($"{"Band",-7} {"Matchup",-46} {"med.s",6} {"mean",6} {"p10",5} {"p90",5} {"band%",6} {"Awin%",6} {"bleed%",7} {"organ%",7} {"sever%",7} {"loseB%",7} {"winB%",6} {"DPS",5} {"waste%",7} {"cap%",5}  topCause");
        sb.AppendLine(new string('-', 190));
        string? lastBand = null;
        foreach (var (m, row) in rows.OrderBy(r => Matchups().FindIndex(x => x.Name == r.Matchup.Name)))
        {
            if (lastBand != null && lastBand != m.Band)
            {
                sb.AppendLine();
            }

            lastBand = m.Band;
            sb.AppendLine(FormatMatchupLine(m, row));
        }

        Flush();

        var pending = Matchups().Where(m => !done.Contains(m.Name)).ToArray();
        var sidecarGate = new object();
        Parallel.ForEach(pending, new ParallelOptions
        {
            MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount)
        }, m =>
        {
            var row = RunMatchup(m);
            var line = FormatMatchupLine(m, row);
            lock (sidecarGate)
            {
                rows.Add((m, row));
                File.AppendAllText(sidecarPath, JsonSerializer.Serialize(new StoredRow(m.Band, m.Name, row), SidecarJson) + Environment.NewLine);
                _output.WriteLine(line);
            }
        });

        var ordered = rows
            .OrderBy(r => Matchups().FindIndex(x => x.Name == r.Matchup.Name))
            .ToList();
        var bandMedians = new Dictionary<string, List<double>>();
        foreach (var (m, row) in ordered)
        {
            if (!bandMedians.TryGetValue(m.Band, out var list))
            {
                bandMedians[m.Band] = list = [];
            }

            list.Add(row.MedianSeconds);
        }

        sb.Clear();
        sb.AppendLine("=== Wendlemire Human-vs-Human Balance (12-win curve) ===");
        if (!string.IsNullOrWhiteSpace(BandFilter))
        {
            sb.AppendLine($"Band filter: {BandFilter.Trim()}");
        }

        sb.AppendLine($"Seeds/matchup: {SeedCount}   Target: {TargetMinTicks / 60}-{TargetMaxTicks / 60}s @ 60tps");
        sb.AppendLine($"Knobs this pass: CombatBalance VitalHpScale={CombatBalance.VitalHpScale} LimbHpScale={CombatBalance.LimbHpScale} ArmorK={CombatBalance.ArmorK} (ElvishLeaf diminishing, on-hit stacks free)");
        sb.AppendLine("Sever dump: currentBlood * (subtree BloodAmount / body BloodAmount) on Severe()");
        sb.AppendLine();
        AppendHumanBloodShares(sb);
        sb.AppendLine();
        sb.AppendLine($"{"Band",-7} {"Matchup",-46} {"med.s",6} {"mean",6} {"p10",5} {"p90",5} {"band%",6} {"Awin%",6} {"bleed%",7} {"organ%",7} {"sever%",7} {"loseB%",7} {"winB%",6} {"DPS",5} {"waste%",7} {"cap%",5}  topCause");
        sb.AppendLine(new string('-', 190));
        lastBand = null;
        foreach (var (m, row) in ordered)
        {
            if (lastBand != null && lastBand != m.Band)
            {
                sb.AppendLine();
            }

            lastBand = m.Band;
            sb.AppendLine(FormatMatchupLine(m, row));
        }

        sb.AppendLine();
        sb.AppendLine("--- Blood & severs ---");
        sb.AppendLine($"{"Band",-7} {"Matchup",-46} {"bleed%",7} {"sever%",7} {"b|sev",6} {"b|no",5} {"loseB%",7} {"winB%",6} {"sev->s",6} {"inst%",6}");
        sb.AppendLine(new string('-', 130));
        lastBand = null;
        foreach (var (m, row) in ordered)
        {
            if (lastBand != null && lastBand != m.Band)
            {
                sb.AppendLine();
            }

            lastBand = m.Band;
            sb.AppendLine(
                $"{m.Band,-7} {m.Name,-46} {row.BleedPct,7:0} {row.SeverPct,7:0} {FmtPct(row.BleedGivenSever),6} {FmtPct(row.BleedGivenNoSever),5} " +
                $"{row.MedianLoserBlood,7:0} {row.MedianWinnerBlood,6:0} {FmtSec(row.MedianSeverToDeathSeconds),6} {row.InstantBleedPct,6:0}");
        }

        sb.AppendLine();
        sb.AppendLine("--- Band medians ---");
        foreach (var (band, values) in bandMedians)
        {
            values.Sort();
            sb.AppendLine($"  {band}: median-of-medians {values[values.Count / 2]:0.0}s   range {values[0]:0.0}-{values[^1]:0.0}s");
        }

        AppendSeverDumpVerdict(sb, ordered.Select(r => r.Row).ToList());

        Flush();
        if (File.Exists(sidecarPath))
        {
            File.Delete(sidecarPath);
        }

        _output.WriteLine(sb.ToString());
    }

    private sealed record StoredRow(string Band, string Name, MatchupResult Row);

    private static string FormatMatchupLine(Matchup m, MatchupResult row) =>
        $"{m.Band,-7} {m.Name,-46} {row.MedianSeconds,6:0.0} {row.MeanSeconds,6:0.0} {row.P10,5:0.0} {row.P90,5:0.0} " +
        $"{row.BandPct,6:0} {row.AWinPct,6:0} {row.BleedPct,7:0} {row.OrganPct,7:0} {row.SeverPct,7:0} " +
        $"{row.MedianLoserBlood,7:0} {row.MedianWinnerBlood,6:0} {row.MedianDps,5:0.0} " +
        $"{row.WastePct,7:0} {row.CapPct,5:0}  " +
        $"{Trunc(row.TopCause, 36)} ({row.TopCauseCount})";

    private sealed record MatchupResult(
        double MedianSeconds,
        double MeanSeconds,
        double P10,
        double P90,
        double BandPct,
        double AWinPct,
        double BleedPct,
        double OrganPct,
        double SeverPct,
        double BleedGivenSever,
        double BleedGivenNoSever,
        double MedianLoserBlood,
        double MedianBleedLoserBlood,
        double MedianWinnerBlood,
        double MedianSeverToDeathSeconds,
        double InstantBleedPct,
        int WithSever,
        int BleedWithSever,
        int BleedWithoutSever,
        int WithoutSever,
        int InstantBleed,
        double MedianDps,
        double WastePct,
        double CapPct,
        string TopCause,
        int TopCauseCount);

    private static MatchupResult RunMatchup(Matchup m)
    {
        var ticks = new List<int>(SeedCount);
        var dps = new List<double>(SeedCount);
        var loserBlood = new List<double>(SeedCount);
        var bleedLoserBlood = new List<double>();
        var winnerBlood = new List<double>(SeedCount);
        var severToDeath = new List<double>();
        var inBand = 0;
        var aWins = 0;
        var bleed = 0;
        var organ = 0;
        var withSever = 0;
        var withoutSever = 0;
        var bleedWithSever = 0;
        var bleedWithoutSever = 0;
        var instantBleed = 0;
        var waste = 0;
        var cap = 0;
        var causes = new Dictionary<string, int>();

        for (var seed = 1; seed <= SeedCount; seed++)
        {
            int t;
            string cause;
            double atkDps = 0;
            double loseB = 0;
            double winB = 0;
            var severs = 0;
            int? firstSeverTick = null;
            try
            {
                var sim = DuelSimulator.Simulate(m.Attacker, m.Defender, seed);
                t = sim.Result.Ticks;
                cause = sim.Result.CauseOfDeath ?? "(none/draw)";
                atkDps = sim.Analytics.Attacker.DamagePerSecond;
                severs = sim.Analytics.Attacker.Severs + sim.Analytics.Defender.Severs;
                firstSeverTick = FirstSeverTick(sim.Log);
                var aWon = sim.Result.WinnerPlayerId == "A";
                if (aWon)
                {
                    aWins++;
                    winB = sim.Analytics.Attacker.BloodPercent * 100;
                    loseB = sim.Analytics.Defender.BloodPercent * 100;
                }
                else
                {
                    winB = sim.Analytics.Defender.BloodPercent * 100;
                    loseB = sim.Analytics.Attacker.BloodPercent * 100;
                }
            }
            catch (TimeoutException)
            {
                t = CombatReplay.MaxTicks;
                cause = "(timeout/unresolved)";
            }

            ticks.Add(t);
            dps.Add(atkDps);
            loserBlood.Add(loseB);
            winnerBlood.Add(winB);
            if (t is >= TargetMinTicks and <= TargetMaxTicks)
            {
                inBand++;
            }

            if (t >= CombatCloser.StartTicks)
            {
                waste++;
            }

            if (t >= CombatCloser.HardResolveTicks)
            {
                cap++;
            }

            var isBleed = IsBleed(cause);
            if (isBleed)
            {
                bleed++;
                bleedLoserBlood.Add(loseB);
            }
            else if (IsOrgan(cause))
            {
                organ++;
            }

            if (severs > 0)
            {
                withSever++;
                if (isBleed)
                {
                    bleedWithSever++;
                }

                if (firstSeverTick is int severTick)
                {
                    severToDeath.Add((t - severTick) / 60.0);
                    if (isBleed && t - severTick <= 60)
                    {
                        instantBleed++;
                    }
                }
            }
            else
            {
                withoutSever++;
                if (isBleed)
                {
                    bleedWithoutSever++;
                }
            }

            causes[cause] = causes.GetValueOrDefault(cause) + 1;
        }

        ticks.Sort();
        dps.Sort();
        loserBlood.Sort();
        bleedLoserBlood.Sort();
        winnerBlood.Sort();
        severToDeath.Sort();
        var top = causes.OrderByDescending(kv => kv.Value).First();
        return new MatchupResult(
            MedianSeconds: ticks[ticks.Count / 2] / 60.0,
            MeanSeconds: ticks.Average() / 60.0,
            P10: ticks[(int)(ticks.Count * 0.10)] / 60.0,
            P90: ticks[(int)(ticks.Count * 0.90)] / 60.0,
            BandPct: 100.0 * inBand / SeedCount,
            AWinPct: 100.0 * aWins / SeedCount,
            BleedPct: 100.0 * bleed / SeedCount,
            OrganPct: 100.0 * organ / SeedCount,
            SeverPct: 100.0 * withSever / SeedCount,
            BleedGivenSever: withSever == 0 ? double.NaN : 100.0 * bleedWithSever / withSever,
            BleedGivenNoSever: withoutSever == 0 ? double.NaN : 100.0 * bleedWithoutSever / withoutSever,
            MedianLoserBlood: loserBlood[loserBlood.Count / 2],
            MedianBleedLoserBlood: bleedLoserBlood.Count == 0 ? double.NaN : bleedLoserBlood[bleedLoserBlood.Count / 2],
            MedianWinnerBlood: winnerBlood[winnerBlood.Count / 2],
            MedianSeverToDeathSeconds: severToDeath.Count == 0 ? double.NaN : severToDeath[severToDeath.Count / 2],
            InstantBleedPct: 100.0 * instantBleed / SeedCount,
            WithSever: withSever,
            BleedWithSever: bleedWithSever,
            BleedWithoutSever: bleedWithoutSever,
            WithoutSever: withoutSever,
            InstantBleed: instantBleed,
            MedianDps: dps[dps.Count / 2],
            WastePct: 100.0 * waste / SeedCount,
            CapPct: 100.0 * cap / SeedCount,
            TopCause: top.Key,
            TopCauseCount: top.Value);
    }

    private static void AppendHumanBloodShares(StringBuilder sb)
    {
        using var human = BodyTestHarness.Human();
        var body = human.Pawn.Body;
        var total = body.AllParts.Sum(p => p.BloodAmount);
        var maxBlood = body.MaxBlood;

        sb.AppendLine("--- Human blood shares if severed (subtree / body @ full pool) ---");
        sb.AppendLine($"  Weight total {total:0.#}   MaxBlood {maxBlood:0}");

        void Row(string name, BodyPart part)
        {
            var weight = part.GetSubtreeBloodWeight();
            sb.AppendLine($"  {name,-22} {100f * weight / total,5:0.0}%   {maxBlood * weight / total,5:0} blood");
        }

        Row("Finger", human.External(BodyPartType.Finger));
        Row("Hand+digits", human.External(BodyPartType.Hand));
        Row("Arm+hand", human.External(BodyPartType.Arm));
        Row("Foot", human.External(BodyPartType.Foot));
        Row("Leg+foot", human.External(BodyPartType.Leg));
        sb.AppendLine($"  {"Head (own, not severable)",-22} {100f * human.External(BodyPartType.Head).BloodAmount / total,5:0.0}%   {maxBlood * human.External(BodyPartType.Head).BloodAmount / total,5:0} blood");
        sb.AppendLine($"  {"Torso (own)",-22} {100f * human.External(BodyPartType.Torso).BloodAmount / total,5:0.0}%   {maxBlood * human.External(BodyPartType.Torso).BloodAmount / total,5:0} blood");
    }

    private static void AppendSeverDumpVerdict(StringBuilder sb, List<MatchupResult> rows)
    {
        var withSever = rows.Sum(r => r.WithSever);
        var withoutSever = rows.Sum(r => r.WithoutSever);
        var bleedWith = rows.Sum(r => r.BleedWithSever);
        var bleedWithout = rows.Sum(r => r.BleedWithoutSever);
        var instant = rows.Sum(r => r.InstantBleed);
        var fights = rows.Count * SeedCount;
        var bleedGivenSever = withSever == 0 ? double.NaN : 100.0 * bleedWith / withSever;
        var bleedGivenNo = withoutSever == 0 ? double.NaN : 100.0 * bleedWithout / withoutSever;
        var instantPct = fights == 0 ? 0 : 100.0 * instant / fights;
        var bleedLoseBloods = rows.Select(r => r.MedianBleedLoserBlood).Where(v => !double.IsNaN(v)).OrderBy(v => v).ToList();
        var medianLose = bleedLoseBloods.Count == 0 ? double.NaN : bleedLoseBloods[bleedLoseBloods.Count / 2];

        sb.AppendLine();
        sb.AppendLine("--- Sever dump check ---");
        sb.AppendLine($"  Fights: {fights}   with sever: {withSever} ({100.0 * withSever / fights:0}%)   bleed deaths: {bleedWith + bleedWithout} ({100.0 * (bleedWith + bleedWithout) / fights:0}%)");
        sb.AppendLine($"  bleed|sever {FmtPct(bleedGivenSever)}   bleed|no-sever {FmtPct(bleedGivenNo)}");
        sb.AppendLine($"  Instant bleed-out within 1s of first sever: {instantPct:0.0}% ({instant}/{fights})");
        sb.AppendLine($"  Median-of-medians loser blood on bleed deaths: {(double.IsNaN(medianLose) ? "n/a" : $"{medianLose:0}%")}");

        var notes = new List<string>();
        if (!double.IsNaN(bleedGivenSever) && !double.IsNaN(bleedGivenNo) && bleedGivenSever + 5 >= bleedGivenNo)
        {
            notes.Add("severs raise or hold bleed deaths (dump + stump hemorrhage matter)");
        }
        else if (!double.IsNaN(bleedGivenSever) && !double.IsNaN(bleedGivenNo))
        {
            notes.Add("WARN: fights with a sever bleed out less often than fights without — dump may be too small or severs hit after the kill");
        }

        if (instantPct <= 15)
        {
            notes.Add("dump is not a one-shot (instant bleed-out after sever is rare)");
        }
        else
        {
            notes.Add("WARN: too many bleed-outs within 1s of sever — dump may be too large");
        }

        if (double.IsNaN(medianLose))
        {
            notes.Add("no bleed deaths to judge remaining blood");
        }
        else if (medianLose <= 15)
        {
            notes.Add("bleed-death loser blood is low (pool is actually spent)");
        }
        else
        {
            notes.Add("WARN: bleed-death losers still have a lot of blood — dump or hemorrhage may be too weak");
        }

        sb.AppendLine($"  Verdict: {string.Join("; ", notes)}");
    }

    private static int? FirstSeverTick(IReadOnlyList<CombatLogEvent> log)
    {
        int? first = null;
        foreach (var ev in log)
        {
            if (ev.Kind == CombatEventKind.PartSevered)
            {
                first = first is int t ? Math.Min(t, ev.Tick) : ev.Tick;
            }

            foreach (var sub in ev.SubEffects)
            {
                if (sub.Kind == CombatEventKind.PartSevered)
                {
                    first = first is int t ? Math.Min(t, ev.Tick) : ev.Tick;
                }
            }
        }

        return first;
    }

    private static bool IsBleed(string cause) =>
        cause.Contains("Blood", StringComparison.OrdinalIgnoreCase);

    private static bool IsOrgan(string cause) =>
        cause.Contains("failed", StringComparison.OrdinalIgnoreCase)
        || cause.Contains("destroyed", StringComparison.OrdinalIgnoreCase);

    private static string Trunc(string s, int n) => s.Length <= n ? s : s[..n];

    private static string FmtPct(double value) => double.IsNaN(value) ? "  n/a" : $"{value,5:0}";

    private static string FmtSec(double value) => double.IsNaN(value) ? "   n/a" : $"{value,5:0.0}";
}
