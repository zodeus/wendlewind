namespace Grafted.Sim;

public class World : IExposable, IIdentityProvider
{
    public Player Player = null!;
    public int TotalKills;
    public List<Zone> Zones = [];
    public Zone GetZone(ZoneDef zoneDef) => Zones.First(z => z.ZoneDef == zoneDef);

    public void Initialize(Player player, IReadOnlyList<ZoneDef> zoneDefs)
    {
        Player = player;
        TotalKills = 0;
        foreach (var zoneDef in zoneDefs.OrderBy(z => z.Stage))
        {
            var zone = new Zone();
            zone.Initialize(zoneDef);
            Zones.Add(zone);
        }
    }

    public void RegisterKill(Pawn pawnKilled)
    {
        TotalKills++;
    }

    public void ExposeData()
    {
        ScribeDeep.Look(ref Player!, "Player");
        ScribeValues.Look(ref TotalKills, "TotalKills");
        ScribeCollections.Look(ref Zones!, "Zones", LookMode.Deep);
    }

    public string GetUniqueId()
    {
        return "world";
    }
}