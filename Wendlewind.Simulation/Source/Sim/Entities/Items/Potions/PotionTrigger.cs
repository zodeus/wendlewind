namespace Wendlewind.Sim.Entities.Items.Potions;

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
            PotionTriggerType.SelfPartsDamaged => IsSelfPartsDamaged(self),
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

    private bool IsSelfPartsDamaged(Pawn self)
    {
        var externalParts = self.Body.AllExternalParts;
        var eyes = externalParts.Where(p => p.Type == BodyPartType.Eye).ToList();
        if (eyes.Count > 0 && eyes.All(e => !e.IsFunctional))
        {
            return true;
        }

        var healthThreshold = HealthThreshold > 0 ? HealthThreshold : 0.6f;
        var damagedCount = externalParts.Count(p => p.HealthPercent < healthThreshold);
        return damagedCount >= externalParts.Count * Threshold;
    }
}
