namespace Wendlewind.Sim.Combat;

public class DamageResponse
{
    public readonly List<DamageRecord> Damages = new();
    public readonly List<DamageRecord> TrinketDamages = new();

    public bool Dodged;
    public bool Missed;
    public double TotalDamageTaken => Damages.Sum(d => d.ActualAmount) + TrinketDamages.Sum(d => d.ActualAmount);
}