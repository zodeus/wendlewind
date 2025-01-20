using Grafted.Sim.Entities.Pawns.Modifiers;

namespace Grafted.Sim.Entities.Items;

public class WeaponProperties
{
    
    public WeaponType WeaponType = WeaponType.None;
    public DamageType DamageType = DamageType.Invalid;
    public List<BodyPartModifierRecord> BodyPartModifiers = new();
    public List<WeaponManeuverDef> WeaponManeuvers = new();
}