namespace Grafted.Sim.Zones;

public class ZoneDef : Def {
    private Texture2D? _texture;

    public int Stage;
    public string? BackgroundTexturePath;
    public int BackgroundTextureTransparency = 20;
    public Color BiomeColor = new(150, 150, 150);

    public List<BiomeResourceRecord> Resources = new();
    public List<EncounterProperties> Encounters = new();
    public List<WeatherDef> Weathers = new();

    public virtual Texture2D BackgroundTexture => _texture ??= BackgroundTexturePath != null ? Core.Content.Load<Texture2D>(BackgroundTexturePath) : BaseContent.Textures.BadTexture;
}
