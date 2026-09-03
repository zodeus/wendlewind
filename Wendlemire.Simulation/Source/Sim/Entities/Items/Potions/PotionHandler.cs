namespace Wendlemire.Sim.Entities.Items.Potions;

/// <summary>
/// Base class for potion handlers providing common functionality.
/// </summary>
[UsedImplicitly(ImplicitUseTargetFlags.WithInheritors)]
public abstract class PotionHandler : IPotionHandler, IExposable, IHasContext, IHasRng
{
    public GameContext Context { get; set; } = null!;
    public IRng Rng { get; set; } = null!;
    private Item _potion = null!;
    
    public Item Potion
    {
        get => _potion;
        set => _potion = value;
    }
    
    public virtual bool CanUseInCombat => true;
    public virtual bool CanUseOutsideCombat => false;
    public virtual bool CanAutoUse => Potion.PotionTrigger != null;
    
    protected string PotionLabel => Potion.Label;
    protected ItemDef PotionDef => Potion.ItemDef;
    
    public virtual Pawn GetCombatApplicationTarget(Pawn user, Pawn? opponent) => user;

    public abstract PotionUseResult UseInCombat(Pawn user, Pawn? target = null);
    
    public virtual PotionUseResult UseOutsideCombat(Pawn user)
    {
        return PotionUseResult.Failed($"{PotionLabel} cannot be used outside of combat.");
    }
    
    public abstract string GetEffectDescription();
    
    /// <summary>
    /// Get a stat value from the potion.
    /// </summary>
    protected float GetStatValue(StatDef stat) => Potion.GetStatValue(stat);
    
    /// <summary>
    /// Get the potion duration in ticks.
    /// </summary>
    protected int GetDuration() => (int)GetStatValue(Defs.Stats.PotionDuration);
    
    public virtual void ExposeData()
    {
        ScribeReferences.Look(ref _potion!, "Potion");
    }
    
    public override string ToString() => $"{PotionLabel} Handler";
}
