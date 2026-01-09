namespace Grafted.Sim.Entities.Items.Weapons;

/// <summary>
/// Base class for unique weapon handlers that execute special effects during combat.
/// </summary>
public abstract class WeaponHandler : IExposable
{
    public Item Weapon = null!;

    public string Label => Weapon.Label;

    /// <summary>
    /// Called after the weapon successfully hits a target and deals damage.
    /// </summary>
    /// <param name="attacker">The pawn wielding the weapon</param>
    /// <param name="victim">The pawn that was hit</param>
    /// <param name="request">The damage request containing attack details</param>
    /// <param name="response">The damage response containing hit results</param>
    /// <param name="damageRecord">The specific damage record for this hit</param>
    public virtual void OnHit(Pawn attacker, Pawn victim, DamageRequest request, DamageRecord damageRecord)
    {
    }

    public virtual void Tick()
    {
    }

    public virtual void ExposeData()
    {
        ScribeReferences.Look(ref Weapon!, "Weapon");
    }

    public override string ToString()
    {
        return $"{Weapon.Label} Handler";
    }
}
