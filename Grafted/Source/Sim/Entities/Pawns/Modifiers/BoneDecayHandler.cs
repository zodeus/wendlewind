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

        CheckIfLostVitalPart();
    }

    public override void ExposeData()
    {
        base.ExposeData();
    }

    public override bool ApplyToPart(BodyPart part)
    {
        if (part.Substance != SubstanceType.Bone && part.Substance != SubstanceType.Chitin)
        {
            return false;
        }

        part.TryAddModifier(this);

        return true;
    }
}