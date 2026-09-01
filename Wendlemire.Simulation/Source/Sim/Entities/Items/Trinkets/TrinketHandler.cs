namespace Wendlemire.Sim.Entities.Items.Trinkets;

public abstract class TrinketHandler : IExposable, IHasContext, IHasRng
{
    public GameContext Context { get; set; } = null!;
    public IRng Rng { get; set; } = null!;
    public bool IsActive { get; protected set; }
    public int Cooldown;
    public int Kills;

    public Item Trinket = null!;

    public string Label => Trinket.Label;

    public virtual void Tick()
    {
        Cooldown = Math.Clamp(Cooldown - 1, 0, int.MaxValue);
    }

    public virtual void ExposeData()
    {
        ScribeReferences.Look(ref Trinket!, "Def");
        ScribeValues.Look(ref Cooldown, "Cooldown");
        ScribeValues.Look(ref Kills, "Kills");
    }

    public override string ToString()
    {
        return $"{Trinket.Label} Handler";
    }

    public virtual bool Activate()
    {
        if (Cooldown > 0)
        {
            return false;
        }

        IsActive = true;

        return true;
    }

    public virtual void DeActivate()
    {
        IsActive = false;
    }

    public virtual void Stop()
    {
        DeActivate();
    }

    public virtual void PostCombatAction(PostCombatReport postCombatReport)
    {
    }

    public virtual DamageRecord? PostAttackHandler(Pawn victim, DamageRequest request, DamageResponse response)
    {
        return null;
    }

    public virtual void OnClick()
    {
    }
}
