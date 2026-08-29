namespace Wendlewind.Sim.Entities.Pawns.Bodies.Handlers;

/// <summary>
/// A body handler that provides powerful regeneration for wearbears.
/// Regeneration is stronger when body parts are more damaged.
/// </summary>
public class WearbearBodyHandler : DefaultBodyHandler
{
    public WearbearBodyHandler(IRng rng)
    {
        Rng = rng;
    }

    public override void Tick()
    {
        base.Tick();
        RegenerateBodyParts();
    }

    private void RegenerateBodyParts()
    {
        foreach (var part in Body.AllParts)
        {
            if (part.IsDestroyed)
            {
                continue;
            }

            var healthPercent = part.HealthPercent;
            var regenAmount = GetRegenerationAmount(healthPercent);

            if (regenAmount > 0)
            {
                part.HitPoints += regenAmount;
            }
        }
    }

    private static float GetRegenerationAmount(double healthPercent)
    {
        return healthPercent switch
        {
            > 0.80 => 0.02f,
            > 0.50 => 0.04f,
            > 0.15 => .08f,
            > 0.01 => 1.4f,
            _ => 0f
        };
    }
}
