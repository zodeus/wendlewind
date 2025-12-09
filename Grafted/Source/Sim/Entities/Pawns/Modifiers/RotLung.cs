namespace Grafted.Sim.Entities.Pawns.Modifiers;

[UsedImplicitly]
public class RotLung : BodyPartModifier
{
    private const double DamageFactorPerTick = .011f;

    public override void Tick()
    {
        base.Tick();
        BodyPart.HitPoints -= BodyPart.HitPoints * DamageFactorPerTick;
        CheckIfLostVitalPart();
        base.Tick();
    }

    public override bool ApplyToPart(BodyPart part)
    {
        if (part.Type is not (BodyPartType.Head or BodyPartType.Neck or BodyPartType.Torso))
        {
            return false;
        }

        var lung = part.Body?.AllExternalParts
            .FirstOrNull(p => p?.Type == BodyPartType.Torso)?
            .AllInternalParts.Where(p => p.Type == BodyPartType.Lung).RandomElement();

        if (lung == null)
        {
            Log.Warning($"No lungs found while applying body part modifier {Defs.BodyPartModifiers.RotLung.Moniker}");
            return false;
        }

        lung.TryAddModifier(this);
        
        return true;
    }
}