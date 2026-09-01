namespace Wendlemire.Sim.Entities.Pawns.Modifiers;

public class BodyPartModifierRecord
{
    public BodyPartModifierDef Def = null!;
    public RangeInt DurationInTicks = new(0, 0);
    public RangeFloat Chance = RangeFloat.One;
    public double Power = 1.0;

    public BodyPartModifierRecord ScaledBy(float magic, bool scaleChance, bool scaleDuration, bool scalePower)
    {
        if (Math.Abs(magic - 1f) < 0.0001f || (!scaleChance && !scaleDuration && !scalePower))
        {
            return this;
        }

        return new BodyPartModifierRecord
        {
            Def = Def,
            DurationInTicks = scaleDuration
                ? new RangeInt(
                    (int)Math.Round(DurationInTicks.Min * magic),
                    (int)Math.Round(DurationInTicks.Max * magic))
                : DurationInTicks,
            Chance = scaleChance ? Chance * magic : Chance,
            Power = scalePower ? Power * magic : Power
        };
    }
}