namespace Grafted.Sim.Entities.Pawns.Modifiers;

[UsedImplicitly]
public class NecrosisHandler : BodyPartModifier
{
    private const double DamageFactorPerTick = .001f;

    public override void Tick()
    {
        BodyPart.HitPoints -= BodyPart.HitPoints * DamageFactorPerTick;
        CheckIfLostVitalPart(BodyPart);
        //base.Tick();
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

    public override BodyPartModifierDef? ApplyToPart(BodyPart part)
    {
        if (part.IsExternal == false)
        {
            return null;
        }

        part.TryAddModifier(this);

        //todo raise event MODIFIER APPLIED
        return Def;
    }
}