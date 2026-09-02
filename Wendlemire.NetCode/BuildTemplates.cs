using Wendlemire.Definitions;
using Wendlemire.NetCode.Contracts;
using Wendlemire.Sim.Entities.Items;
using Wendlemire.Sim.Entities.Items.Medicinals;
using Wendlemire.Sim.Entities.Items.Potions;

namespace Wendlemire.NetCode;

public static class BuildTemplates
{
    public static IReadOnlyList<BuildSnapshot> All =>
    [
        AcidRusher(),
        TankRegen(),
        Glasscannon(),
        LeatherSkirmisher(),
        BoneReaver(),
        IronRaider(),
        IroncladWarden(),
        WitchDoctorSage(),
        PlagueHexer(),
        DualFury(),
        HexTwig()
    ];

    public static BuildSnapshot Get(string buildId)
    {
        return All.FirstOrDefault(t => t.BuildId == buildId)
               ?? throw new ArgumentException($"Unknown build template '{buildId}'.");
    }

    public static BuildSnapshot AcidRusher() => WithFullInventory(new()
    {
        PlayerId = "template",
        BuildId = "AcidRusher",
        EntityDefMonikers = ["BoneAxe", "AcidFlask"],
        StanceMoniker = "Offensive",
        Weapons =
        [
            new WeaponConfig { ItemMoniker = "BoneAxe", UseInCombat = true }
        ],
        Potions =
        [
            new PotionConfig
            {
                ItemMoniker = "AcidFlask",
                Type = PotionTriggerType.AfterSeconds,
                AfterSeconds = 5
            }
        ]
    });

    public static BuildSnapshot TankRegen() => WithFullInventory(new()
    {
        PlayerId = "template",
        BuildId = "TankRegen",
        EntityDefMonikers = ["IronSword", "JarOfBlood", "HealingPotion"],
        StanceMoniker = "Defensive",
        Weapons =
        [
            new WeaponConfig { ItemMoniker = "IronSword", UseInCombat = true }
        ],
        Potions =
        [
            new PotionConfig
            {
                ItemMoniker = "JarOfBlood",
                Type = PotionTriggerType.SelfBloodBelow,
                Threshold = 0.2f
            },
            new PotionConfig
            {
                ItemMoniker = "HealingPotion",
                Type = PotionTriggerType.SelfPartsDamaged,
                Threshold = 0.4f,
                HealthThreshold = 0.6f
            }
        ]
    });

    public static BuildSnapshot Glasscannon() => WithFullInventory(new()
    {
        PlayerId = "template",
        BuildId = "Glasscannon",
        EntityDefMonikers = ["BloodSuckler", "StrengthPotion"],
        StanceMoniker = "Offensive",
        Weapons =
        [
            new WeaponConfig { ItemMoniker = "BloodSuckler", UseInCombat = true }
        ],
        Potions =
        [
            new PotionConfig
            {
                ItemMoniker = "StrengthPotion",
                Type = PotionTriggerType.Immediately
            }
        ]
    });

    public static BuildSnapshot LeatherSkirmisher() => WithFullInventory(new()
    {
        PlayerId = "template",
        BuildId = "LeatherSkirmisher",
        EntityDefMonikers =
        [
            "BoneAxe", "BoneKnife",
            ..LeatherSet(),
            "EvasionCloak",
            "AcidFlask", "JarOfBlood"
        ],
        StanceMoniker = "Offensive",
        Weapons =
        [
            new WeaponConfig { ItemMoniker = "BoneAxe", UseInCombat = true },
            new WeaponConfig { ItemMoniker = "BoneKnife", UseInCombat = true }
        ],
        Potions =
        [
            AfterSeconds("AcidFlask", 4),
            SelfBloodBelow("JarOfBlood", 0.25f)
        ],
        Sockets =
        [
            Socket("LeatherHelmet", "SpidersBite"),
            Socket("LeatherGlove", "BloodBath"),
            Socket("LeatherGlove", "ElvishLeaf"),
            Socket("LeatherBoot", "RhinoSkin"),
            Socket("LeatherBoot", "SoothingVibrations")
        ],
        FoodBuffs = ["CookedMeat"]
    });

