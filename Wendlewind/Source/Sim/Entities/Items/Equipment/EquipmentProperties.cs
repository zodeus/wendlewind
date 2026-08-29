namespace Wendlewind.Sim.Entities.Items.Equipment;

public class EquipmentProperties
{
    public EquipmentType EquipmentType;
    public int MaxEnchantments = 0;
    public EquipmentSlotType? SlotUsedToEquip = EquipmentSlotType.Invalid;
    [UsedImplicitly] public Type? HandlerClass;
    public EquipmentHandler? Handler => HandlerClass != null ? (EquipmentHandler)Activator.CreateInstance(HandlerClass)! : null;
}
