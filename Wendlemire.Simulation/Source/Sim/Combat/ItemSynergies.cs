namespace Wendlemire.Sim.Combat;

/// <summary>
/// Cross-item bonuses. Each check is local (same piece, same wound, or worn kit)
/// so realistic 2–4 piece kits matter more than nine copies of one enchant.
/// </summary>
public static class ItemSynergies
{
    public const float StripDecayDamage = 1.35f;
    public const float StripDecayChance = 1.5f;

    public const float TallowBurnDamage = 1.4f;
    public const float TallowBurnSpread = 0.40f;
    public const float TallowBurnPenetrate = 0.18f;

    public const float PoisonFesterPenetrateBonus = 0.15f;

    public const float LeafSoothingHeal = 1.35f;
    public const float SoothingLeafDuration = 1.5f;

    public const float BiteRhinoPairChance = 1.15f;
    public const float BitePerRhinoLevel = 0.08f;

    public const float CollarPairMagic = 0.12f;

    public const float SucklerBathBlood = 1.4f;
    public const float SucklerBellBlood = 1.2f;
    public const float SucklerBellHotBlood = 1.35f;
    public const int SucklerBathHealParts = 2;
    public const int SucklerBellHealParts = 1;

    public const float TwigMaskDuration = 1.4f;
    public const int TwigMaskExtraAfflictions = 1;

    public const float ThornBiteAcidDuration = 24f;
    public const float ThornBiteAcidPower = 0.85f;

    public const float CausticFireBurn = 1.3f;
    public const float CausticFireAcid = 1.35f;
    public const float CausticFireSpread = 0.35f;
    public const float CausticFirePenetrate = 0.15f;

    public static bool HostHas(Item? host, ItemDef def) =>
        host?.Enchantments?.Any(e => e.ItemDef == def) == true;

    public static Item? HostOf(Pawn pawn, Item enchantment)
    {
        foreach (var item in pawn.Equipment)
        {
            if (item.Enchantments == null)
            {
                continue;
            }

            foreach (var socketed in item.Enchantments)
            {
                if (ReferenceEquals(socketed, enchantment))
                {
                    return item;
                }
            }
        }

        return null;
    }

    public static bool PawnHasEnchant(Pawn pawn, ItemDef def)
    {
        foreach (var item in pawn.Equipment)
        {
            if (HostHas(item, def))
            {
                return true;
            }
        }

        return false;
    }

    public static bool Wears(Pawn pawn, ItemDef def) =>
        pawn.Equipment.Any(i => i.ItemDef == def);

    public static int RhinoLevel(Item? host)
    {
        if (host?.Enchantments == null)
        {
            return 1;
        }

        foreach (var socketed in host.Enchantments)
        {
            if (socketed.EnchantmentHandler is RhinoSkinHandler rhino)
            {
                return rhino.Level;
            }
        }

        return 1;
    }

    public static float BiteChanceFromRhino(int rhinoLevel)
    {
        if (rhinoLevel < 1)
        {
            rhinoLevel = 1;
        }

        return BiteRhinoPairChance + (rhinoLevel - 1) * BitePerRhinoLevel;
    }

    public static BloodyBellHandler? BloodyBell(Pawn pawn)
    {
        foreach (var trinket in pawn.Inventory.Trinkets)
        {
            if (trinket.TrinketHandler is BloodyBellHandler bell)
            {
                return bell;
            }
        }

        return null;
    }

    public static float SucklerBloodMultiplier(Pawn attacker)
    {
        var mult = 1f;
        if (PawnHasEnchant(attacker, Defs.Items.BloodBath))
        {
            mult *= SucklerBathBlood;
        }

        var bell = BloodyBell(attacker);
        if (bell == null)
        {
            return mult;
        }

        return mult * (bell.IsActive || bell.Cooldown > 0 ? SucklerBellHotBlood : SucklerBellBlood);
    }

    public static int SucklerExtraHealParts(Pawn attacker)
    {
        var extra = 0;
        if (PawnHasEnchant(attacker, Defs.Items.BloodBath))
        {
            extra += SucklerBathHealParts;
        }

        var bell = BloodyBell(attacker);
        if (bell is { IsActive: true } || bell?.Cooldown > 0)
        {
            extra += SucklerBellHealParts;
        }

        return extra;
    }

    public static BodyPart ExternalHost(BodyPart part)
    {
        var host = part;
        while (host is { IsExternal: false })
        {
            host = host.Socket?.ParentPart;
            if (host == null)
            {
                return part;
            }
        }

        return host;
    }

    public static bool ClusterHas(BodyPart part, BodyPartModifierDef def)
    {
        foreach (var candidate in Cluster(part))
        {
            if (candidate.HasModifier(def))
            {
                return true;
            }
        }

        return false;
    }

    public static IEnumerable<BodyPart> Cluster(BodyPart part)
    {
        yield return part;
        if (part.Skin != null)
        {
            yield return part.Skin;
        }

        var host = ExternalHost(part);
        if (!ReferenceEquals(host, part))
        {
            yield return host;
            if (host.Skin != null)
            {
                yield return host.Skin;
            }
        }
    }

    public static bool HasStripDoT(BodyPart part) =>
        ClusterHas(part, Defs.BodyPartModifiers.Acid)
        || ClusterHas(part, Defs.BodyPartModifiers.Burning)
        || ClusterHas(part, Defs.BodyPartModifiers.Festering);

    public static bool HostHasPoison(BodyPart part)
    {
        var host = ExternalHost(part);
        if (host.HasModifier(Defs.BodyPartModifiers.Poison))
        {
            return true;
        }

        return host.AllInternalParts.Any(p => p.HasModifier(Defs.BodyPartModifiers.Poison));
    }

    public static bool HasCausticFire(BodyPart part) =>
        ClusterHas(part, Defs.BodyPartModifiers.Burning)
        && ClusterHas(part, Defs.BodyPartModifiers.Acid);

    public static bool PartHasSoothingRegen(BodyPart part)
    {
        if (part.HasModifier(Defs.BodyPartModifiers.HealthRegeneration))
        {
            return true;
        }

        return part.AllInternalParts.Any(p => p.HasModifier(Defs.BodyPartModifiers.HealthRegeneration));
    }
}
