namespace Grafted.Sim.Entities.Items.Potions;

/// <summary>
/// Properties for potion items that define their handler class.
/// </summary>
public class PotionProperties
{
    /// <summary>
    /// The type of handler class to instantiate for this potion.
    /// </summary>
    [UsedImplicitly] 
    public Type? HandlerClass;
    
    /// <summary>
    /// Creates an instance of the handler if HandlerClass is specified.
    /// </summary>
    public PotionHandler? CreateHandler()
    {
        if (HandlerClass == null) return null;
        return (PotionHandler)Activator.CreateInstance(HandlerClass)!;
    }
}
