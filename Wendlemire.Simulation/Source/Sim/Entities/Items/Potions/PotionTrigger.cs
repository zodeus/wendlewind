namespace Wendlemire.Sim.Entities.Items.Potions;

public class PotionTrigger : IExposable
{
    public PotionTriggerType Type;
    public float Threshold;
    public float AfterSeconds;
    public float HealthThreshold = 0.6f;

    public PotionTrigger Clone()
    {
        return new PotionTrigger
        {
            Type = Type,
            Threshold = Threshold,
            AfterSeconds = AfterSeconds,
            HealthThreshold = HealthThreshold
        };
    }

    public bool ShouldFire(Pawn self, Pawn enemy, int tick)
    {
        return Type switch
        {
            PotionTriggerType.Immediately => true,
            PotionTriggerType.AfterSeconds => tick >= AfterSeconds * GameContext.TicksPerSecond,
            PotionTriggerType.SelfBloodBelow => self.Body.BloodPercent < Threshold,
            PotionTriggerType.EnemyBloodBelow => enemy.Body.BloodPercent < Threshold,
            PotionTriggerType.SelfPartsDamaged => self.Body.IsSelfPartsDamaged(Threshold, HealthThreshold),
            _ => false
        };
    }

    public string Describe()
    {
        return Type switch
        {
            PotionTriggerType.Immediately => "Use immediately",
            PotionTriggerType.AfterSeconds => $"Use after {AfterSeconds:0.##}s",
            PotionTriggerType.SelfBloodBelow => $"Use when own blood < {Threshold * 100:0}%",
            PotionTriggerType.EnemyBloodBelow => $"Use when enemy blood < {Threshold * 100:0}%",
            PotionTriggerType.SelfPartsDamaged =>
                $"Use when {Threshold * 100:0}% of parts are below {HealthThreshold * 100:0}% health",
            _ => Type.ToString()
        };
    }

    public void ExposeData()
    {
        ScribeValues.Look(ref Type, "Type");
        ScribeValues.Look(ref Threshold, "Threshold");
        ScribeValues.Look(ref AfterSeconds, "AfterSeconds");
        ScribeValues.Look(ref HealthThreshold, "HealthThreshold", 0.6f);
    }

}
