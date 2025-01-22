namespace Grafted.Sim.Entities.Items.Trinkets;

public abstract class TrinketHandler : IExposable
{
    public bool IsActive { get; protected set; }
    public int Cooldown;
    public int Charges;
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
        ScribeValues.Look(ref Charges, "Charges");
        ScribeValues.Look(ref Kills, "Kills");
    }

    public override string ToString()
    {
        return $"{Trinket.Label} Handler";
    }

    public virtual DamageRecord? HandleCombat(Pawn pawn, Pawn target)
    {
        return null;
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
        Charges = 0;
        IsActive = false;
    }

    public virtual void Stop()
    {
        DeActivate();
    }

    public virtual void PostCombatAction(Pawn playerPawn, Pawn enemyPawn)
    {
    }
}