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

    public override string GetEffectDescription(Item item)
    {
        var pool = item.GetStatValue(Defs.Stats.HealingValue);
        return $"Sprays a {pool:0} HP pool through flesh, bone, and skin.";
    }

    public override IReadOnlyList<string> GetHowItWorks(Item item) =>
    [
        "Heals the targeted limb, then travels through connected sockets.",
        "Spends the pool on flesh, bone, and skin — not organs or arteries.",
        "Stops when the pool runs out."
    ];

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
