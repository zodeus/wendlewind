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

        if (item.ItemDef.FoodProperties == null || pawn.MealPlan.Items.Count < MealPlan.MaxSlots)
        {
            return;
        }

        pawn.MealPlan.RemoveAt(0);
        pawn.MealPlan.TryAdd(item);
    }

    private static void ApplyIncense(Pawn pawn, Item item)
    {
        if (pawn.TryLightIncense(item, requireFlameStick: false))
        {
            return;
        }

        var effect = item.ItemDef.IncenseProperties?.Effect?.Def;
        if (effect == null || pawn.ActiveIncense.Any(a => a.Def == effect))
        {
            return;
        }

        if (pawn.ActiveIncense.Count >= IncenseProperties.MaxActive)
        {
            pawn.ExtinguishIncense(0);
        }

        pawn.TryLightIncense(item, requireFlameStick: false);
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

        foreach (var bodyPart in pawn.Body.AllExternalParts)
        {
            if (OccupiedSlotFor(bodyPart, item) is { } occupied)
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

    private static EquipmentSlotType? OccupiedSlotFor(BodyPart bodyPart, Item item)
    {
        if (!bodyPart.HasEquipmentSlots)
        {
            return null;
        }

        if (item.ItemDef.ItemType == ItemType.Potion)
        {
            foreach (var potionSlot in bodyPart.EquipmentSlots!
                         .Where(s => s is EquipmentSlotType.PotionSlot1 or EquipmentSlotType.PotionSlot2))
            {
                if (bodyPart.Equipment[potionSlot] != null)
                {
                    return potionSlot;
                }
            }

            return null;
        }

        var needed = item.ItemDef.EquipmentProperties?.SlotUsedToEquip;
        if (needed is null or EquipmentSlotType.Invalid or EquipmentSlotType.BuiltIn)
        {
            return null;
        }

        if (!bodyPart.EquipmentSlots!.Contains(needed.Value) || bodyPart.Equipment[needed.Value] == null)
        {
            return null;
        }

        return needed.Value;
    }
}
