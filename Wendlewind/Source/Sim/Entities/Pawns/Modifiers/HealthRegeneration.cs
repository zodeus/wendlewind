namespace Wendlewind.Sim.Entities.Pawns.Modifiers;

[UsedImplicitly]
public class HealthRegeneration : BodyPartModifier
{
    private const double HealthRegenerationPerTick = .1f;
    private const float TotalTicksToRegenerate = 15;
    private int _ticksToRegenerate;

    public override void Tick()
    {
        base.Tick();
        if (BodyPart.IsDestroyed)
        {
            _ticksToRegenerate++;
            if (_ticksToRegenerate >= TotalTicksToRegenerate)
            {
                BodyPart.HitPoints = 1;
            }
            return;
        }
        
        var health = Power * HealthRegenerationPerTick;
        BodyPart.HitPoints += health;
    }

    public override Widget? GetInfoPanel() => BuildInfoPanel(new InfoPanelData
    {
        Healing = Power * HealthRegenerationPerTick,
        Lines = [new("Restores destroyed parts", InfoColors.Info)],
        ShowPower = true
    });
}