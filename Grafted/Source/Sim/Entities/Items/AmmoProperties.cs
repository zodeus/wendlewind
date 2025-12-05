namespace Grafted.Sim.Entities.Items;

public class AmmoProperties
{
    public DamageType DamageType = DamageType.Invalid;
    public readonly RangeFloat DamageRange = new(0, 0);
}