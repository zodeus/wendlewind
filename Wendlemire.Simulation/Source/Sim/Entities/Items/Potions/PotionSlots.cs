using Wendlemire.Sim.Entities.Items.Equipment;

namespace Wendlemire.Sim.Entities.Items.Potions;

public static class PotionSlots
{
    public const int BaseSlots = 2;
    public const int MaxSlots = 4;

    public static bool IsPotionSlot(EquipmentSlotType slot)
    {
        return slot is EquipmentSlotType.PotionSlot1
            or EquipmentSlotType.PotionSlot2
            or EquipmentSlotType.PotionSlot3
            or EquipmentSlotType.PotionSlot4;
    }

    public static int SlotIndex(EquipmentSlotType slot)
    {
        return slot switch
        {
            EquipmentSlotType.PotionSlot1 => 1,
            EquipmentSlotType.PotionSlot2 => 2,
            EquipmentSlotType.PotionSlot3 => 3,
            EquipmentSlotType.PotionSlot4 => 4,
            _ => 0
        };
    }

    public static bool IsUnlocked(EquipmentSlotType slot, int capacity)
    {
        var index = SlotIndex(slot);
        return index > 0 && index <= capacity;
    }
}
