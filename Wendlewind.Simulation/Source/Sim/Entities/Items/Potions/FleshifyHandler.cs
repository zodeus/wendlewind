namespace Wendlewind.Sim.Entities.Items.Potions;

/// <summary>
/// Handler for Fleshify potion - converts all non-flesh body parts into flesh.
/// </summary>
[UsedImplicitly]
public class FleshifyHandler : PotionHandler
{
    public FleshifyHandler(IRng rng)
    {
        Rng = rng;
    }

    public override bool CanUseInCombat => true;
    public override bool CanUseOutsideCombat => true;

    public override Pawn GetCombatApplicationTarget(Pawn user, Pawn? opponent) => opponent ?? user;
    
    public override PotionUseResult UseInCombat(Pawn user, Pawn? target = null)
    {
        return Fleshify(target ?? user);
    }
    
    public override PotionUseResult UseOutsideCombat(Pawn user)
    {
        return Fleshify(user);
    }
    
    private PotionUseResult Fleshify(Pawn pawn)
    {
        var partsConverted = 0;
        
        foreach (var part in pawn.Body.AllParts)
        {
            if (part.Substance != SubstanceType.Flesh)
            {
                part.SetSubstanceOverride(SubstanceType.Flesh);
                partsConverted++;
            }
        }

        var message = partsConverted > 0
            ? $"/c[{TC.Attacker}]{pawn.LabelShort} /c[{TC.Yellow}]consumed the /c[{TC.Item}]{PotionLabel}/c[{TC.Yellow}]. {partsConverted} body parts turned to flesh!"
            : $"/c[{TC.Attacker}]{pawn.LabelShort} /c[{TC.Yellow}]consumed the /c[{TC.Item}]{PotionLabel}/c[{TC.Yellow}], but they were already entirely flesh.";
        
        return PotionUseResult.Succeeded(
            message,
            alertMessage: partsConverted > 0 
                ? $"{pawn.Label}'s body transforms into soft, pliable flesh" 
                : null,
            alertColor: Color.PaleVioletRed
        );
    }
    
    public override string GetEffectDescription()
    {
        return "Permanently converts all non-flesh body parts into flesh.";
    }
}
