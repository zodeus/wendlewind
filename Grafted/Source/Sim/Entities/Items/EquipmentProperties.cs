using System.Collections.Generic;

namespace Grafted.Sim.Entities.Items;

public class EquipmentProperties {
    public EquipmentType EquipmentType;
    public int MaxTrinkets = 0;
    public EquipmentSlotType? SlotUsedToEquip = EquipmentSlotType.Invalid;
}

public enum EquipmentSlotType {
    Invalid,
    BuiltIn,
    HandWeapon,
    HandArmor,
    FootWeapon,
    FootArmor,
    LegArmor,
    ArmArmor,
    HeadArmor
}