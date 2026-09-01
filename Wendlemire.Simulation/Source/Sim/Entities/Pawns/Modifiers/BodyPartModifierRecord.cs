namespace Wendlemire.Sim.Entities.Pawns.Modifiers;

public class BodyPartModifierRecord
{
    public BodyPartModifierDef Def = null!;
    public RangeInt DurationInTicks = new(0, 0);
    public RangeFloat Chance = RangeFloat.One;
    public double Power = 1.0;
}