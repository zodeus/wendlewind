using Wendlemire.Definitions;
using Wendlemire.NetCode.Contracts;
using Wendlemire.Sim.Entities.Items;
using Wendlemire.Sim.Entities.Items.Equipment;

namespace Wendlemire.NetCode;

public static class BuildCatalog
{
    public const int BuildsPerStage = 6;
    public const string GeneratedPrefix = "gen/";

    private static readonly Dictionary<string, BuildSnapshot> Pinned = new(StringComparer.Ordinal);
    private static IReadOnlyList<BuildSnapshot> _generated = [];

    public static int CatalogSeed { get; private set; } = 384710648;

    public static IReadOnlyList<BuildSnapshot> Generated => _generated;

    public static IReadOnlyList<BuildSnapshot> All
    {
        get
        {
            EnsureGenerated(CatalogSeed);
            return [..BuildTemplates.All, .._generated];
        }
    }

    public static BuildSnapshot Get(string buildId)
    {
        if (TryGet(buildId, out var snapshot))
        {
            return snapshot;
        }

        throw new ArgumentException($"Unknown build '{buildId}'.");
    }

    public static bool TryGet(string buildId, out BuildSnapshot snapshot)
    {
        EnsureGenerated(CatalogSeed);

        var generated = _generated.FirstOrDefault(b => b.BuildId == buildId);
        if (generated != null)
        {
            snapshot = generated;
            return true;
        }

        var template = BuildTemplates.All.FirstOrDefault(b => b.BuildId == buildId);
        if (template != null)
        {
            snapshot = template;
            return true;
        }

        return Pinned.TryGetValue(buildId, out snapshot!);
    }

    public static void Pin(BuildSnapshot snapshot)
    {
        Pinned[snapshot.BuildId] = snapshot;
    }

    public static void EnsureGenerated(int seed, int perStage = BuildsPerStage)
    {
        if (_generated.Count > 0 && CatalogSeed == seed)
        {
            return;
        }

        Regenerate(seed, perStage);
    }

    public static IReadOnlyList<BuildSnapshot> Regenerate(int seed, int perStage = BuildsPerStage)
    {
        CatalogSeed = seed;
        _generated = BuildGenerator.GenerateSet(seed, perStage);
        foreach (var build in _generated)
        {
            Pinned[build.BuildId] = build;
        }

        return _generated;
    }

    public static BuildStage StageOf(BuildSnapshot snapshot) => StageOf(snapshot.BuildId);

    public static BuildStage StageOf(string buildId)
    {
        if (buildId.StartsWith(GeneratedPrefix, StringComparison.Ordinal)
            && buildId.Split('/').Length >= 2
            && Enum.TryParse<BuildStage>(buildId.Split('/')[1], ignoreCase: true, out var stage))
        {
            return stage;
        }

        return BuildStage.Signature;
    }

    public static string DisplayName(BuildSnapshot snapshot)
    {
        if (!string.IsNullOrWhiteSpace(snapshot.PawnName))
        {
            return snapshot.PawnName;
        }

        return SplitCamel(snapshot.BuildId);
    }

    public static IEnumerable<BuildSnapshot> OfStage(BuildStage stage) =>
        All.Where(b => StageOf(b) == stage);

    public static string ArmorSummary(BuildSnapshot snapshot)
    {
        var prefixes = snapshot.EntityDefMonikers
            .Select(m => DefRepository<ItemDef>.GetByMoniker(m, raiseError: false))
            .Where(d => d?.EquipmentProperties?.EquipmentType == EquipmentType.Armor
                        && d.EquipmentProperties.SlotUsedToEquip != EquipmentSlotType.Cloak)
            .Select(d => ArmorPrefix(d!.Moniker))
            .Where(p => p != null)
            .GroupBy(p => p!)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .ToList();

        return prefixes.Count == 0 ? "Unarmored" : string.Join("/", prefixes);
    }

    public sealed record LoadoutGroup(string Label, IReadOnlyList<ItemDef> Items);

