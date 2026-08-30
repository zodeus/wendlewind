namespace Wendlewind.Sim.Entities.Items.Medicinals;

public class MedicinalProperties
{
    public int DurationInTicks;
    public bool InfiniteUse;
    public int CooldownInTicks;
    public MedicalTrigger? DefaultTrigger;
    public List<MedicalTriggerType> AllowedTriggerTypes = [];
    public List<MedicalTargetSelector> AllowedTargetSelectors = [];
    public List<MedicalTargetPool> WatchPool = [];
    public MedicalApplyMode ApplyMode;
    [UsedImplicitly] public Type? HandlerClass;

    public IReadOnlyList<MedicalTriggerType> GetAllowedTriggerTypes()
    {
        return AllowedTriggerTypes.Count > 0
            ? AllowedTriggerTypes
            : Enum.GetValues<MedicalTriggerType>();
    }

    public IReadOnlyList<MedicalTargetSelector> GetAllowedTargetSelectors()
    {
        IEnumerable<MedicalTargetSelector> selectors = AllowedTargetSelectors.Count > 0
            ? AllowedTargetSelectors
            : Enum.GetValues<MedicalTargetSelector>();
        return selectors.Where(s => s != MedicalTargetSelector.MostDamagedPart).ToList();
    }

    public IReadOnlyList<MedicalTargetPool> GetWatchPool()
    {
        return WatchPool.Count > 0
            ? WatchPool
            : [MedicalTargetPool.External];
    }

    public bool AllowsTrigger(MedicalTriggerType type)
    {
        return GetAllowedTriggerTypes().Contains(type);
    }

    public bool AllowsTarget(MedicalTargetSelector selector)
    {
        return GetAllowedTargetSelectors().Contains(selector);
    }

    public bool Watches(MedicalTargetPool pool)
    {
        return GetWatchPool().Contains(pool);
    }

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