    public static BuildSnapshot BoneReaver() => WithFullInventory(new()
    {
        PlayerId = "template",
        BuildId = "BoneReaver",
        EntityDefMonikers =
        [
            "BoneAxe", "BoneSpear",
            ..ClothSet(),
            "PussBomb", "AcidFlask"
        ],
        StanceMoniker = "Offensive",
        Weapons =
        [
            new WeaponConfig { ItemMoniker = "BoneAxe", UseInCombat = true },
            new WeaponConfig { ItemMoniker = "BoneSpear", UseInCombat = true }
        ],
        Potions =
        [
            AfterSeconds("PussBomb", 4),
            AfterSeconds("AcidFlask", 7)
        ],
        FoodBuffs = ["CookedFish"]
    });

    public static BuildSnapshot IronRaider() => WithFullInventory(new()
    {
        PlayerId = "template",
        BuildId = "IronRaider",
        EntityDefMonikers =
        [
            "IronSword", "IronDagger",
            ..LeatherSet(),
            "EvasionCloak",
            "StrengthPotion", "JarOfBlood"
        ],
        StanceMoniker = "Offensive",
        Weapons =
        [
            new WeaponConfig { ItemMoniker = "IronSword", UseInCombat = true },
            new WeaponConfig { ItemMoniker = "IronDagger", UseInCombat = true }
        ],
        Potions =
        [
            Immediately("StrengthPotion"),
            SelfBloodBelow("JarOfBlood", 0.2f)
        ],
        Sockets =
        [
            Socket("IronSword", "EverburningStone"),
            Socket("IronDagger", "FesteringWounds"),
            Socket("LeatherHelmet", "RhinoSkin"),
            Socket("LeatherGlove", "BloodBath"),
            Socket("LeatherGlove", "ElvishLeaf")
        ],
        FoodBuffs = ["CookedMeat", "CookedCorn"]
    });

    public static BuildSnapshot IroncladWarden() => WithFullInventory(new()
    {
        PlayerId = "template",
        BuildId = "IroncladWarden",
        EntityDefMonikers =
        [
            "IronSword", "IronMace",
            ..ChainSetNoNeck(),
            "BlessedIronCollar",
            "StrengthCloak",
            "JarOfBlood", "HealingPotion"
        ],
        StanceMoniker = "Defensive",
        Weapons =
        [
            new WeaponConfig { ItemMoniker = "IronSword", UseInCombat = true },
            new WeaponConfig { ItemMoniker = "IronMace", UseInCombat = true }
        ],
        Potions =
        [
            SelfBloodBelow("JarOfBlood", 0.2f),
            PartsDamaged("HealingPotion", 0.35f, 0.55f)
        ],
        Sockets =
        [
            Socket("IronSword", "EverburningStone"),
            Socket("IronMace", "BoneEater"),
            Socket("BlessedIronCollar", "RhinoSkin", "BloodBath", "ElvishLeaf")
        ],
        FoodBuffs = DefaultMeal,
        Meal = DefaultMeal,
        Incense = DefaultIncense,
        MedicalChest = DefaultMedical
    });

