using Wendlewind.NetCode.Contracts;
using Wendlewind.Sim.Entities.Items.Potions;

namespace Wendlewind.NetCode;

public static class BuildTemplates
{
    public static IReadOnlyList<BuildSnapshot> All { get; } =
    [
        AcidRusher(),
        TankRegen(),
        Glasscannon()
    ];

    public static BuildSnapshot Get(string buildId)
    {
        return All.FirstOrDefault(t => t.BuildId == buildId)
               ?? throw new ArgumentException($"Unknown build template '{buildId}'.");
    }

    public static BuildSnapshot AcidRusher() => new()
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
    };

    public static BuildSnapshot TankRegen() => new()
    {
        PlayerId = "template",
        BuildId = "TankRegen",
        EntityDefMonikers = ["IronSword", "JarOfBlood", "SpicedChurni"],
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
                ItemMoniker = "SpicedChurni",
                Type = PotionTriggerType.SelfPartsDamaged,
                Threshold = 0.4f,
                HealthThreshold = 0.6f
            }
        ]
    };

    public static BuildSnapshot Glasscannon() => new()
    {
        PlayerId = "template",
        BuildId = "Glasscannon",
        EntityDefMonikers = ["StrangeWitheredTwig", "SpicedChurni"],
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
    };
}
