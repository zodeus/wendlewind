namespace Grafted.Sim.Entities.Pawns.Modifiers;

[UsedImplicitly]
public class PurpleRegeneration : BodyPartModifier
{
    public override void Tick()
    {
        base.Tick();
        // Chance to regenerate from destroyed status
        if (BodyPart.HitPoints < 1 && Core.Random.Chance(0.01f))
        {
            BodyPart.HitPoints = 1;
        }

        //if (Core.Context.Ticks % 15==0)
        //{
        BodyPart.HitPoints += BodyPart.HitPoints * .001f;
        //}
    }
}