namespace Grafted.Sim.Entities.Items.Potions;

/// <summary>
/// Base class for potion handlers providing common functionality.
/// </summary>
public abstract class PotionHandler : IPotionHandler, IExposable
{
    private Item _potion = null!;
    
    public Item Potion
    {
        get => _potion;
        set => _potion = value;
    }
    
    public virtual bool CanUseInCombat => true;
    public virtual bool CanUseOutsideCombat => false;
    public virtual bool CanAutoUse => false;
    
    protected string PotionLabel => Potion.Label;
    protected ItemDef PotionDef => Potion.ItemDef;
    
    public abstract PotionUseResult UseInCombat(Pawn user, Pawn? target = null);
    
    public virtual PotionUseResult UseOutsideCombat(Pawn user)
    {
        return PotionUseResult.Failed($"{PotionLabel} cannot be used outside of combat.");
    }
    
    public abstract string GetEffectDescription();
    
    /// <summary>
    /// Attempts to automatically consume this potion during combat if conditions are met.
    /// Override in derived classes to implement auto-consume logic.
    /// </summary>
    public virtual PotionUseResult? TryAutoUse(Pawn pawn) => null;
    
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
