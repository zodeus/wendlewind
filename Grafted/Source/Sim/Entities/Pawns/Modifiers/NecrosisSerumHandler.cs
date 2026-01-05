namespace Grafted.Sim.Entities.Pawns.Modifiers;

[UsedImplicitly]
public class NecrosisSerumHandler : BodyPartModifier
{
    public override void Tick()
    {
        if (!IsExpired) return;

        var modifier = BodyPart.Modifiers.FirstOrNull(m => m?.Def == Defs.BodyPartModifiers.Necrosis);
        if (modifier!=null)
        {
            modifier.IsExpired = true;
        }
        base.Tick();
    }

    public override bool ApplyToPart(BodyPart part)
    {
        part.TryAddModifier(this);

        return true;
    }
}