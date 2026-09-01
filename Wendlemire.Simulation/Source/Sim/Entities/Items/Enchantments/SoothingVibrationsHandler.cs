namespace Wendlemire.Sim.Entities.Items.Enchantments;

[UsedImplicitly]
public class SoothingVibrationsHandler : EnchantmentHandler
{
    public SoothingVibrationsHandler(IRng rng)
    {
        Rng = rng;
    }

    private const int DurationInTicks = 60;
    public override void PostPawnDamageTakenEffect(BodyPart bodyPart, Pawn pawn, Pawn requestSource, DamageRecord damageRecord)
    {
        ApplyToRegenerationToPart(bodyPart);
        foreach (var externalPart in bodyPart.ExternalParts)
        {
            ApplyToRegenerationToPart(externalPart);
        }
    }

    public void ApplyToRegenerationToPart(BodyPart part)
    {
        part.TryAddModifier(Context.Factory.CreateModifier(Defs.BodyPartModifiers.HealthRegeneration, DurationInTicks, 1));
        foreach (var internalPart in part.AllInternalParts)
        {
            internalPart.TryAddModifier(Context.Factory.CreateModifier(Defs.BodyPartModifiers.HealthRegeneration, DurationInTicks, 1));
        }
    }
}