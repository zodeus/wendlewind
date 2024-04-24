namespace Grafted.Sim.Entities.Items;

public class WeaponProperties {
    public List<BodyPartModifierRecord> BodyPartModifiers = new();
    public DamageType DamageType = DamageType.Invalid;
}