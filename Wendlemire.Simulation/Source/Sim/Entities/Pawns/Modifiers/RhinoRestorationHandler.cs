namespace Wendlemire.Sim.Entities.Pawns.Modifiers;

[UsedImplicitly]
public class RhinoRestorationHandler : BodyPartModifier
{
    public RhinoRestorationHandler(IRng rng)
    {
        Rng = rng;
    }

    private const double HealthRestoredPerTick = .14;
    private const double SkinHealthRestoredPerTick = .2;
    private const double DestroyPartRegenerationPercent = 0.05f;
    private const double EnhancedHealthMultiplier = 5;
    private const int EnhancedHealthDuration = 20;
    private double _enhancedHealthTicksRemaining;

    public override void Tick()
    {
        base.Tick();
        if (BodyPart.IsDestroyed)
        {
            var max = BodyPart.MaxHitPoints;
            if (max > 0)
            {
                var floor = Math.Min(1, max);
                BodyPart.HitPoints = Math.Clamp(BodyPart.HitPoints + max * DestroyPartRegenerationPercent, floor, max);
                _enhancedHealthTicksRemaining = EnhancedHealthDuration;
            }
        }

        var health = (BodyPart.Type == BodyPartType.Skin ? SkinHealthRestoredPerTick : HealthRestoredPerTick) * Power;

        if (_enhancedHealthTicksRemaining > 0)
        {
            health *= EnhancedHealthMultiplier;
            _enhancedHealthTicksRemaining--;
        }

        BodyPart.HitPoints += health;
    }

    public override bool ApplyToPart(BodyPart part)
    {
        part.TryAddModifier(this);

        return true;
    }

    public override InfoPanelData GetInfoData()
    {
        var isSkin = BodyPart?.Type == BodyPartType.Skin;
        var healRate = (isSkin ? SkinHealthRestoredPerTick : HealthRestoredPerTick) * Power;
        var enhancedRate = healRate * EnhancedHealthMultiplier;

        var lines = new List<InfoLine>
        {
            new($"Enhanced healing: +{enhancedRate:0.##} health/tick for {EnhancedHealthDuration}t after", InfoColors.Cure),
            new($"On destruction: restores {DestroyPartRegenerationPercent * 100:0}% max HP", InfoColors.Info),
            new("Prevents permanent part destruction", InfoColors.Info)
        };

        if (_enhancedHealthTicksRemaining > 0)
        {
            lines.Insert(0, new($"Enhanced healing active: {_enhancedHealthTicksRemaining:0}t remaining", InfoColors.Warning));
        }

        return new InfoPanelData
        {
            Healing = healRate,
            HealingSuffix = "health/tick",
            Lines = lines
        };
    }
}