namespace Wendlemire.Sim.Entities.Pawns.Modifiers;

[UsedImplicitly]
public class HealthRegeneration : BodyPartModifier
{
    public HealthRegeneration(IRng rng)
    {
        Rng = rng;
    }

    private const double HealthRegenerationPerTick = .07f;
    private const float TotalTicksToRegenerate = 15;
    private int _ticksToRegenerate;

    public override void Tick()
    {
        base.Tick();
        if (BodyPart.IsDestroyed)
        {
            _ticksToRegenerate++;
            if (_ticksToRegenerate < TotalTicksToRegenerate)
            {
                return;
            }

            // Heal toward recovery instead of slamming HP to 1 every tick after the wait.
            // The first ready tick kicks a zeroed part so flat regen can climb; later ticks
            // apply the normal rate so DoT can still win without a red/green strobe.
            if (_ticksToRegenerate == TotalTicksToRegenerate
                && BodyPart.HitPoints < BodyPart.DestroyedRecoverHitPoints
                && BodyPart.DestroyedRecoverHitPoints < BodyPart.MaxHitPoints)
            {
                BodyPart.HitPoints = BodyPart.DestroyedRecoverHitPoints;
            }

            BodyPart.HitPoints += Power * HealthRegenerationPerTick;
            return;
        }

        _ticksToRegenerate = 0;
        BodyPart.HitPoints += Power * HealthRegenerationPerTick;
    }

    public override InfoPanelData GetInfoData() => new InfoPanelData
    {
        Healing = Power * HealthRegenerationPerTick,
        Lines = [new("Restores destroyed parts", InfoColors.Info)],
        ShowPower = true
    };
}