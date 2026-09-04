namespace Wendlemire.Sim.Arena;

public static class PurchaseAutoEquip
{
    public static void TryApply(Pawn pawn, Item item)
    {
        if (pawn == null || item == null || item.IsDestroyed)
        {
            return;
        }

        switch (item.ItemDef.ItemType)
        {
            case ItemType.Medical:
            case ItemType.Trinket:
                return;
            case ItemType.Food:
                ApplyFood(pawn, item);
                return;
            case ItemType.Incense:
                ApplyIncense(pawn, item);
                return;
            case ItemType.Potion:
            case ItemType.Equipment:
                ApplyGear(pawn, item);
                return;
        }
    }

    private static void ApplyFood(Pawn pawn, Item item)
    {
        if (pawn.MealPlan.TryAdd(item))
        {
            return;
        }

        if (item.ItemDef.FoodProperties == null || pawn.MealPlan.Items.Count < pawn.MealPlan.Capacity)
        {
            return;
        }

        pawn.MealPlan.RemoveAt(0);
        pawn.MealPlan.TryAdd(item);
    }

    private static void ApplyIncense(Pawn pawn, Item item)
    {
        if (pawn.TryLightIncense(item))
        {
            return;
        }

        var effect = item.ItemDef.IncenseProperties?.Effect?.Def;
        if (effect == null || pawn.ActiveIncense.Any(a => a.Def == effect))
        {
            return;
        }

        if (pawn.ActiveIncense.Count >= pawn.IncenseCapacity)
        {
            pawn.ExtinguishIncense(0);
        }

        pawn.TryLightIncense(item);
    }

    public static bool WouldFillEmptySlot(Pawn pawn, MerchantOffer offer)
    {
        if (pawn == null || offer == null)
        {
            return false;
        }

        if (offer.IsSet)
        {
            for (var i = 0; i < offer.SetPieces.Count; i++)
            {
                if (WouldFillEmptySlot(pawn, offer.SetPieces[i]))
                {
                    return true;
                }
            }

            return false;
        }

        return offer.ItemDef != null && WouldFillEmptySlot(pawn, offer.ItemDef);
    }

    public static bool WouldFillEmptySlot(Pawn pawn, ItemDef def)
    {
        if (pawn == null || def == null)
        {
            return false;
        }

        switch (def.ItemType)
        {
            case ItemType.Equipment:
            case ItemType.Potion:
                return HasEmptyGearSlot(pawn, def);
            case ItemType.Food:
                return def.FoodProperties != null && pawn.MealPlan.Items.Count < pawn.MealPlan.Capacity;
            case ItemType.Incense:
                return HasEmptyIncenseSlot(pawn, def);
            case ItemType.Medical:
                return MedicalChest.IsMedicalItem(def)
                       && pawn.MedicalChest.Slots.Count < pawn.MedicalChest.Capacity;
            case ItemType.Enchantment:
                return EnchantmentSocketing.EnchantmentHasCompatibleHost(pawn, def);
            default:
                return false;
        }
    }

    private static bool HasEmptyGearSlot(Pawn pawn, ItemDef def)
    {
        foreach (var bodyPart in pawn.Body.AllExternalParts)
        {
            if (bodyPart.EmptySlotFor(def) is { } empty && empty != EquipmentSlotType.BuiltIn)
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasEmptyIncenseSlot(Pawn pawn, ItemDef def)
    {
        pawn.PruneActiveIncense();
        var effect = def.IncenseProperties?.Effect?.Def;
        if (effect == null)
        {
            return false;
        }

        if (pawn.ActiveIncense.Any(a => a.Def == effect))
        {
            return false;
        }

        return pawn.ActiveIncense.Count < pawn.IncenseCapacity;
    }

    private static void ApplyGear(Pawn pawn, Item item)
    {
        if (!TryFindSlot(pawn, item, out var part, out var slot))
        {
            return;
        }

        Item toEquip;
        if (item.ItemDef.ItemType == ItemType.Potion && item.StackSize > 1)
        {
            item.StackSize--;
            toEquip = pawn.Context.Factory.CreateEntity<Item>(item.ItemDef, 1);
        }
        else
        {
            toEquip = item;
        }

        var swapped = pawn.Equipment.TryEquip(part, slot, toEquip);
        if (swapped != null)
        {
            pawn.Inventory.TryAdd(swapped);
        }
    }

    private static bool TryFindSlot(Pawn pawn, Item item, out BodyPart part, out EquipmentSlotType slot)
    {
        foreach (var bodyPart in pawn.Body.AllExternalParts)
        {
            if (bodyPart.EmptySlotFor(item) is { } empty && empty != EquipmentSlotType.BuiltIn)
            {
                part = bodyPart;
                slot = empty;
                return true;
            }
        }

        if (TryFindOccupiedSlot(pawn, item, skipSameDef: true, out part, out slot))
        {
            return true;
        }

        return TryFindOccupiedSlot(pawn, item, skipSameDef: false, out part, out slot);
    }

    private static bool TryFindOccupiedSlot(Pawn pawn, Item item, bool skipSameDef, out BodyPart part, out EquipmentSlotType slot)
    {
        foreach (var bodyPart in pawn.Body.AllExternalParts)
        {
            if (OccupiedSlotFor(bodyPart, item, skipSameDef) is { } occupied)
            {
                part = bodyPart;
                slot = occupied;
                return true;
            }
        }

        part = null!;
        slot = default;
        return false;
    }

    private static EquipmentSlotType? OccupiedSlotFor(BodyPart bodyPart, Item item, bool skipSameDef)
    {
        if (!bodyPart.HasEquipmentSlots)
        {
            return null;
        }

        if (item.ItemDef.ItemType == ItemType.Potion)
        {
            var capacity = bodyPart.Body?.Pawn.PotionCapacity ?? PotionSlots.BaseSlots;
            foreach (var potionSlot in bodyPart.EquipmentSlots!.Where(PotionSlots.IsPotionSlot))
            {
                if (!PotionSlots.IsUnlocked(potionSlot, capacity))
                {
                    continue;
                }

                var potion = bodyPart.Equipment[potionSlot];
                if (potion == null)
                {
                    continue;
                }

                if (skipSameDef && potion.Def == item.Def)
                {
                    continue;
                }

                return potionSlot;
            }

            return null;
        }

        var needed = item.ItemDef.EquipmentProperties?.SlotUsedToEquip;
        if (needed is null or EquipmentSlotType.Invalid or EquipmentSlotType.BuiltIn)
        {
            return null;
        }

        if (!bodyPart.EquipmentSlots!.Contains(needed.Value))
        {
            return null;
        }

        var equipped = bodyPart.Equipment[needed.Value];
        if (equipped == null)
        {
            return null;
        }

        if (skipSameDef && equipped.Def == item.Def)
        {
            return null;
        }

        return needed.Value;
    }
}
