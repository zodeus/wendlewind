using System.Globalization;

namespace Wendlemire.Sim.Entities.Items.Medicinals;

[UsedImplicitly]
public class MendersMistHandler : MedicinalHandler
{
    public MendersMistHandler(IRng rng)
    {
        Rng = rng;
    }

    private double _mistAmount;

    // Colors for the infographic

    public override bool ApplyToPart(Item item, BodyPart part)
    {
        var healingValue = item.GetStatValue(Defs.Stats.HealingValue);
        _mistAmount = healingValue;
        MistPart(part);
        return _mistAmount < healingValue;
    }

    private void MistPart(BodyPart bodyPart)
    {
        if (_mistAmount <= 0)
        {
            return;
        }

        _mistAmount -= UpdateHealth(bodyPart);
        foreach (var internalPart in bodyPart.InternalParts)
        {
            if (internalPart.Substance == SubstanceType.Bone || internalPart.Type is BodyPartType.Skin)
            {
                _mistAmount -= UpdateHealth(internalPart);
            }
        }

        foreach (var externalPart in bodyPart.ExternalParts)
        {
            MistPart(externalPart);
        }
    }

    private double UpdateHealth(BodyPart bodyPart)
    {
        var currentHealth = bodyPart.HitPoints;
        bodyPart.HitPoints += Math.Min(bodyPart.MaxHitPoints - bodyPart.HitPoints, _mistAmount);
        return bodyPart.HitPoints - currentHealth;
    }
}
