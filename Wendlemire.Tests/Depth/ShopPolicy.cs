using Wendlemire.Definitions;
using Wendlemire.NetCode;
using Wendlemire.Sim;
using Wendlemire.Sim.Arena;
using Wendlemire.Sim.Entities.Items;
using Wendlemire.Sim.Entities.Items.Enchantments;
using Wendlemire.Sim.Entities.Items.Equipment;
using Wendlemire.Sim.Entities.Items.Medicinals;
using Wendlemire.Sim.Entities.Items.Potions;
using Wendlemire.Sim.Entities.Pawns;

namespace Wendlemire.Tests.Depth;

internal interface IShopPolicy
{
    string Name { get; }

    void Shop(ArenaRun run, GameContext ctx, IReadOnlyList<RolledShelf> shelves, Random rng);
}

internal static class ShopPolicies
{
    public static IShopPolicy Random() => new RandomPolicy();

    public static IShopPolicy Greedy() => new GreedyPolicy();

    public static IShopPolicy Planned(BuildGenerator.Archetype archetype) =>
        new PlannedPolicy(archetype, buyGear: true, buyKit: true);

    public static IShopPolicy Planned(Random rng)
    {
        var archetypes = (BuildGenerator.Archetype[])Enum.GetValues(typeof(BuildGenerator.Archetype));
        return Planned(archetypes[rng.Next(archetypes.Length)]);
    }

    public static IShopPolicy Hoarder() => new HoarderPolicy();

    public static IShopPolicy GearOnly(BuildGenerator.Archetype archetype) =>
        new PlannedPolicy(archetype, buyGear: true, buyKit: false);

    public static IShopPolicy KitOnly(BuildGenerator.Archetype archetype) =>
        new PlannedPolicy(archetype, buyGear: false, buyKit: true);

    public static IReadOnlyList<IShopPolicy> AllForReport(Random rng) =>
    [
        Random(),
        Greedy(),
        Planned(rng),
        Hoarder(),
        GearOnly(BuildGenerator.Archetype.Bruiser),
        KitOnly(BuildGenerator.Archetype.Sage)
    ];
}

internal static class PolicyPrep
{
    public static int KitReserve(BuildStage stage) => stage switch
    {
        BuildStage.Early => 70,
        BuildStage.Mid => 120,
        BuildStage.Late => 160,
        _ => 200
    };

    public static int MaxEnchantments(BuildStage stage) => stage switch
    {
        BuildStage.Early => 1,
        BuildStage.Mid => 3,
        BuildStage.Late => 4,
        _ => 5
    };

    public static bool TryBuy(ArenaRun run, GameContext ctx, MerchantOffer offer, int reserve = 0)
    {
        if (offer.Available < 1)
        {
            return false;
        }

        var cost = offer.ResolveGoldCost();
        if (cost < 0 || run.Gold - reserve < cost)
        {
            return false;
        }

        if (!run.TryBuy(ctx, offer))
        {
            return false;
        }

        offer.Available--;
        return true;
    }

    public static void ArmMedical(Pawn pawn, bool planned, Random? rng = null)
    {
        foreach (var item in pawn.Inventory.ToList())
        {
            if (item.IsDestroyed || !MedicalChest.IsMedicalItem(item.ItemDef))
            {
                continue;
            }

            var existing = pawn.MedicalChest.Slots.FirstOrDefault(s => s.Def == item.ItemDef);
            if (existing != null)
            {
                pawn.MedicalChest.AddCharge(existing);
                continue;
            }

            var trigger = planned
                ? PlannedMedicalTrigger(item.ItemDef.Moniker)
                : RandomMedicalTrigger(rng ?? new Random());
            pawn.MedicalChest.TryArm(item, trigger);
        }
    }

    public static void ConfigurePotions(Pawn pawn, bool planned, Random? rng = null)
    {
        var index = 0;
        foreach (var potion in pawn.Equipment.Potions)
        {
            potion.PotionTrigger = planned
                ? PlannedPotionTrigger(potion.Def.Moniker, index)
                : RandomPotionTrigger(rng ?? new Random(), index);
            index++;
        }
    }

