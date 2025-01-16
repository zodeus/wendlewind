namespace Grafted.Sim.Entities.Items.Enchantments;

public class EnchantmentProperties
{
    [UsedImplicitly] public Type HandlerClass = typeof(EnchantmentHandler);
    public EnchantmentHandler Handler => (EnchantmentHandler)Activator.CreateInstance(HandlerClass)!;
}