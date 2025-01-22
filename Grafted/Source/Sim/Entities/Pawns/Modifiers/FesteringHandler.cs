namespace Grafted.Sim.Entities.Pawns.Modifiers;

[UsedImplicitly]
public class FesteringHandler : BodyPartModifier
{
    private const double DamageFactorPerTick = .001f;
    private const double SpreadThreshold = .5;
    private bool HasSpread = false;

    public override void Tick()
    {
        BodyPart.HitPoints -= BodyPart.HitPoints * DamageFactorPerTick;
        if (BodyPart.HealthPercent < SpreadThreshold)
        {
            var childPart = BodyPart.ExternalParts.InRandomOrder().FirstOrNull();
            var parentPart = BodyPart.Socket?.ParentPart;

            if (HasSpread) return;

            if (childPart != null && Core.Random.Chance(0.5f))
            {
                ApplyToPart(childPart);
                HasSpread = true;
            }
            else if (parentPart != null)
            {
                ApplyToPart(parentPart);
                HasSpread = true;
            }
        }

        CheckIfLostVitalPart(BodyPart);
        //base.Tick();
    }

    public override void ExposeData()
    {
        ScribeValues.Look(ref HasSpread, "HasSpread");
        base.ExposeData();
    }

    private void CheckIfLostVitalPart(BodyPart bodyPart)
    {
        if (bodyPart.IsFunctional) return;

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