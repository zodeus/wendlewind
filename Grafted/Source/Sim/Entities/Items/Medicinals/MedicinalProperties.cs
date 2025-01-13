namespace Grafted.Sim.Entities.Items.Medicinals;

public class MedicinalProperties
{
    public int DurationInTicks;
    [UsedImplicitly] public Type HandlerClass = typeof(MedicinalHandler);
    public MedicinalHandler Handler => (MedicinalHandler)Activator.CreateInstance(HandlerClass)!;
}

public abstract class MedicinalHandler
{
    public abstract bool ApplyToPart(Item item, BodyPart part);
}