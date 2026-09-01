namespace Wendlemire.Sim.Entities.Items;

public class WeaponManeuverDef : Def {
    public List<WeaponType>? Weapons = null;
    public RangeFloat DamageMultiplier = new(1, 1);
}