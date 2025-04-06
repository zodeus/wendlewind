namespace Grafted.Sim.Entities.Pawns.Modifiers;

[UsedImplicitly]
public class FesteringHandler : BodyPartModifier
{
    private const double DamageFactorPerTick = .001f;
    private const double SpreadThreshold = .5;
    private bool _hasSpread;

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

        CheckIfLostVitalPart(BodyPart);
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

    private void CheckIfLostVitalPart(BodyPart bodyPart)
    {
        if (bodyPart.IsFunctional) return;

        if (bodyPart.Body == null)
        {
            Log.Warning($"bodyPart.Body was null for {bodyPart} ");
            return;
        }

        var remainingFunctionalParts = bodyPart.Body!.AllParts.Count(p => p.Type == bodyPart.Type && p.IsFunctional);
        if (bodyPart is { IsVital: true, IsFunctional: false } && remainingFunctionalParts <= 0)
        {
            bodyPart.Body.Pawn.TriggerDeath($"{bodyPart.Label} {(bodyPart.IsDestroyed ? "was destroyed" : "stopped functioning")}");
        }
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
}