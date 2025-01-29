using Grafted.Sim.Entities.Pawns.Modifiers;

namespace Grafted.Sim.Entities.Items.Enchantments;

public class EnchantmentProperties
{
    public List<EquipmentType> ValidEquipmentTypes = [];
    public List<BodyPartModifierRecord> BodyPartModifiers = new();
    [UsedImplicitly] public Type? HandlerClass;
}