using Wendlemire.Definitions;
using Wendlemire.NetCode.Contracts;
using Wendlemire.Sim;
using Wendlemire.Sim.Entities.Items;
using Wendlemire.Sim.Entities.Items.Equipment;
using Wendlemire.Sim.Entities.Items.Medicinals;
using Wendlemire.Sim.Entities.Items.Potions;
using Wendlemire.Sim.Entities.Pawns;

namespace Wendlemire.NetCode;

public static class BuildSnapshotFactory
{
    public static BuildSnapshot ToSnapshot(Pawn pawn, string playerId, string buildId, int seed = 0, int round = 0, int rating = 0)
    {
        var equipment = pawn.Equipment
            .Where(i => i.ItemDef.EquipmentProperties?.SlotUsedToEquip != EquipmentSlotType.BuiltIn)
            .Select(i => i.Def.Moniker)
            .ToArray();

        return new BuildSnapshot
        {
            PlayerId = playerId,
            BuildId = buildId,
            EntityDefMonikers = equipment,
            Seed = seed,
            PawnDefMoniker = pawn.PawnDef.Moniker,
            PawnName = pawn.Biography.Name,
            SubmittedAt = DateTimeOffset.UtcNow,
            Round = round,
            Rating = rating,
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
            FoodBuffs = CaptureMeal(pawn),
            Meal = CaptureMeal(pawn),
            MedicalChest = CaptureMedicalChest(pawn),
            Incense = CaptureIncense(pawn),
            Inventory = CaptureInventory(pawn),
            Skills = CaptureSkills(pawn)
        };
    }

    public static PawnDef ResolvePawnDef(BuildSnapshot snapshot)
    {
        var moniker = string.IsNullOrWhiteSpace(snapshot.PawnDefMoniker) ? "HumanA" : snapshot.PawnDefMoniker;
        return DefRepository<PawnDef>.GetByMoniker(moniker, raiseError: false)
               ?? DefRepository<PawnDef>.GetByMoniker("HumanA")!;
    }

    public static Pawn CreatePawn(GameContext context, BuildSnapshot snapshot, PawnType pawnType)
    {
        var empty = DefRepository<PawnLoadoutDef>.GetByMoniker("EmptyLoadout")
                    ?? Defs.PawnLoadouts.DefaultStarterLoadout;
        var pawn = PawnGenerator.CreatePawn(
            context,
            new PawnRequest(
                snapshot.PawnName ?? snapshot.PlayerId,
                ResolvePawnDef(snapshot),
                empty,
                pawnType));
        Apply(pawn, snapshot);
        return pawn;
    }

    public static void Apply(Pawn pawn, BuildSnapshot snapshot)
    {
        if (snapshot.Potions.Length > pawn.PotionCapacity)
        {
            pawn.PotionCapacity = snapshot.Potions.Length;
        }

        if (snapshot.Incense.Length > pawn.IncenseCapacity)
        {
            pawn.IncenseCapacity = snapshot.Incense.Length;
        }

        if (snapshot.EntityDefMonikers.Length > 0)
        {
            ReplaceLoadout(pawn, snapshot.EntityDefMonikers);
        }

        if (!string.IsNullOrEmpty(snapshot.StanceMoniker)
            && DefRepository<BodyStanceDef>.GetByMoniker(snapshot.StanceMoniker, raiseError: false) is { } stance)
        {
            pawn.Body.Stance = stance;
        }

        pawn.Equipment.SyncWeaponCombatUse();
        ApplyWeaponCombatUse(pawn, snapshot.Weapons);
        ApplyPotionTriggers(pawn, snapshot.Potions);
        ApplySockets(pawn, snapshot.Sockets);
        ApplyMeal(pawn, snapshot.Meal.Length > 0 ? snapshot.Meal : snapshot.FoodBuffs);
        ApplyMedicalChest(pawn, snapshot.MedicalChest);
        ApplyIncense(pawn, snapshot.Incense);
        ApplyInventory(pawn, snapshot.Inventory);
        ApplySkills(pawn, snapshot.Skills);
    }

    public static SkillConfig[] CaptureSkills(Pawn pawn)
    {
        return pawn.Skills
            .Where(s => s.TotalXp > 0)
            .Select(s => new SkillConfig
            {
                SkillMoniker = s.Def.Moniker,
                Level = s.Level,
                CurrentLevelXp = s.CurrentLevelXp
            })
            .ToArray();
    }

