namespace Wendlemire.Sim.Entities.Items.Equipment;

[UsedImplicitly]
public class EquipmentProperties
{
    public EquipmentType EquipmentType;
    public int MaxEnchantments = 0;
    public EquipmentSlotType? SlotUsedToEquip = EquipmentSlotType.Invalid;
    public bool OccupiesBothHands;
    public string? ArmorSet;
    [UsedImplicitly] public Type? HandlerClass;
    public EquipmentHandler? CreateHandler(ISimFactory factory) =>
        HandlerClass != null ? factory.Create<EquipmentHandler>(HandlerClass) : null;
}