    public static BuildSnapshot WitchDoctorSage() => WithFullInventory(new()
    {
        PlayerId = "template",
        BuildId = "WitchDoctorSage",
        EntityDefMonikers =
        [
            "StrangeWitheredTwig", "IronDagger",
            ..WitchDoctorSet(),
            "RejuvenationCloak",
            "PussBomb", "BlackenedSmoke"
        ],
        StanceMoniker = "Offensive",
        Weapons =
        [
            new WeaponConfig { ItemMoniker = "StrangeWitheredTwig", UseInCombat = true },
            new WeaponConfig { ItemMoniker = "IronDagger", UseInCombat = true }
        ],
        Potions =
        [
            AfterSeconds("PussBomb", 3),
            AfterSeconds("BlackenedSmoke", 6)
        ],
        Sockets =
        [
            Socket("StrangeWitheredTwig", "SpidersBite"),
            Socket("IronDagger", "FesteringWounds"),
            ..SocketSet("WitchDoctorHelmet", "RhinoSkin", "ElvishLeaf"),
            ..SocketSet("WitchDoctorGorget", "BloodBath", "SoothingVibrations"),
            ..SocketSet("WitchDoctorTunic", "RhinoSkin", "ElvishLeaf"),
            Socket("WitchDoctorGlove", "BloodBath", "SpidersBite"),
            Socket("WitchDoctorGlove", "SoothingVibrations", "ElvishLeaf"),
            Socket("WitchDoctorVambrace", "RhinoSkin", "BloodBath"),
            Socket("WitchDoctorVambrace", "ElvishLeaf", "SoothingVibrations"),
            Socket("WitchDoctorGreave", "RhinoSkin", "BloodBath"),
            Socket("WitchDoctorGreave", "ElvishLeaf", "SoothingVibrations"),
            Socket("WitchDoctorBoot", "RhinoSkin", "SpidersBite"),
            Socket("WitchDoctorBoot", "BloodBath", "ElvishLeaf")
        ],
        FoodBuffs = DefaultMeal,
        Meal = DefaultMeal,
        Incense = DefaultIncense,
        MedicalChest = DefaultMedical
    });

    public static BuildSnapshot PlagueHexer() => WithFullInventory(new()
    {
        PlayerId = "template",
        BuildId = "PlagueHexer",
        EntityDefMonikers =
        [
            "BloodSuckler", "IronDagger",
            "PlagueMask",
            "BlessedIronCollar",
            "WitchDoctorTunic",
            ..Pair("WitchDoctorGlove"),
            ..Pair("WitchDoctorVambrace"),
            ..Pair("WitchDoctorGreave"),
            ..Pair("WitchDoctorBoot"),
            "ThornCloak",
            "AcidFlask", "PussBomb"
        ],
        StanceMoniker = "Offensive",
        Weapons =
        [
            new WeaponConfig { ItemMoniker = "BloodSuckler", UseInCombat = true },
            new WeaponConfig { ItemMoniker = "IronDagger", UseInCombat = true }
        ],
        Potions =
        [
            AfterSeconds("AcidFlask", 4),
            AfterSeconds("PussBomb", 7)
        ],
        Sockets =
        [
            Socket("IronDagger", "FesteringWounds"),
            Socket("PlagueMask", "SpidersBite", "RhinoSkin"),
            Socket("BlessedIronCollar", "BloodBath", "ElvishLeaf", "SoothingVibrations"),
            ..SocketSet("WitchDoctorTunic", "RhinoSkin", "ElvishLeaf"),
            Socket("WitchDoctorGlove", "BloodBath", "SpidersBite"),
            Socket("WitchDoctorGlove", "SoothingVibrations", "ElvishLeaf"),
            Socket("WitchDoctorVambrace", "RhinoSkin", "BloodBath"),
            Socket("WitchDoctorVambrace", "ElvishLeaf", "SpidersBite"),
            Socket("WitchDoctorGreave", "RhinoSkin", "SoothingVibrations"),
            Socket("WitchDoctorGreave", "BloodBath", "ElvishLeaf"),
            Socket("WitchDoctorBoot", "RhinoSkin", "SpidersBite"),
            Socket("WitchDoctorBoot", "BloodBath", "ElvishLeaf")
        ],
        FoodBuffs = ["GoldCapMushroom", "HeartyStew"]
    });

