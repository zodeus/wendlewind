namespace Wendlewind.Sim.Entities.Items.Trinkets;

[UsedImplicitly]
public class BloodyBellHandler : TrinketHandler
{
    public BloodyBellHandler(IRng rng)
    {
        Rng = rng;
    }

    private const int DefaultCooldown = 300;
    private const float BaseBloodDrainPercent = 0.08f;
    private const float BloodDrainPerRing = 0.02f;
    private const float MaxBloodDrainPercent = 0.25f;
    
    
    private int _totalRings;
    
    public int TotalRings => _totalRings;
    
    public float CurrentBloodDrainPercent => Math.Min(BaseBloodDrainPercent + (_totalRings * BloodDrainPerRing), MaxBloodDrainPercent);
    
    public override void OnClick()
    {
        if (Cooldown > 0) return;
        
        if (IsActive)
        {
            DeActivate();
        }
        else
        {
            Activate();
        }
    }
    
    
    
    
    
    public override void ExposeData()
    {
        base.ExposeData();
        ScribeValues.Look(ref _totalRings, "TotalRings");
    }
}

