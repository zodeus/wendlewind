namespace Grafted.Sim.Entities.Pawns.Modifiers;

[UsedImplicitly]
public class RotLung : BodyPartModifier
{
    private const double DamageFactorPerTick = .011f;

    public override void Tick()
    {
        BodyPart.HitPoints -= BodyPart.HitPoints * DamageFactorPerTick;
        CheckIfLostVitalPart(BodyPart);
        base.Tick();
    }

    private bool CheckIfLostVitalPart(BodyPart bodyPart)
    {
        if (bodyPart.IsFunctional) return false;

        var remainingFunctionalParts = bodyPart.Body!.AllParts.Count(p => p.Type == bodyPart.Type && p.IsFunctional);
        if (bodyPart is { IsVital: true, IsFunctional: false } && remainingFunctionalParts <= 0)
        {
            bodyPart.Body.Pawn.TriggerDeath($"{bodyPart.Label} {(bodyPart.IsDestroyed ? "was destroyed" : "stopped functioning")}");
            return true;
        }

        return false;
    }


    public override void Expired()
    {
    }
}