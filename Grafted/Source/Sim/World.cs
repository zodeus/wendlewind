namespace Grafted.Sim;

public class World : IExposable, IIdentityProvider
{
    public Player Player = null!;
    public int TotalKills;
    public List<Zone> Zones = [];
    public Zone GetZone(BiomeDef biome) => Zones.First(z => z.BiomeDef == biome);

    public void Initialize(Player player, IReadOnlyList<BiomeDef> biomeDefs)
    {
        Player = player;
        TotalKills = 0;
        foreach (var biomeDef in biomeDefs)
        {
            var zone = new Zone();
            zone.Initialize(biomeDef);
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