namespace Grafted.Sim.Entities.Items.Potions;

/// <summary>
/// Handler for Puss Bomb - applies festering to target's external body parts.
/// </summary>
[UsedImplicitly]
public class PussBombHandler : PotionHandler
{
    private static RangeInt FesteringDuration = new RangeInt(400, 900);
    private const double FesteringPower = 2;

    public override bool CanUseInCombat => true;

    public override PotionUseResult UseInCombat(Pawn user, Pawn? target = null)
    {
        if (target == null)
        {
            return PotionUseResult.Failed("Puss Bomb requires a target.");
        }

        var partsAffected = 0;

        // Apply festering to random external body parts
        foreach (var part in target.Body.AllExternalParts.Where(p => p.Type != BodyPartType.Eye))
        {
            var duration = FesteringDuration.RandomValue;
            var modifier = BodyPartModifierGenerator.Generate(Defs.BodyPartModifiers.Festering, duration, FesteringPower);
            if (modifier.ApplyToPart(part))
            {
                partsAffected++;
            }
        }

        var message = partsAffected > 0
            ? $"/c[{TC.Attacker}]{user.LabelShort} /c[{TC.Yellow}]hurled a /c[{TC.Item}]{PotionLabel} " +
              $"/c[{TC.Default}]at /c[{TC.Victim}]{target.LabelShort}/c[{TC.Default}], splattering them with infection"
            : $"/c[{TC.Attacker}]{user.LabelShort} /c[{TC.Yellow}]threw a /c[{TC.Item}]{PotionLabel} " +
              $"/c[{TC.Default}]at /c[{TC.Victim}]{target.LabelShort} /c[{TC.Default}]but it had no effect";

        return PotionUseResult.Succeeded(
            message,
            alertMessage: partsAffected > 0
                ? $"{target.Label} is covered in festering pus"
                : $"{target.Label} resisted the infection",
            alertColor: Color.YellowGreen
        );
    }

    public override string GetEffectDescription()
    {
        return "Hurls a festering bomb that infects the target's flesh, causing it to rot.";
    }
}
