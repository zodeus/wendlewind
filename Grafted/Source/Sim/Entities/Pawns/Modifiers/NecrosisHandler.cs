namespace Grafted.Sim.Entities.Pawns.Modifiers;

[UsedImplicitly]
public class NecrosisHandler : BodyPartModifier
{
    private static RangeDouble DamageFactorPerTick = new (0.001, 0.002);
    private const int TotalTicksToSpread = 6000;

    public static readonly List<SubstanceType> AllowedSubstances = [
        SubstanceType.Flesh, SubstanceType.Bone, SubstanceType.Fungus, SubstanceType.Wood
    ];

    private int _ticksToSpread;

    public override void Tick()
    {
        _ticksToSpread = Math.Clamp(_ticksToSpread--, 0, TotalTicksToSpread);
        BodyPart.HitPoints -= BodyPart.HitPoints * DamageFactorPerTick.RandomValue;
        if (BodyPart.IsDestroyed && _ticksToSpread < 1)
        {
            _ticksToSpread = TotalTicksToSpread;
            List<BodyPart> randomParts = [];
            if (BodyPart.Socket?.ParentPart != null)
            {
                randomParts.Add(BodyPart.Socket.ParentPart);
            }

            randomParts.AddRange(BodyPart.ExternalParts.InRandomOrder());

            if (randomParts.Count > 0)
            {
                var part = randomParts.RandomElement();
                SpreadTo(part);
            }
        }

        CheckIfLostVitalPart();
    }

    public override bool ApplyToPart(BodyPart part)
    {
        if (part.IsExternal == false)
        {
            return false;
        }

        part.TryAddModifier(this);

        return true;
    }

    public override void ExposeData()
    {
        ScribeValues.Look(ref _ticksToSpread, "TicksToSpread");
        base.ExposeData();
    }
}