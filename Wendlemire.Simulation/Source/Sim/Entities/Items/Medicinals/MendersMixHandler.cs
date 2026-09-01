using System.Globalization;

namespace Wendlemire.Sim.Entities.Items.Medicinals;

[UsedImplicitly]
public class MendersMixHandler : MedicinalHandler
{
    public MendersMixHandler(IRng rng)
    {
        Rng = rng;
    }

    private double _healAmount;
    private bool _appliedAnyEffect;

    // Colors for the infographic
    private const double RhinoPower = 1;

    public override bool ApplyToPart(Item item, BodyPart part)
    {
        var healingValue = item.GetStatValue(Defs.Stats.HealingValue);
        var duration = item.ItemDef.MedicinalProperties!.DurationInTicks;

        _healAmount = healingValue;
        _appliedAnyEffect = false;
        ApplyToPart(part, duration);

        return _healAmount < healingValue || _appliedAnyEffect;
    }

    private void ApplyToPart(BodyPart bodyPart, int duration)
    {
        if (_healAmount <= 0)
        {
            return;
        }

        _healAmount -= UpdateHealth(bodyPart);
        bodyPart.TryAddModifier(Context.Factory.CreateModifier(Defs.BodyPartModifiers.RhinoRestoration, duration, RhinoPower));
        _appliedAnyEffect = true;

        foreach (var internalPart in bodyPart.AllInternalParts)
        {
            _healAmount -= UpdateHealth(internalPart);
            internalPart.TryAddModifier(Context.Factory.CreateModifier(Defs.BodyPartModifiers.RhinoRestoration, duration, RhinoPower));
        }

        foreach (var externalPart in bodyPart.ExternalParts)
        {
            ApplyToPart(externalPart, duration);
        }
    }

    private double UpdateHealth(BodyPart bodyPart)
    {
        var currentHealth = bodyPart.HitPoints;
        bodyPart.HitPoints += _healAmount;
        return bodyPart.HitPoints - currentHealth;
    }
}
