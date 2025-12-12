namespace Grafted.Sim;

public class World : IExposable, IIdentityProvider
{
    public Player Player = null!;

    public List<Zone> Zones = [];
    public Zone GetZone(ZoneDef zoneDef) => Zones.First(z => z.ZoneDef == zoneDef);

    public void Initialize(Player player, IReadOnlyList<ZoneDef> zoneDefs)
    {
        Player = player;
        foreach (var zoneDef in zoneDefs.OrderBy(z => z.Stage))
        {
            var zone = new Zone();
            zone.Initialize(zoneDef);
            Zones.Add(zone);
        }
    }

    public void Reset()
    {
        Zones.ForEach(z =>
        { 
            z.ActiveEncounter = null;
            z.IsComplete = false;
            z.Stage = 0;
        });
    }

    public void ExposeData()
    {
        ScribeDeep.Look(ref Player!, "Player");
        ScribeCollections.Look(ref Zones!, "Zones", LookMode.Deep);
    }

    public string GetUniqueId()
    {
        return "world";
    }
}