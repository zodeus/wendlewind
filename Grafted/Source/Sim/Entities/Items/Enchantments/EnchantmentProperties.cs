namespace Grafted.Sim.Entities.Items.Enchantments;

public class EnchantmentProperties
{
    public List<EquipmentType> ValidEquipmentTypes = [];
    [UsedImplicitly] public Type HandlerClass = typeof(EnchantmentHandler);
}