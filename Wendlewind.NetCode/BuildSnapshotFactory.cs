using Wendlewind.Definitions;
using Wendlewind.NetCode.Contracts;
using Wendlewind.Sim.Entities.Items;
using Wendlewind.Sim.Entities.Items.Equipment;
using Wendlewind.Sim.Entities.Items.Potions;
using Wendlewind.Sim.Entities.Pawns;

namespace Wendlewind.NetCode;

public static class BuildSnapshotFactory
{
    public static BuildSnapshot ToSnapshot(Pawn pawn, string playerId, string buildId, int seed = 0)
    {
        var items = pawn.Equipment
            .Where(i => i.ItemDef.EquipmentProperties?.SlotUsedToEquip != EquipmentSlotType.BuiltIn)
            .Select(i => i.Def.Moniker)
            .Concat(pawn.Inventory.Select(i => i.Def.Moniker))
            .ToArray();

        return new BuildSnapshot
        {
            PlayerId = playerId,
            BuildId = buildId,
            EntityDefMonikers = items,
            Seed = seed,
            StanceMoniker = pawn.Body.Stance?.Moniker,
            Weapons = pawn.Equipment.Weapons
                .Select(w => new WeaponConfig
                {
                    ItemMoniker = w.Item1.Def.Moniker,
                    UseInCombat = w.Item1.UseInCombat
                })
                .ToArray(),
            Potions = pawn.Equipment.Potions
                .Select(p => new PotionConfig
                {
                    ItemMoniker = p.Def.Moniker,
                    Type = p.PotionTrigger?.Type ?? PotionTriggerType.Immediately,
                    Threshold = p.PotionTrigger?.Threshold ?? 0,
                    AfterSeconds = p.PotionTrigger?.AfterSeconds ?? 0,
                    HealthThreshold = p.PotionTrigger?.HealthThreshold ?? 0.6f
                })
                .ToArray(),
            Sockets = CaptureSockets(pawn),
            FoodBuffs = CaptureFoodBuffs(pawn)
        };
    }

    public static void Apply(Pawn pawn, BuildSnapshot snapshot)
    {
        if (snapshot.EntityDefMonikers.Length > 0)
        {
            ReplaceLoadout(pawn, snapshot.EntityDefMonikers);
        }

        if (!string.IsNullOrEmpty(snapshot.StanceMoniker)
            && DefRepository<BodyStanceDef>.GetByMoniker(snapshot.StanceMoniker, raiseError: false) is { } stance)
        {
            pawn.Body.Stance = stance;
        }

        ApplyWeaponFlags(pawn, snapshot.Weapons);
        ApplyPotionTriggers(pawn, snapshot.Potions);
        ApplySockets(pawn, snapshot.Sockets);
        ApplyFoodBuffs(pawn, snapshot.FoodBuffs);
    }

    private static void ReplaceLoadout(Pawn pawn, IEnumerable<string> monikers)
    {
        foreach (var item in pawn.Equipment.ToList())
        {
            if (item.ItemDef.EquipmentProperties?.SlotUsedToEquip == EquipmentSlotType.BuiltIn)
            {
                continue;
            }

            pawn.Equipment.UnEquip(item);
            item.Destroy();
        }

        var defs = new List<ItemDef>();
        foreach (var moniker in monikers)
        {
            var def = DefRepository<ItemDef>.GetByMoniker(moniker, raiseError: false);
            if (def != null)
            {
                defs.Add(def);
            }
        }

        PawnGenerator.RegisterEquipment(pawn, defs);
    }

    private static void ApplyWeaponFlags(Pawn pawn, WeaponConfig[] configs)
    {
        var remaining = pawn.Equipment.Weapons.Select(w => w.Item1).ToList();
        foreach (var config in configs)
        {
            var match = remaining.FirstOrDefault(w => w.Def.Moniker == config.ItemMoniker);
            if (match == null)
            {
                continue;
            }

            match.UseInCombat = config.UseInCombat;
            remaining.Remove(match);
        }
    }

    private static void ApplyPotionTriggers(Pawn pawn, PotionConfig[] configs)
    {
        var remaining = pawn.Equipment.Potions.ToList();
        foreach (var config in configs)
        {
            var match = remaining.FirstOrDefault(p => p.Def.Moniker == config.ItemMoniker);
            if (match == null)
            {
                continue;
            }

            match.PotionTrigger = new PotionTrigger
            {
                Type = config.Type,
                Threshold = config.Threshold,
                AfterSeconds = config.AfterSeconds,
                HealthThreshold = config.HealthThreshold > 0 ? config.HealthThreshold : 0.6f
            };
            remaining.Remove(match);
        }
    }

    private static void ApplySockets(Pawn pawn, SocketedItemConfig[] configs)
    {
        var remaining = pawn.Equipment.Where(i => i.Enchantments != null).ToList();
        foreach (var config in configs)
        {
            var match = remaining.FirstOrDefault(i => i.Def.Moniker == config.ItemMoniker);
            if (match?.Enchantments == null)
            {
                continue;
            }

            remaining.Remove(match);
            var max = match.Enchantments.MaxEnchantments;
            for (var i = 0; i < config.EnchantmentMonikers.Length && i < max; i++)
            {
                var def = DefRepository<ItemDef>.GetByMoniker(config.EnchantmentMonikers[i], raiseError: false);
                if (def == null)
                {
                    continue;
                }

                match.Enchantments.TryAdd(pawn.Context.Factory.CreateEntity<Item>(def, 1), i);
            }
        }
    }

    private static void ApplyFoodBuffs(Pawn pawn, string[] foodMonikers)
    {
        foreach (var moniker in foodMonikers)
        {
            var def = DefRepository<ItemDef>.GetByMoniker(moniker, raiseError: false);
            if (def?.FoodProperties == null)
            {
                continue;
            }

            pawn.TryEat(pawn.Context.Factory.CreateEntity<Item>(def, 1));
        }
    }

    private static SocketedItemConfig[] CaptureSockets(Pawn pawn)
    {
        return pawn.Equipment
            .Where(i => i.Enchantments != null)
            .Select(i => new SocketedItemConfig
            {
                ItemMoniker = i.Def.Moniker,
                EnchantmentMonikers = Enumerable.Range(0, i.Enchantments!.MaxEnchantments)
                    .Select(p => i.Enchantments.TryGetAtSocket(p)?.Def.Moniker)
                    .Where(m => m != null)
                    .ToArray()!
            })
            .Where(s => s.EnchantmentMonikers.Length > 0)
            .ToArray();
    }

    private static string[] CaptureFoodBuffs(Pawn pawn)
    {
        var active = pawn.Body.Effects
            .Where(e => !e.IsExpired)
            .Select(e => e.Def)
            .ToHashSet();
        if (active.Count == 0)
        {
            return [];
        }

        var foods = DefRepository<ItemDef>.Defs
            .Where(d => d.FoodProperties is { Effects.Count: > 0 })
            .Where(d => d.FoodProperties!.Effects.All(r => active.Contains(r.Def)))
            .OrderByDescending(d => d.FoodProperties!.Effects.Count)
            .ToList();

        var covered = new HashSet<BodyEffectDef>();
        var result = new List<string>();
        foreach (var food in foods)
        {
            var effects = food.FoodProperties!.Effects.Select(r => r.Def).ToList();
            if (effects.Any(e => !covered.Contains(e)))
            {
                result.Add(food.Moniker);
                foreach (var effect in effects)
                {
                    covered.Add(effect);
                }
            }
        }

        return result.ToArray();
    }
}
