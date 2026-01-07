namespace Grafted.Sim.Entities.Items.Medicinals;

[UsedImplicitly]
public class MendersMixHandler : MedicinalHandler
{
    private double _healAmount;

    public override bool ApplyToPart(Item item, BodyPart part)
    {
        var healingValue = item.GetStatValue(Defs.Stats.HealingValue);
        var duration = item.ItemDef.MedicinalProperties!.DurationInTicks;
        
        _healAmount = healingValue;
        ApplyToPart(part, duration);
        
        return _healAmount < healingValue;
    }

    private void ApplyToPart(BodyPart bodyPart, int duration)
    {
        if (_healAmount <= 0)
        {
            return;
        }

        // Heal this part and apply soothing balm
        _healAmount -= UpdateHealth(bodyPart);
        bodyPart.TryAddModifier(BodyPartModifierGenerator.Generate(Defs.BodyPartModifiers.SoothingBalm, duration));

        // Heal internal parts (bone, flesh, skin, organs)
        foreach (var internalPart in bodyPart.InternalParts)
        {
            _healAmount -= UpdateHealth(internalPart);
            internalPart.TryAddModifier(BodyPartModifierGenerator.Generate(Defs.BodyPartModifiers.SoothingBalm, duration));
        }

        // Recursively apply to external parts (travels through sockets)
        foreach (var externalPart in bodyPart.ExternalParts)
        {
            ApplyToPart(externalPart, duration);
        }
    }

    private double UpdateHealth(BodyPart bodyPart)
    {
        var currentHealth = bodyPart.HitPoints;
        bodyPart.HitPoints += Math.Min(bodyPart.MaxHitPoints - bodyPart.HitPoints, _healAmount);
        return bodyPart.HitPoints - currentHealth;
    }
}