    public static IReadOnlyList<LoadoutGroup> LoadoutGroups(BuildSnapshot snapshot)
    {
        var groups = new List<LoadoutGroup>();
        AddGroup(groups, "Wpn", snapshot.Weapons.Select(w => w.ItemMoniker));
        AddGroup(groups, "Arm", ArmorMonikers(snapshot));
        AddGroup(groups, "Pot", snapshot.Potions.Select(p => p.ItemMoniker));
        AddGroup(groups, "Med", snapshot.MedicalChest.Select(m => m.ItemMoniker));
        AddGroup(groups, "Inc", snapshot.Incense.Select(i => i.ItemMoniker));
        AddGroup(groups, "Food", snapshot.Meal.Length > 0 ? snapshot.Meal : snapshot.FoodBuffs);
        var trinkets = TrinketMonikers(snapshot);
        if (trinkets.Count > 0 && trinkets.Count <= 4)
        {
            AddGroup(groups, "Trk", trinkets);
        }

        return groups;
    }

    private static void AddGroup(List<LoadoutGroup> groups, string label, IEnumerable<string> monikers)
    {
        var items = new List<ItemDef>();
        foreach (var moniker in monikers)
        {
            var def = DefRepository<ItemDef>.GetByMoniker(moniker, raiseError: false);
            if (def != null && items.All(d => d.Moniker != def.Moniker))
            {
                items.Add(def);
            }
        }

        if (items.Count > 0)
        {
            groups.Add(new LoadoutGroup(label, items));
        }
    }

    private static IEnumerable<string> ArmorMonikers(BuildSnapshot snapshot)
    {
        var slots = new[]
        {
            EquipmentSlotType.HeadArmor,
            EquipmentSlotType.NeckArmor,
            EquipmentSlotType.TorsoArmor,
            EquipmentSlotType.Cloak,
            EquipmentSlotType.ArmArmor,
            EquipmentSlotType.HandArmor,
            EquipmentSlotType.LegArmor,
            EquipmentSlotType.FootArmor
        };

        return snapshot.EntityDefMonikers
            .Select(m => DefRepository<ItemDef>.GetByMoniker(m, raiseError: false))
            .Where(d => d?.EquipmentProperties?.EquipmentType == EquipmentType.Armor)
            .GroupBy(d => d!.Moniker)
            .Select(g => g.First()!)
            .OrderBy(d =>
            {
                var slot = d.EquipmentProperties?.SlotUsedToEquip ?? EquipmentSlotType.Invalid;
                var index = Array.IndexOf(slots, slot);
                return index < 0 ? 99 : index;
            })
            .Select(d => d.Moniker);
    }

    private static List<string> TrinketMonikers(BuildSnapshot snapshot) =>
        snapshot.EntityDefMonikers
            .Select(m => DefRepository<ItemDef>.GetByMoniker(m, raiseError: false))
            .Where(d => d?.ItemType == ItemType.Trinket)
            .Select(d => d!.Moniker)
            .Distinct()
            .ToList();

    private static string? ArmorPrefix(string moniker)
    {
        foreach (var prefix in new[] { "WitchDoctor", "Plate", "Chain", "Leather", "Cloth" })
        {
            if (moniker.StartsWith(prefix, StringComparison.Ordinal))
            {
                return prefix == "WitchDoctor" ? "Witch" : prefix;
            }
        }

        return moniker is "PlagueMask" ? "Plague" : moniker is "BlessedIronCollar" ? "Collar" : null;
    }

    private static string SplitCamel(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        var leaf = value.Contains('/') ? value[(value.LastIndexOf('/') + 1)..] : value;
        var chars = new List<char>(leaf.Length + 4);
        for (var i = 0; i < leaf.Length; i++)
        {
            var c = leaf[i];
            if (i > 0 && char.IsUpper(c) && !char.IsUpper(leaf[i - 1]))
            {
                chars.Add(' ');
            }

            chars.Add(c);
        }

        return new string(chars.ToArray());
    }
}