    public static BuildSnapshot DualFury() => WithFullInventory(new()
    {
        PlayerId = "template",
        BuildId = "DualFury",
        EntityDefMonikers =
        [
            "IronClaws", "IronAxe",
            ..LeatherSet(),
            "NinjaCloak",
            "StrengthPotion", "AntiStaticFlask"
        ],
        StanceMoniker = "Offensive",
        Weapons =
        [
            new WeaponConfig { ItemMoniker = "IronClaws", UseInCombat = true },
            new WeaponConfig { ItemMoniker = "IronAxe", UseInCombat = true }
        ],
        Potions =
        [
            Immediately("StrengthPotion"),
            AfterSeconds("AntiStaticFlask", 8)
        ],
        Sockets =
        [
            Socket("IronClaws", "SpidersBite"),
            Socket("IronAxe", "EverburningStone"),
            Socket("LeatherHelmet", "RhinoSkin"),
            Socket("LeatherGlove", "BloodBath"),
            Socket("LeatherGlove", "ElvishLeaf"),
            Socket("LeatherBoot", "SoothingVibrations"),
            Socket("LeatherBoot", "RhinoSkin")
        ],
        FoodBuffs = ["DriedMeat", "HoneyPot", "Walnut"]
    });

    public static BuildSnapshot HexTwig() => WithFullInventory(new()
    {
        PlayerId = "template",
        BuildId = "HexTwig",
        EntityDefMonikers =
        [
            "StrangeWitheredTwig",
            ..ClothSet(),
            "ClericCloak",
            "BlackenedSmoke", "Fleshify"
        ],
        StanceMoniker = "Offensive",
        Weapons =
        [
            new WeaponConfig { ItemMoniker = "StrangeWitheredTwig", UseInCombat = true }
        ],
        Potions =
        [
            AfterSeconds("BlackenedSmoke", 3),
            Immediately("Fleshify")
        ],
        Sockets =
        [
            Socket("StrangeWitheredTwig", "SpidersBite")
        ],
        FoodBuffs = ["WondrousJam", "CookedCorn"]
    });

    public const int FullInventoryStack = 99;
    public const int EnchantmentCopies = 3;

    private static InventoryStackConfig[]? _fullInventory;
    private static string[]? _allTrinkets;
    private static string[]? _allEnchantments;

    private static BuildSnapshot WithFullInventory(BuildSnapshot snapshot)
    {
        var already = snapshot.EntityDefMonikers.ToHashSet();
        return snapshot with
        {
            PawnDefMoniker = string.IsNullOrWhiteSpace(snapshot.PawnDefMoniker) ? "HumanA" : snapshot.PawnDefMoniker,
            Inventory = [..FullInventory(), ..EnchantmentInventory()],
            EntityDefMonikers = snapshot.EntityDefMonikers
                .Concat(AllTrinkets().Where(already.Add))
                .ToArray()
        };
    }

    public static string[] AllTrinkets()
    {
        if (_allTrinkets is { Length: > 0 })
        {
            return _allTrinkets;
        }

        _allTrinkets = DefRepository<ItemDef>.Defs
            .Where(d => d.ItemType == ItemType.Trinket && !string.IsNullOrEmpty(d.Moniker) && d.Moniker != "undefined")
            .Select(d => d.Moniker)
            .ToArray();
        return _allTrinkets;
    }

    public static string[] AllEnchantments()
    {
        if (_allEnchantments is { Length: > 0 })
        {
            return _allEnchantments;
        }

        _allEnchantments = DefRepository<ItemDef>.Defs
            .Where(d => d.ItemType == ItemType.Enchantment && !string.IsNullOrEmpty(d.Moniker) && d.Moniker != "undefined")
            .Select(d => d.Moniker)
            .ToArray();
        return _allEnchantments;
    }

    private static InventoryStackConfig[] EnchantmentInventory() =>
        AllEnchantments()
            .Select(moniker => new InventoryStackConfig
            {
                ItemMoniker = moniker,
                Amount = EnchantmentCopies
            })
            .ToArray();

    private static InventoryStackConfig[] FullInventory()
    {
        if (_fullInventory is { Length: > 0 })
        {
            return _fullInventory;
        }

        _fullInventory = DefRepository<ItemDef>.Defs
            .Where(d => d.StackLimit > 1 && !string.IsNullOrEmpty(d.Moniker))
            .Select(d => new InventoryStackConfig
            {
                ItemMoniker = d.Moniker,
                Amount = FullInventoryStack
            })
            .ToArray();
        return _fullInventory;
    }

