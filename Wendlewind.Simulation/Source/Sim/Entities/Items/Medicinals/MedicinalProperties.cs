namespace Wendlewind.Sim.Entities.Items.Medicinals;

public class MedicinalProperties
{
    public int DurationInTicks;
    public bool InfiniteUse;
    public int CooldownInTicks;
    [UsedImplicitly] public Type? HandlerClass;

    public MedicinalHandler? CreateHandler(ISimFactory factory)
    {
        return HandlerClass == null ? null : factory.Create<MedicinalHandler>(HandlerClass);
    }
}

public abstract class MedicinalHandler : IHasContext, IHasRng
{
    public GameContext Context { get; set; } = null!;
    public IRng Rng { get; set; } = null!;
    /// <summary>
    /// Applies this medical item to the specified body part.
    /// </summary>
    /// <param name="item">The medical item being used</param>
    /// <param name="part">The body part to apply the item to</param>
    /// <returns>True if the item was successfully applied, false if it cannot be applied</returns>
    public abstract bool ApplyToPart(Item item, BodyPart part);
}