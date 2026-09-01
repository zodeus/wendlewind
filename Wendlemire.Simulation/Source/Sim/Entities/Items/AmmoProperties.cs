namespace Wendlemire.Sim.Entities.Items;

public class AmmoProperties
{
    public DamageType DamageType = DamageType.Invalid;
    public readonly RangeFloat DamageRange = new(0, 0);
    public List<BodyPartModifierRecord> BodyPartModifiers = new();
    
    // Explosive properties - if set, the ammo will deal splash damage
    public bool IsExplosive = false;
    public readonly RangeFloat SplashDamageRange = new(0, 0);
}