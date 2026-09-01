namespace Wendlemire.Sim.Entities.Items.Potions;

/// <summary>
/// Thickens the drinker's blood so open wounds drip instead of emptying them.
/// </summary>
[UsedImplicitly]
public class PitchbloodHandler : PotionHandler
{
    public PitchbloodHandler(IRng rng)
    {
        Rng = rng;
    }

    public override bool CanUseInCombat => true;
    public override bool CanUseOutsideCombat => false;

    public override PotionUseResult UseInCombat(Pawn user, Pawn? target = null)
    {
        var duration = GetDuration();
        user.Body.Effects.TryApplyEffect(new BodyEffect
        {
            Def = Defs.BodyEffects.Pitchblood,
            TicksLeft = duration
        });

        var message = $"/c[{TC.Attacker}]{user.LabelShort} /c[{TC.Yellow}]swallowed the /c[{TC.Item}]{PotionLabel}";

        return PotionUseResult.Succeeded(
            message,
            alertMessage: $"{user.Label}'s blood thickens to pitch",
            alertColor: Color.DarkRed
        );
    }

    public override string GetEffectDescription()
    {
        return "Thickens the blood so wounds weep instead of spray. Does not restore lost blood.";
    }
}
