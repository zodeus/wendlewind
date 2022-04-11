namespace Grafted.Sim.Combat;

public class CombatSequenceStep {
    public DamageRequest Damages { get; set; }
    public string Tool { get; set; } = "";
    public float VisualWaitTime { get; set; } = 0.75f;
    public string Name { get; set; } = "";
}