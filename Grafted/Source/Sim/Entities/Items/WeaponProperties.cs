namespace Grafted.Sim.Entities.Items;

public class WeaponProperties
{
    
    public WeaponType WeaponType = WeaponType.None;
    public DamageType DamageType = DamageType.Invalid;
    public List<BodyPartModifierRecord> BodyPartModifiers = new();
    public List<WeaponManeuverDef> WeaponManeuvers = new();
    public List<SubstanceModifier> SubstanceModifiers = new();

    public float GetSubstanceModifier(SubstanceType substance)
    {
        foreach (var mod in SubstanceModifiers)
        {
            if (mod.Substance == substance)
            {
                return mod.Modifier;
            }
        }
        return 1f;
    }
}