namespace Wendlewind.Sim.Entities.Items.Potions;

/// <summary>
/// Handler for Acid Flask - burns out opponent's eyes.
/// </summary>
[UsedImplicitly]
public class AcidFlaskHandler : PotionHandler
{
    public AcidFlaskHandler(IRng rng)
    {
        Rng = rng;
    }

    public override bool CanUseInCombat => true;
    public override bool CanUseOutsideCombat => false;
    
    public override PotionUseResult UseInCombat(Pawn user, Pawn? target = null)
    {
        if (target == null)
        {
            return PotionUseResult.Failed("Acid Flask requires a target.");
        }
        
        string? burnedEyeMessage = null;
        
        foreach (var eye in target.Body.AllExternalParts
            .Where(part => part.Type == BodyPartType.Eye)
            .InRandomOrder(Context.Rng))
        {
            if (Context.Rng.Chance(1))
            {
                eye.HitPoints = 0;
                var eyeText = $"{eye.Socket?.Label.Split(" ")[0]} {eye.Type}";
                burnedEyeMessage = $"/c[{TC.Attacker}]{user.LabelShort} /c[{TC.Yellow}]burned out " +
                                   $"/c[{TC.Victim}]{target.LabelShort}'s /c[{TC.BodyPart}]{eyeText} " +
                                   $"/c[{TC.Default}]with /c[{TC.Item}]{PotionLabel}";
                break;
            }
        }
        
        var message = burnedEyeMessage ?? 
            $"/c[{TC.Attacker}]{user.LabelShort} /c[{TC.Yellow}]threw acid at /c[{TC.Victim}]{target.LabelShort} /c[{TC.Default}]but missed";
        
        return PotionUseResult.Succeeded(
            message,
            alertMessage: $"{target.Label} has been splashed with acid",
            alertColor: Color.YellowGreen
        );
    }
    
    public override string GetEffectDescription()
    {
        return "Throws acid at opponent, potentially burning out their eyes.";
    }
}
