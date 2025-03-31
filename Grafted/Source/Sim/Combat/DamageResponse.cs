namespace Grafted.Sim.Combat;

public class DamageResponse
{
    public readonly List<DamageRecord> Damages = new();
    public readonly List<DamageRecord> TrinketDamages = new();

    public bool Dodged;
    public bool Missed;
    public double TotalDamage => Damages.Sum(d => d.TotalDamage) + TrinketDamages.Sum(d => d.TotalDamage);
    public double ActualDamage => Damages.Sum(d => d.ActualAmount) + TrinketDamages.Sum(d => d.ActualAmount);
}