    public static void SocketEnchantments(Pawn pawn, int max)
    {
        var remaining = max;
        var hosts = pawn.Equipment
            .Where(i => i.Enchantments != null)
            .OrderByDescending(i => i.ItemDef.EquipmentProperties?.EquipmentType == EquipmentType.Weapon)
            .ToList();
        foreach (var enchant in EnchantmentSocketing.UnequippedEnchantments(pawn).ToList())
        {
            if (remaining <= 0)
            {
                break;
            }

            foreach (var host in hosts)
            {
                if (EnchantmentSocketing.TrySocket(host, enchant))
                {
                    remaining--;
                    break;
                }
            }
        }
    }

    public static void SetStance(Pawn pawn, BuildGenerator.Archetype? archetype)
    {
        var moniker = archetype == BuildGenerator.Archetype.Warden ? "Defensive" : "Offensive";
        if (DefRepository<BodyStanceDef>.GetByMoniker(moniker, raiseError: false) is { } stance)
        {
            pawn.Body.Stance = stance;
        }
    }

    public static PotionTrigger PlannedPotionTrigger(string moniker, int index) => moniker switch
    {
        "StrengthPotion" or "Fleshify" => new PotionTrigger { Type = PotionTriggerType.Immediately },
        "JarOfBlood" => new PotionTrigger { Type = PotionTriggerType.SelfBloodBelow, Threshold = 0.2f },
        "HealingPotion" or "HealingFlask" or "HealingSalve" => new PotionTrigger
        {
            Type = PotionTriggerType.SelfPartsDamaged,
            Threshold = 0.4f,
            HealthThreshold = 0.55f
        },
        _ => new PotionTrigger { Type = PotionTriggerType.AfterSeconds, AfterSeconds = 4 + index * 2 }
    };

    public static MedicalTrigger PlannedMedicalTrigger(string moniker) => moniker switch
    {
        "Cauterize" => new MedicalTrigger
        {
            Type = MedicalTriggerType.PartSevered,
            TargetSelector = MedicalTargetSelector.SeveredOrUnsealedSocket
        },
        "BalmyOintment" => new MedicalTrigger { Type = MedicalTriggerType.BurningOrAcid },
        "AntiNecroticSerum" => new MedicalTrigger { Type = MedicalTriggerType.HasNecrosis },
        "Antidote" => new MedicalTrigger { Type = MedicalTriggerType.HasPoison },
        "ClotPack" => new MedicalTrigger { Type = MedicalTriggerType.SelfBloodBelow, Threshold = 0.25f },
        "Suture" or "Bandage" => new MedicalTrigger
        {
            Type = MedicalTriggerType.PartBelowHealth,
            HealthThreshold = 0.6f
        },
        "MendersMix" or "MendersMist" => new MedicalTrigger
        {
            Type = MedicalTriggerType.PartBelowHealth,
            HealthThreshold = 0.4f
        },
        _ => new MedicalTrigger { Type = MedicalTriggerType.PartBelowHealth, HealthThreshold = 0.5f }
    };

    public static PotionTrigger RandomPotionTrigger(Random rng, int index)
    {
        var types = Enum.GetValues<PotionTriggerType>();
        return new PotionTrigger
        {
            Type = types[rng.Next(types.Length)],
            Threshold = 0.15f + (float)rng.NextDouble() * 0.5f,
            AfterSeconds = 2 + rng.Next(8) + index,
            HealthThreshold = 0.4f + (float)rng.NextDouble() * 0.4f
        };
    }

    public static MedicalTrigger RandomMedicalTrigger(Random rng)
    {
        var types = Enum.GetValues<MedicalTriggerType>();
        var selectors = Enum.GetValues<MedicalTargetSelector>();
        return new MedicalTrigger
        {
            Type = types[rng.Next(types.Length)],
            TargetSelector = selectors[rng.Next(selectors.Length)],
            Threshold = 0.2f + (float)rng.NextDouble() * 0.4f,
            AfterSeconds = rng.Next(6),
            HealthThreshold = 0.3f + (float)rng.NextDouble() * 0.5f
        };
    }

