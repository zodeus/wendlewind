namespace Grafted.Sim.Entities.Items;

public class EquipmentProperties
{
    public EquipmentType EquipmentType;
    public int MaxTrinkets = 0;
    public int MaxEnchantments = 0;
    public EquipmentSlotType? SlotUsedToEquip = EquipmentSlotType.Invalid;
}