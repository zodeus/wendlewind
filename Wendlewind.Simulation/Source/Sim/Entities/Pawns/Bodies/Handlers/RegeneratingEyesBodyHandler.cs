namespace Wendlewind.Sim.Entities.Pawns.Bodies.Handlers;

/// <summary>
/// A body handler that allows eyes to regenerate over time.
/// Used for creatures like horses that have naturally regenerating eyes.
/// </summary>
public class RegeneratingEyesBodyHandler : DefaultBodyHandler
{
    public RegeneratingEyesBodyHandler(IRng rng)
    {
        Rng = rng;
    }

    private const float RegenerationAmount = 0.005f; // HP regenerated per interval
    
    public override void Tick()
    {
        base.Tick();        
        RegenerateEyes();
    }
    
    private void RegenerateEyes()
    {
        foreach (var part in Body.AllParts)
        {
            if (part.Type != BodyPartType.Eye)
            {
                continue;
            }
            
            part.HitPoints = Math.Min(part.HitPoints + RegenerationAmount, part.MaxHitPoints);
        }
    }
}
