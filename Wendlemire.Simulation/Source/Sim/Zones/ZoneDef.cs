namespace Wendlemire.Sim.Zones;

public class ZoneDef : Def {
    public int Stage;
    public Color ZoneColor = new(150, 150, 150);

    public List<BiomeResourceRecord> Resources = new();
    public List<EncounterProperties> Encounters = new();
    public List<WeatherDef> Weathers = new();
}