    public static bool IsMagic(ItemDef def)
    {
        var type = def.WeaponProperties?.WeaponType;
        return type is WeaponType.Staff or WeaponType.FireStaff or WeaponType.StormStaff
            or WeaponType.Wand or WeaponType.EmberWand or WeaponType.HexWand or WeaponType.Branch;
    }

    public static bool IsWeapon(MerchantOffer offer) =>
        !offer.IsSet && offer.ItemDef?.EquipmentProperties?.EquipmentType == EquipmentType.Weapon;

    public static bool IsArmorPiece(MerchantOffer offer) =>
        !offer.IsSet && offer.ItemDef?.EquipmentProperties?.EquipmentType == EquipmentType.Armor
                    && offer.ItemDef.EquipmentProperties.SlotUsedToEquip != EquipmentSlotType.Cloak;

    public static bool IsEnchantment(MerchantOffer offer) =>
        offer.ItemDef?.ItemType == ItemType.Enchantment;

    public static bool IsCloak(MerchantOffer offer) =>
        offer.ItemDef?.EquipmentProperties?.SlotUsedToEquip == EquipmentSlotType.Cloak;

    public static bool PrefersWeapon(ItemDef def, BuildGenerator.Archetype archetype)
    {
        var magic = IsMagic(def);
        var twoHand = def.EquipmentProperties?.OccupiesBothHands == true;
        return archetype switch
        {
            BuildGenerator.Archetype.Sage => magic,
            BuildGenerator.Archetype.Hexer => magic || def.Moniker is "BloodSuckler" or "StrangeWitheredTwig",
            BuildGenerator.Archetype.Dualist or BuildGenerator.Archetype.Skirmisher => !twoHand,
            BuildGenerator.Archetype.Warden => !magic,
            _ => !magic
        };
    }

    public static bool PrefersSet(MerchantOffer offer, BuildGenerator.Archetype archetype)
    {
        if (!offer.IsSet)
        {
            return false;
        }

        var label = offer.SetLabel ?? "";
        var heavy = label.Contains("Chain", StringComparison.OrdinalIgnoreCase)
                    || label.Contains("Plate", StringComparison.OrdinalIgnoreCase)
                    || label.Contains("Witch", StringComparison.OrdinalIgnoreCase);
        var mystic = label.Contains("Witch", StringComparison.OrdinalIgnoreCase)
                     || label.Contains("Cloth", StringComparison.OrdinalIgnoreCase);
        var light = label.Contains("Leather", StringComparison.OrdinalIgnoreCase)
                    || label.Contains("Cloth", StringComparison.OrdinalIgnoreCase);
        return archetype switch
        {
            BuildGenerator.Archetype.Warden => heavy,
            BuildGenerator.Archetype.Skirmisher or BuildGenerator.Archetype.Dualist => light && !heavy,
            BuildGenerator.Archetype.Sage or BuildGenerator.Archetype.Hexer => mystic || light,
            _ => true
        };
    }
}

internal sealed class RandomPolicy : IShopPolicy
{
    public string Name => "Random";

    public void Shop(ArenaRun run, GameContext ctx, IReadOnlyList<RolledShelf> shelves, Random rng)
    {
        var offers = ShopStock.Flatten(shelves).ToList();
        while (true)
        {
            var affordable = offers.Where(o => o.Available > 0 && o.ResolveGoldCost() <= run.Gold).ToList();
            if (affordable.Count == 0)
            {
                break;
            }

            PolicyPrep.TryBuy(run, ctx, affordable[rng.Next(affordable.Count)]);
        }

        var pawn = ctx.PlayerPawn;
        PolicyPrep.ArmMedical(pawn, planned: false, rng);
        PolicyPrep.ConfigurePotions(pawn, planned: false, rng);
        PolicyPrep.SocketEnchantments(pawn, 8);
        PolicyPrep.SetStance(pawn, null);
    }
}

internal sealed class GreedyPolicy : IShopPolicy
{
    public string Name => "Greedy";