    private static string[] LeatherSet() =>
    [
        "LeatherHelmet", "LeatherGorget", "LeatherTunic",
        ..Pair("LeatherGlove"), ..Pair("LeatherVambrace"),
        ..Pair("LeatherGreave"), ..Pair("LeatherBoot")
    ];

    private static string[] ClothSet() =>
    [
        "ClothHelmet", "ClothGorget", "ClothTunic",
        ..Pair("ClothGlove"), ..Pair("ClothVambrace"),
        ..Pair("ClothGreave"), ..Pair("ClothBoot")
    ];

    private static string[] ChainSetNoNeck() =>
    [
        "ChainHelmet", "ChainTunic",
        ..Pair("ChainGlove"), ..Pair("ChainVambrace"),
        ..Pair("ChainGreave"), ..Pair("ChainBoot")
    ];

    private static string[] WitchDoctorSet() =>
    [
        "WitchDoctorHelmet", "WitchDoctorGorget", "WitchDoctorTunic",
        ..Pair("WitchDoctorGlove"), ..Pair("WitchDoctorVambrace"),
        ..Pair("WitchDoctorGreave"), ..Pair("WitchDoctorBoot")
    ];

    private static string[] Pair(string moniker) => [moniker, moniker];

    private static SocketedItemConfig Socket(string item, params string[] enchants) =>
        new() { ItemMoniker = item, EnchantmentMonikers = enchants };

    private static SocketedItemConfig[] SocketSet(string item, params string[] enchants) =>
        [Socket(item, enchants)];

    private static readonly string[] DefaultMeal = ["CookedFish", "DriedMeat", "HeartyStew"];

    private static readonly IncenseConfig[] DefaultIncense =
    [
        new() { ItemMoniker = "MullinStick", EncountersRemaining = 2 },
        new() { ItemMoniker = "ShadeWood", EncountersRemaining = 2 }
    ];

    private static readonly MedicalChestConfig[] DefaultMedical =
    [
        MedPartBelow("MedKit", 0.5f, 2),
        MedPartBelow("MendersMist", 0.4f, 1),
        MedPartBelow("Suture", 0.6f, 1),
        MedCauterize()
    ];

    private static MedicalChestConfig MedPartBelow(string moniker, float health, int charges) => new()
    {
        ItemMoniker = moniker,
        Charges = charges,
        Type = MedicalTriggerType.PartBelowHealth,
        TargetSelector = MedicalTargetSelector.Auto,
        HealthThreshold = health
    };

    private static MedicalChestConfig MedBloodBelow(string moniker, float threshold, int charges) => new()
    {
        ItemMoniker = moniker,
        Charges = charges,
        Type = MedicalTriggerType.SelfBloodBelow,
        Threshold = threshold
    };

    private static MedicalChestConfig MedCauterize() => new()
    {
        ItemMoniker = "Cauterize",
        Charges = 0,
        Type = MedicalTriggerType.PartSevered,
        TargetSelector = MedicalTargetSelector.SeveredOrUnsealedSocket
    };

    private static PotionConfig Immediately(string moniker) => new()
    {
        ItemMoniker = moniker,
        Type = PotionTriggerType.Immediately
    };

    private static PotionConfig AfterSeconds(string moniker, float seconds) => new()
    {
        ItemMoniker = moniker,
        Type = PotionTriggerType.AfterSeconds,
        AfterSeconds = seconds
    };

    private static PotionConfig SelfBloodBelow(string moniker, float threshold) => new()
    {
        ItemMoniker = moniker,
        Type = PotionTriggerType.SelfBloodBelow,
        Threshold = threshold
    };

    private static PotionConfig PartsDamaged(string moniker, float threshold, float healthThreshold) => new()
    {
        ItemMoniker = moniker,
        Type = PotionTriggerType.SelfPartsDamaged,
        Threshold = threshold,
        HealthThreshold = healthThreshold
    };
}
