using Grafted.Sim.Entities.Pawns.Modifiers;

namespace Grafted.Sim.Entities.Items.Enchantments;

[UsedImplicitly]
public class SoothingVibrationsHandler : EnchantmentHandler
{
    private const int DurationInTicks = 60;
    public override void HandlePawnTakeDamageEffect(BodyPart bodyPart, Pawn pawn, Pawn requestSource, DamageRecord damageRecord)
    {
        ApplyToRegenerationToPart(bodyPart);
        foreach (var externalPart in bodyPart.ExternalParts)
        {
            ApplyToRegenerationToPart(externalPart);
        }
    }

    public void ApplyToRegenerationToPart(BodyPart part)
    {
        part.TryAddModifier(BodyPartModifierGenerator.Generate(Defs.BodyPartModifiers.LifeRegeneration, DurationInTicks));
        foreach (var internalPart in part.AllInternalParts)
        {
            internalPart.TryAddModifier(BodyPartModifierGenerator.Generate(Defs.BodyPartModifiers.LifeRegeneration, DurationInTicks));
        }
    }
}