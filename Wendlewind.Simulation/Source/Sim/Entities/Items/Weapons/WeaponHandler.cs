namespace Wendlewind.Sim.Entities.Items.Weapons;

/// <summary>
/// Base class for unique weapon handlers that execute special effects during combat.
/// </summary>
public abstract class WeaponHandler : IExposable, IHasContext, IHasRng
{
    public GameContext Context { get; set; } = null!;
    public IRng Rng { get; set; } = null!;
    public Item Weapon = null!;

    public string Label => Weapon.Label;

    /// <summary>
    /// Called after the weapon successfully hits a target and deals damage.
    /// </summary>
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
