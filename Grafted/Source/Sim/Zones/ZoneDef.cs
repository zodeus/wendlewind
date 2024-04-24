namespace Grafted.Sim.Zones;

public class BiomeDef : Def {
    private Texture2D? _texture;

    public string? BackgroundTexturePath;
    public int BackgroundTextureTransparency = 20;

    public List<ZoneResourceRecord> Resources = new();

    public virtual Texture2D BackgroundTexture => _texture ??= BackgroundTexturePath != null ? Core.Content.Load<Texture2D>(BackgroundTexturePath) : BaseContent.Textures.BadTexture;
}