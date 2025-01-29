namespace Grafted.Sim.Entities.Pawns.Modifiers;

[UsedImplicitly]
public class BoneDecayHandler : BodyPartModifier
{
    public override void Tick()
    {
        BodyPart.HitPoints -= Math.Clamp(BodyPart.HitPoints * .001, 0.01, 0.5);

        if (BodyPart.IsDestroyed)
        {
            IsExpired = true;
        }

        CheckIfLostVitalPart(BodyPart);
    }

    public override void ExposeData()
    {
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
        if (part.IsBone == false)
        {
            return false;
        }

        part.TryAddModifier(this);

        return true;
    }
}