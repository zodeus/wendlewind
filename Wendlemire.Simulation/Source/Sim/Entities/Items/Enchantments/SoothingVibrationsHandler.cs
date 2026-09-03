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
        var durationScale = HostHasEnchant(pawn, Defs.Items.ElvishLeaf)
            ? ItemSynergies.SoothingLeafDuration
            : 1f;

        ApplyToRegenerationToPart(bodyPart, magic, durationScale);
        foreach (var externalPart in bodyPart.ExternalParts)
        {
            ApplyToRegenerationToPart(externalPart, magic, durationScale);
        }
    }

    private void ApplyToRegenerationToPart(BodyPart part, float magic, float durationScale)
    {
        var duration = Math.Max(1, (int)Math.Round(DurationInTicks * magic * durationScale));
        part.TryAddModifier(Context.Factory.CreateModifier(Defs.BodyPartModifiers.HealthRegeneration, duration, magic));
        foreach (var internalPart in part.AllInternalParts)
        {
            internalPart.TryAddModifier(Context.Factory.CreateModifier(Defs.BodyPartModifiers.HealthRegeneration, duration, magic));
        }
    }
}