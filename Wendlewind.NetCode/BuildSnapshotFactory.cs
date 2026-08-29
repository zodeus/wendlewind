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
                .ToArray()
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
}
