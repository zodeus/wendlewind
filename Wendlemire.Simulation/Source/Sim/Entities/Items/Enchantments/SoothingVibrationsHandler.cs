namespace Wendlemire.Sim.Entities.Items.Enchantments;

[UsedImplicitly]
public class SoothingVibrationsHandler : EnchantmentHandler
{
    public SoothingVibrationsHandler(IRng rng)
    {
        Rng = rng;
    }

    private const int DurationInTicks = 48;
    public override void PostPawnDamageTakenEffect(BodyPart bodyPart, Pawn pawn, Pawn requestSource, DamageRecord damageRecord)
    {
        var magic = GetMagic(pawn);
        ApplyToRegenerationToPart(bodyPart, magic);
        foreach (var externalPart in bodyPart.ExternalParts)
        {
            ApplyToRegenerationToPart(externalPart, magic);
        }
    }

    public void ApplyToRegenerationToPart(BodyPart part, float magic)
    {
        var duration = Math.Max(1, (int)Math.Round(DurationInTicks * magic));
        part.TryAddModifier(Context.Factory.CreateModifier(Defs.BodyPartModifiers.HealthRegeneration, duration, magic));
        foreach (var internalPart in part.AllInternalParts)
        {
            internalPart.TryAddModifier(Context.Factory.CreateModifier(Defs.BodyPartModifiers.HealthRegeneration, duration, magic));
        }
    }
}