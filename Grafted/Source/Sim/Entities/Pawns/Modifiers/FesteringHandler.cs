namespace Grafted.Sim.Entities.Pawns.Modifiers;

[UsedImplicitly]
public class FesteringHandler : BodyPartModifier
{
    private const double DamageFactorPerTick = .001f;
    private const double SpreadThreshold = .5;
    private bool _hasSpread;
    
    public static readonly List<SubstanceType> AllowedSubstances = [SubstanceType.Flesh, SubstanceType.Bone];
    
    public override void Tick()
    {
        base.Tick();

        if (BodyPart.IsSevered)
        {
            return;
        }
        BodyPart.HitPoints -= BodyPart.MaxHitPoints * DamageFactorPerTick;
        if (BodyPart.HealthPercent < SpreadThreshold)
        {
            if (_hasSpread) return;
            var childPart = BodyPart.ExternalParts.InRandomOrder().FirstOrNull();
            var parentPart = BodyPart.Socket?.ParentPart;

            if (childPart != null && Core.Random.Chance(0.5f))
            {
                SpreadTo(childPart);
                _hasSpread = true;
            }
            else if (parentPart != null)
            {
                SpreadTo(parentPart);
                _hasSpread = true;
            }
        }

        CheckIfLostVitalPart();
    }


    public override void MergeWith(BodyPartModifier modifier)
    {
        _hasSpread = false;
        base.MergeWith(modifier);
    }

    public override void ExposeData()
    {
        ScribeValues.Look(ref _hasSpread, "HasSpread");
        base.ExposeData();
    }

    public override bool ApplyToPart(BodyPart part)
    {
        if (part.IsExternal == false)
        {
            return false;
        }

        if (AllowedSubstances.Contains(part.Substance) == false)
        {
            return false;
        }

        part.TryAddModifier(this);

        return true;
    }
}