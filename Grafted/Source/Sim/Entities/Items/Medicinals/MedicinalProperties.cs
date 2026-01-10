namespace Grafted.Sim.Entities.Items.Medicinals;

public class MedicinalProperties
{
    public int DurationInTicks;
    [UsedImplicitly] public Type HandlerClass = typeof(MedicinalHandler);
    public MedicinalHandler Handler => (MedicinalHandler)Activator.CreateInstance(HandlerClass)!;
}

public abstract class MedicinalHandler
{
    /// <summary>
    /// Applies this medical item to the specified body part.
    /// </summary>
    /// <param name="item">The medical item being used</param>
    /// <param name="part">The body part to apply the item to</param>
    /// <returns>True if the item was successfully applied, false if it cannot be applied</returns>
    public abstract bool ApplyToPart(Item item, BodyPart part);
    
    /// <summary>
    /// Gets a custom info panel widget for this medical item.
    /// Override to provide a custom panel with infographics or detailed explanations.
    /// </summary>
    /// <param name="item">The medical item</param>
    /// <returns>A custom widget, or null to use the default ConsumablePanel</returns>
    public virtual Widget? GetInfoPanel(Item item) => null;
}