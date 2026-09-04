namespace Wendlemire.Sim.Combat;

/// <summary>
/// Counts worn armor-family pieces and applies 2/4/6-piece bonuses.
/// Magnitudes live in <see cref="Table"/> so they can be retuned in one place.
/// </summary>
public static class SetBonuses
{
    public const string WitchDoctor = "WitchDoctor";
    public const string Plate = "Plate";
    public const string Chain = "Chain";
    public const string Leather = "Leather";

    public const int Tier2 = 2;
    public const int Tier4 = 4;
    public const int Tier6 = 6;

    public static readonly SetTier[] WitchDoctorTiers =
    [
        new(Tier2, Magic: 0.12f),
        new(Tier4, Magic: 0.24f, RegenPerTick: 0.004f),
        new(Tier6, Magic: 0.40f, RegenPerTick: 0.008f)
    ];

    public static readonly SetTier[] PlateTiers =
    [
        new(Tier2, PhysicalResistance: 6f),
        new(Tier4, AttackSpeed: 0.06f),
        new(Tier6, Strength: 0.25f)
    ];

    public static readonly IReadOnlyDictionary<string, SetTier[]> Table =
        new Dictionary<string, SetTier[]>
        {
            [WitchDoctor] = WitchDoctorTiers,
            [Plate] = PlateTiers
        };

    public readonly record struct SetTier(
        int Pieces,
        float Magic = 0f,
        float Strength = 0f,
        float PhysicalResistance = 0f,
        float AttackSpeed = 0f,
        float RegenPerTick = 0f);

    public static int CountWorn(Pawn pawn, string set)
    {
        var count = 0;
        foreach (var item in pawn.Equipment.Armor)
        {
            if (item.ItemDef.EquipmentProperties?.ArmorSet == set)
            {
                count++;
            }
        }

        return count;
    }

    public static int CountBrokenParts(Pawn pawn)
    {
        var broken = 0;
        foreach (var part in pawn.Body.AllExternalParts)
        {
            if (part.IsDestroyed)
            {
                broken++;
            }

            foreach (var socket in part.Sockets)
            {
                if (socket.IsExternal && socket.AttachedPart == null && !socket.IsSealed)
                {
                    broken++;
                }
            }
        }

        return broken;
    }

    public static SetTier? HighestTier(Pawn pawn, string set)
    {
        if (!Table.TryGetValue(set, out var tiers))
        {
            return null;
        }

        var worn = CountWorn(pawn, set);
        SetTier? best = null;
        foreach (var tier in tiers)
        {
            if (worn >= tier.Pieces)
            {
                best = tier;
            }
        }

        return best;
    }

    public static void Apply(Pawn pawn, StatDef stat, ref float value)
    {
        foreach (var set in Table.Keys)
        {
            var tier = HighestTier(pawn, set);
            if (tier == null)
            {
                continue;
            }

            if (stat == Defs.Stats.Magic)
            {
                value += tier.Value.Magic;
            }
            else if (stat == Defs.Stats.Strength)
            {
                value += tier.Value.Strength;
            }
            else if (stat == Defs.Stats.PhysicalResistance)
            {
                value += tier.Value.PhysicalResistance;
            }
            else if (stat == Defs.Stats.AttackSpeed)
            {
                value += tier.Value.AttackSpeed;
            }
        }
    }

    public static string DisplayName(string set) => set switch
    {
        WitchDoctor => "Witch Doctor",
        Plate => "Plate",
        Chain => "Chain",
        Leather => "Leather",
        _ => set
    };

    public static int MaxPieces(string set) =>
        Table.TryGetValue(set, out var tiers) && tiers.Length > 0
            ? tiers[^1].Pieces
            : 0;

    public static string DescribeTier(SetTier tier)
    {
        var parts = new List<string>();
        if (tier.Magic != 0f)
        {
            parts.Add($"{FormatBonus(tier.Magic)} Magic");
        }

        if (tier.Strength != 0f)
        {
            parts.Add($"{FormatBonus(tier.Strength)} Strength");
        }

        if (tier.PhysicalResistance != 0f)
        {
            parts.Add($"{FormatBonus(tier.PhysicalResistance)} Physical Resistance");
        }

        if (tier.AttackSpeed != 0f)
        {
            parts.Add($"{FormatBonus(tier.AttackSpeed)} Attack Speed");
        }

        if (tier.RegenPerTick > 0f)
        {
            parts.Add("regen");
        }

        return parts.Count == 0 ? "no bonus" : string.Join(", ", parts);
    }

    public static string? DescribeActive(Pawn pawn, string set)
    {
        if (!Table.ContainsKey(set))
        {
            return null;
        }

        var worn = CountWorn(pawn, set);
        if (worn <= 0)
        {
            return null;
        }

        var text = $"{DisplayName(set)} {worn}/{MaxPieces(set)}";
        var tier = HighestTier(pawn, set);
        return tier == null ? text : $"{text}: {DescribeTier(tier.Value)}";
    }

    public static string? NextTierHint(Pawn pawn, string set)
    {
        if (!Table.TryGetValue(set, out var tiers))
        {
            return null;
        }

        var worn = CountWorn(pawn, set);
        if (worn <= 0)
        {
            return null;
        }

        foreach (var tier in tiers)
        {
            if (worn < tier.Pieces)
            {
                return $"{tier.Pieces - worn} more for {tier.Pieces}-piece";
            }
        }

        return null;
    }

    private static string FormatBonus(float value)
    {
        var sign = value > 0 ? "+" : "";
        return Math.Abs(value - MathF.Round(value)) < 0.001f
            ? $"{sign}{value:0}"
            : $"{sign}{value:0.##}";
    }

    public static void Tick(Pawn pawn)
    {
        var regen = 0f;
        foreach (var set in Table.Keys)
        {
            var tier = HighestTier(pawn, set);
            if (tier is { RegenPerTick: > 0 })
            {
                regen += tier.Value.RegenPerTick;
            }
        }

        if (regen <= 0f)
        {
            return;
        }

        var heal = regen * pawn.GetStatValue(Defs.Stats.Magic);
        foreach (var part in pawn.Body.AllParts)
        {
            if (part.IsSevered)
            {
                continue;
            }

            if (part.HitPoints <= 0)
            {
                continue;
            }

            if (part.HitPoints >= part.MaxHitPoints)
            {
                continue;
            }

            part.HitPoints += heal;
        }
    }
}
