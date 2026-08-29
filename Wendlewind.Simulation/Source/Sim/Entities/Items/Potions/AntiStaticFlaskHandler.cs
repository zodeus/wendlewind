namespace Wendlewind.Sim.Entities.Items.Potions;

/// <summary>
/// Handler for AntiStaticFlask potion - purges all electrified modifiers from body parts.
/// </summary>
[UsedImplicitly]
public class AntiStaticFlaskHandler : PotionHandler
{
    public AntiStaticFlaskHandler(IRng rng)
    {
        Rng = rng;
    }

    public override bool CanUseInCombat => true;
    public override bool CanUseOutsideCombat => true;
    public override bool CanAutoUse => true;

    public override PotionUseResult UseOutsideCombat(Pawn user)
    {
        return Use(user);
    }

    public override PotionUseResult UseInCombat(Pawn user, Pawn? target = null)
    {
        return Use(user);
    }

    private PotionUseResult Use(Pawn user)
    {
        var actualTarget = user;

        // Find and expire all Electrofied modifiers on all body parts
        var purgedCount = 0;
        foreach (var part in actualTarget.Body.AllParts)
        {
            foreach (var modifier in part.Modifiers)
            {
                if (modifier.Def == Defs.BodyPartModifiers.Electrofied)
                {
                    modifier.IsExpired = true;
                    purgedCount++;
                }
            }
        }

        var message = $"/c[{TC.Attacker}]{actualTarget.LabelShort} /c[{TC.Yellow}]gulped down the /c[{TC.Item}]{PotionLabel}";

        return PotionUseResult.Succeeded(
            message,
            alertMessage: purgedCount > 0
                ? $"{actualTarget.Label} is grounded - static purged"
                : $"{actualTarget.Label} feels strangely calm",
            alertColor: Color.Cyan
        );
    }

    public override PotionUseResult? TryAutoUse(Pawn pawn)
    {
        // // Auto-use if any body part has the Electrofied modifier
        // var hasElectrofied = pawn.Body.AllParts.Any(p =>
        //     p.Modifiers.Any(m => m.Def == Defs.BodyPartModifiers.Electrofied));

        // if (hasElectrofied)
        //     return UseInCombat(pawn);

        return null;
    }

    public override string GetEffectDescription()
    {
        return "Instantly purges all electrified afflictions from the body, grounding dangerous currents.";
    }
}
