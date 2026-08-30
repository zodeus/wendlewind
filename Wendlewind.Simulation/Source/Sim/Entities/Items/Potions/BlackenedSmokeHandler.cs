namespace Wendlewind.Sim.Entities.Items.Potions;

/// <summary>
/// Handler for BlackenedSmoke potion - applies a body effect that decreases accuracy
/// and infects lungs with BlackLung modifier.
/// </summary>
[UsedImplicitly]
public class BlackenedSmokeHandler : PotionHandler
{
    public BlackenedSmokeHandler(IRng rng)
    {
        Rng = rng;
    }

    public override bool CanUseInCombat => true;
    public override bool CanUseOutsideCombat => false;
    private const double BlackLungPower = 1;

    public override Pawn GetCombatApplicationTarget(Pawn user, Pawn? opponent) => opponent ?? user;

    public override PotionUseResult UseInCombat(Pawn user, Pawn? target = null)
    {
        var actualTarget = target ?? user;
        var duration = GetDuration();

        // Apply the BlackenedSmoke body effect (reduces accuracy)
        actualTarget.Body.Effects.TryApplyEffect(new BodyEffect
        {
            Def = Defs.BodyEffects.BlackenedSmoke,
            TicksLeft = duration
        });

        // Apply BlackLung modifier to the target's lungs
        var blackLungModifier = Context.Factory.CreateModifier(Defs.BodyPartModifiers.BlackLung, duration, BlackLungPower);
        var lungs = actualTarget.Body.AllParts.Where(p => p?.Type == BodyPartType.Lung).ToList();

        var lungsAffected = 0;
        foreach (var lung in lungs)
        {
            lung.TryAddModifier(Context.Factory.CreateModifier(Defs.BodyPartModifiers.BlackLung, duration, BlackLungPower));
            lungsAffected++;
        }

        var message = $"/c[{TC.Attacker}]{user.LabelShort} /c[{TC.Yellow}]hurled a /c[{TC.Item}]{PotionLabel} /c[{TC.Yellow}]at /c[{TC.Victim}]{actualTarget.LabelShort}";

        return PotionUseResult.Succeeded(
            message,
            alertMessage: lungsAffected > 0
                ? $"{actualTarget.Label}'s lungs fill with acrid black smoke"
                : $"{actualTarget.Label} is shrouded in black smoke",
            alertColor: Color.DarkGray
        );
    }

    public override string GetEffectDescription()
    {
        return "Hurls a flask of blackened smoke that reduces accuracy and damages the target's lungs with a choking affliction.";
    }
}
