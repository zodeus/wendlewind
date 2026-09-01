namespace Wendlemire.Sim.Entities.Items.Potions;

/// <summary>
/// Properties for potion items that define their handler class and combat trigger.
/// </summary>
public class PotionProperties
{
    /// <summary>
    /// The type of handler class to instantiate for this potion.
    /// </summary>
    [UsedImplicitly]
    public Type? HandlerClass;

    public PotionTrigger? DefaultTrigger;

    public List<PotionTriggerType> AllowedTriggerTypes = [];

    public IReadOnlyList<PotionTriggerType> GetAllowedTriggerTypes()
    {
        return AllowedTriggerTypes.Count > 0
            ? AllowedTriggerTypes
            : Enum.GetValues<PotionTriggerType>();
    }

    /// <summary>
    /// Creates an instance of the handler if HandlerClass is specified.
    /// </summary>
    public PotionHandler? CreateHandler(ISimFactory factory)
    {
        if (HandlerClass == null) return null;
        return factory.Create<PotionHandler>(HandlerClass);
    }
}