    public void Shop(ArenaRun run, GameContext ctx, IReadOnlyList<RolledShelf> shelves, Random rng)
    {
        var offers = ShopStock.Flatten(shelves).ToList();
        while (true)
        {
            var next = offers
                .Where(o => o.Available > 0 && o.ResolveGoldCost() <= run.Gold)
                .OrderByDescending(o => o.ResolveGoldCost())
                .ThenBy(o => o.StockKey, StringComparer.Ordinal)
                .FirstOrDefault();
            if (next == null || !PolicyPrep.TryBuy(run, ctx, next))
            {
                break;
            }
        }

        var pawn = ctx.PlayerPawn;
        PolicyPrep.ArmMedical(pawn, planned: false, rng);
        PolicyPrep.ConfigurePotions(pawn, planned: false, rng);
        PolicyPrep.SocketEnchantments(pawn, 8);
        PolicyPrep.SetStance(pawn, null);
    }
}

internal sealed class HoarderPolicy : IShopPolicy
{
    public string Name => "Hoarder";

    public void Shop(ArenaRun run, GameContext ctx, IReadOnlyList<RolledShelf> shelves, Random rng)
    {
        var pawn = ctx.PlayerPawn;
        PolicyPrep.ArmMedical(pawn, planned: true);
        PolicyPrep.ConfigurePotions(pawn, planned: true);
        PolicyPrep.SetStance(pawn, null);
    }
}

internal sealed class PlannedPolicy : IShopPolicy
{
    private readonly BuildGenerator.Archetype _archetype;
    private readonly bool _buyGear;
    private readonly bool _buyKit;

    public PlannedPolicy(BuildGenerator.Archetype archetype, bool buyGear, bool buyKit)
    {
        _archetype = archetype;
        _buyGear = buyGear;
        _buyKit = buyKit;
        Name = buyGear && buyKit
            ? $"Planned:{archetype}"
            : buyGear
                ? $"GearOnly:{archetype}"
                : $"KitOnly:{archetype}";
    }

    public string Name { get; }

    public void Shop(ArenaRun run, GameContext ctx, IReadOnlyList<RolledShelf> shelves, Random rng)
    {
        var stage = OpponentLadder.StageFor(run.UpcomingRound);
        var caps = PrepSlotUnlocks.ForRound(run.UpcomingRound);
        var reserve = _buyKit ? PolicyPrep.KitReserve(stage) : 0;
        var merchant = run.CurrentMerchant;
        var working = shelves;
        if (_buyGear && merchant != null && NeedsWeaponRefresh(ctx.PlayerPawn, working))
        {
            run.TryRefreshShelf(merchant, ShopCategory.Weapons, ShopStock.OwnedUniqueMonikers(ctx.Player));
            working = ShopStock.Restore(merchant, run.ShopShelves);
        }

        var offers = ShopStock.Flatten(working).ToList();
        if (_buyGear)
        {
            BuyPreferred(run, ctx, offers, o => PolicyPrep.PrefersSet(o, _archetype), reserve);
            BuyPreferred(run, ctx, offers, o => PolicyPrep.IsWeapon(o) && PolicyPrep.PrefersWeapon(o.ItemDef!, _archetype), reserve);
            if (!HasHeldWeapon(ctx.PlayerPawn))
            {
                BuyPreferred(run, ctx, offers, PolicyPrep.IsWeapon, reserve);
            }

            BuyPreferred(run, ctx, offers, PolicyPrep.IsCloak, reserve);
            BuyPreferred(run, ctx, offers, PolicyPrep.IsEnchantment, reserve, PolicyPrep.MaxEnchantments(stage));
            SpendLeftover(run, ctx, offers, o => PolicyPrep.IsArmorPiece(o) || PolicyPrep.IsWeapon(o), 0);
        }

        if (_buyKit)
        {
            BuyNamed(run, ctx, offers, FoodPriority(stage), caps.Food);
            BuyNamed(run, ctx, offers, PotionPriority(), caps.Potion);
            BuyNamed(run, ctx, offers, IncensePriority(stage), caps.Incense);
            BuyNamed(run, ctx, offers, MedicalPriority(), Math.Min(caps.Medical, 8));
        }

        var pawn = ctx.PlayerPawn;
        PolicyPrep.ArmMedical(pawn, planned: true);
        PolicyPrep.ConfigurePotions(pawn, planned: true);
        PolicyPrep.SocketEnchantments(pawn, PolicyPrep.MaxEnchantments(stage));
        PolicyPrep.SetStance(pawn, _archetype);
    }

