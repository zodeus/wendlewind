using Grafted.Sim.Entities.Pawns;
using Grafted.Sim.Entities.Pawns.Modifiers;

namespace Grafted.Sim.Combat;

/// <summary>
/// Encapsulates the parameters needed when applying damage to body parts.
/// Created from a Damage object to pass through the damage cascade chain.
/// </summary>
public record DamageContext(
    double Amount,
    DamageType Type,
    string WeaponManeuver,
    List<BodyPartModifierRecord> BodyPartModifiers,
    Func<SubstanceType, float>? GetSubstanceModifier = null)
{
    /// <summary>
    /// Creates a DamageContext from a Damage object with the specified amount.
    /// </summary>
    public static DamageContext FromDamage(Damage damage, double amount) => new(
        amount,
        damage.Type,
        damage.WeaponManeuver,
        damage.BodyPartModifiers,
        damage.GetSubstanceModifier);

    /// <summary>
    /// Creates a new context with a different damage amount.
    /// </summary>
    public DamageContext WithAmount(double newAmount) => this with { Amount = newAmount };
}

