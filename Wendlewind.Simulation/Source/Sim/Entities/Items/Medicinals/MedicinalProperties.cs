namespace Wendlewind.Sim.Entities.Items.Medicinals;

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
}