    private static bool NeedsWeaponRefresh(Pawn pawn, IReadOnlyList<RolledShelf> shelves)
    {
        if (HasHeldWeapon(pawn))
        {
            return false;
        }

        return !ShopStock.Flatten(shelves).Any(PolicyPrep.IsWeapon);
    }

    private static bool HasHeldWeapon(Pawn pawn) =>
        pawn.Equipment.Weapons.Any(w =>
            w.Item1.ItemDef.EquipmentProperties?.SlotUsedToEquip != EquipmentSlotType.BuiltIn);

    private static void BuyPreferred(
        ArenaRun run,
        GameContext ctx,
        List<MerchantOffer> offers,
        Func<MerchantOffer, bool> match,
        int reserve,
        int max = 1)
    {
        var bought = 0;
        foreach (var offer in offers
                     .Where(o => o.Available > 0 && match(o))
                     .OrderByDescending(o => o.ResolveGoldCost())
                     .ThenBy(o => o.StockKey, StringComparer.Ordinal)
                     .ToList())
        {
            if (bought >= max)
            {
                break;
            }

            if (PolicyPrep.TryBuy(run, ctx, offer, reserve))
            {
                bought++;
            }
        }
    }

    private static void SpendLeftover(
        ArenaRun run,
        GameContext ctx,
        List<MerchantOffer> offers,
        Func<MerchantOffer, bool> match,
        int reserve)
    {
        while (true)
        {
            var next = offers
                .Where(o => o.Available > 0 && match(o) && o.ResolveGoldCost() <= run.Gold - reserve)
                .OrderByDescending(o => o.ResolveGoldCost())
                .FirstOrDefault();
            if (next == null || !PolicyPrep.TryBuy(run, ctx, next, reserve))
            {
                return;
            }
        }
    }

    private static void BuyNamed(
        ArenaRun run,
        GameContext ctx,
        List<MerchantOffer> offers,
        IEnumerable<string> monikers,
        int cap)
    {
        var bought = 0;
        foreach (var moniker in monikers)
        {
            if (bought >= cap)
            {
                break;
            }

            var offer = offers.FirstOrDefault(o =>
                o.Available > 0 && !o.IsSet && o.ItemDef?.Moniker == moniker);
            if (offer != null && PolicyPrep.TryBuy(run, ctx, offer))
            {
                bought++;
            }
        }
    }

    private static IEnumerable<string> FoodPriority(BuildStage stage) => stage switch
    {
        BuildStage.Early => ["CookedMeat", "CookedFish"],
        BuildStage.Mid => ["HeartyStew", "DriedMeat", "CookedCorn"],
        BuildStage.Late => ["HeartyStew", "HoneyPot", "DriedMeat"],
        _ => ["HeartyStew", "Walnut", "HoneyPot", "WondrousJam"]
    };

    private static IEnumerable<string> PotionPriority() =>
        ["JarOfBlood", "HealingPotion", "StrengthPotion", "AcidFlask"];

    private static IEnumerable<string> IncensePriority(BuildStage stage) => stage switch
    {
        BuildStage.Early => ["MullinStick"],
        BuildStage.Mid => ["ShadeWood", "MullinStick"],
        BuildStage.Late => ["DippedMullinStick", "ShadeWood", "MullinStick"],
        _ => ["DippedMullinStick", "WitchWood", "MullinStick"]
    };

    private static IEnumerable<string> MedicalPriority() =>
    [
        "MedKit", "Suture", "MendersMist", "BalmyOintment", "Cauterize",
        "MendersMix", "Antidote", "ClotPack", "BoneCleanse", "Bandage"
    ];
}