    public static void ApplySkills(Pawn pawn, SkillConfig[]? skills)
    {
        if (skills == null)
        {
            return;
        }

        foreach (var config in skills)
        {
            if (config?.SkillMoniker == null)
            {
                continue;
            }

            var def = DefRepository<SkillDef>.GetByMoniker(config.SkillMoniker, raiseError: false);
            if (def == null)
            {
                continue;
            }

            var skill = pawn.Skills.GetSkill(def);
            skill.Level = config.Level;
            skill.CurrentLevelXp = config.CurrentLevelXp;
        }
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

    private static void ApplyInventory(Pawn pawn, InventoryStackConfig[] stacks)
    {
        foreach (var stack in stacks)
        {
            var def = DefRepository<ItemDef>.GetByMoniker(stack.ItemMoniker, raiseError: false);
            if (def == null)
            {
                continue;
            }

            if (def.StackLimit <= 1)
            {
                var existingCount = pawn.Inventory.Count(i => i.Def == def && !i.IsDestroyed);
                var needed = Math.Max(stack.Amount, 1) - existingCount;
                for (var n = 0; n < needed; n++)
                {
                    pawn.Inventory.TryAdd(pawn.Context.Factory.CreateEntity<Item>(def, 1));
                }

                continue;
            }

            var amount = Math.Clamp(stack.Amount > 0 ? stack.Amount : 99, 1, def.StackLimit);
            var existing = pawn.Inventory.FirstOrDefault(i => i.Def == def && !i.IsDestroyed);
            if (existing != null)
            {
                existing.StackSize = Math.Max(existing.StackSize, amount);
                continue;
            }

            pawn.Inventory.TryAdd(pawn.Context.Factory.CreateEntity<Item>(def, amount));
        }
    }

    private static InventoryStackConfig[] CaptureInventory(Pawn pawn)
    {
        return pawn.Inventory
            .Where(i => !i.IsDestroyed)
            .GroupBy(i => i.Def.Moniker)
            .Select(g => new InventoryStackConfig
            {
                ItemMoniker = g.Key,
                Amount = g.Sum(i => i.StackSize)
            })
            .ToArray();
    }

    private static void ApplyWeaponCombatUse(Pawn pawn, WeaponConfig[] configs)
    {
        var remaining = pawn.Equipment.Weapons.Select(w => w.Item1).ToList();
        foreach (var config in configs)
        {
            var match = remaining.FirstOrDefault(weapon => weapon.Def.Moniker == config.ItemMoniker);
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

    private static void ApplyMeal(Pawn pawn, string[] foodMonikers)
    {
        pawn.MealPlan.Prune();
        foreach (var moniker in foodMonikers)
        {
            var item = FindOrCreateInventoryItem(pawn, moniker, d => d.FoodProperties != null);
            if (item != null)
            {
                pawn.MealPlan.TryAdd(item);
            }
        }
    }

    private static void ApplyMedicalChest(Pawn pawn, MedicalChestConfig[] configs)
    {
        pawn.MedicalChest.EnsureCapacity(configs.Length);
        pawn.MedicalChest.Clear();
        foreach (var config in configs)
        {
            var def = DefRepository<ItemDef>.GetByMoniker(config.ItemMoniker, raiseError: false);
            if (def == null || !MedicalChest.IsMedicalItem(def))
            {
                continue;
            }

            if (!pawn.MedicalChest.TryInstall(def, config.Charges, new MedicalTrigger
            {
                Type = config.Type,
                TargetSelector = config.TargetSelector,
                Threshold = config.Threshold,
                AfterSeconds = config.AfterSeconds,
                HealthThreshold = config.HealthThreshold > 0 ? config.HealthThreshold : 0.6f,
                TargetPartKey = config.TargetPartKey
            }))
            {
                continue;
            }

            MedicalChest.Sanitize(pawn.MedicalChest.Slots[^1]);
        }
    }

    private static void ApplyIncense(Pawn pawn, IncenseConfig[] configs)
    {
        pawn.ActiveIncense.Clear();
        foreach (var config in configs)
        {
            if (pawn.ActiveIncense.Count >= pawn.IncenseCapacity)
            {
                break;
            }

            var def = DefRepository<ItemDef>.GetByMoniker(config.ItemMoniker, raiseError: false);
            var effect = def?.IncenseProperties?.Effect?.Def;
            if (effect == null)
            {
                continue;
            }

            pawn.ActiveIncense.Add(new ActiveIncense
            {
                Def = effect,
                EncountersRemaining = config.EncountersRemaining > 0 ? config.EncountersRemaining : 1,
                SourceMoniker = config.ItemMoniker
            });
        }
    }

    private static Item? FindOrCreateInventoryItem(Pawn pawn, string moniker, Func<ItemDef, bool> isValid)
    {
        var existing = pawn.Inventory.FirstOrDefault(i => i.Def.Moniker == moniker && !i.IsDestroyed);
        if (existing != null)
        {
            return existing;
        }

        var def = DefRepository<ItemDef>.GetByMoniker(moniker, raiseError: false);
        if (def == null || !isValid(def))
        {
            return null;
        }

        var created = pawn.Context.Factory.CreateEntity<Item>(def, 1);
        pawn.Inventory.TryAdd(created);
        return created;
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

    private static string[] CaptureMeal(Pawn pawn)
    {
        pawn.MealPlan.Prune();
        return pawn.MealPlan.Items.Select(i => i.Def.Moniker).ToArray();
    }

    private static MedicalChestConfig[] CaptureMedicalChest(Pawn pawn)
    {
        pawn.MedicalChest.Prune();
        return pawn.MedicalChest.Slots.Select(s => new MedicalChestConfig
        {
            ItemMoniker = s.Def.Moniker,
            Charges = s.Charges,
            Type = s.Trigger.Type,
            TargetSelector = s.Trigger.TargetSelector,
            Threshold = s.Trigger.Threshold,
            AfterSeconds = s.Trigger.AfterSeconds,
            HealthThreshold = s.Trigger.HealthThreshold,
            TargetPartKey = s.Trigger.TargetPartKey
        }).ToArray();
    }

    private static IncenseConfig[] CaptureIncense(Pawn pawn)
    {
        return pawn.ActiveIncense
            .Where(a => a.EncountersRemaining > 0 && a.Def != null)
            .Select(a => new IncenseConfig
            {
                ItemMoniker = a.SourceMoniker ?? a.Def.Moniker,
                EncountersRemaining = a.EncountersRemaining,
                AfterSeconds = a.AfterSeconds
            })
            .ToArray();
    }
}
