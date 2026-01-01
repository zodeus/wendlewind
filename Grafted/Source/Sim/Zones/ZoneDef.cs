namespace Grafted.Sim.Zones;

public class ZoneDef : Def {
    private Texture2D? _texture;
    private Texture2D? _iconTexture;

    public int Stage;
    public Color ZoneColor = new(150, 150, 150);

    public List<BiomeResourceRecord> Resources = new();
    public List<EncounterProperties> Encounters = new();
    public List<WeatherDef> Weathers = new();

    public virtual Texture2D BackgroundTexture => _texture ??=  Core.Content.Load<Texture2D>("Zones/" + Moniker);
    public virtual Texture2D IconTexture => _iconTexture ??= Core.Content.Load<Texture2D>("Zones/Icons/" + Moniker);
}
