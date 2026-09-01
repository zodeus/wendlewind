using Wendlemire.Graphics.Textures;
using Wendlemire.Sim.LootBoxes;
using Wendlemire.Sim.Zones;

namespace Wendlemire.Presentation;

public static class EntityVisuals
{
    private static readonly Dictionary<string, Texture2D> Cache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, IImage> IconImageCache = new(StringComparer.OrdinalIgnoreCase);

    public static Texture2D GetTexture(this EntityDef def) => Load(def.TexturePath);

    public static Texture2D GetIcon(this EntityDef def)
    {
        if (def.TexturePath == null)
        {
            return BaseContent.Textures.BadTexture;
        }

        var key = "icon:" + def.TexturePath;
        if (Cache.TryGetValue(key, out var cached))
        {
            return cached;
        }

        var texture = TextureUtils.PreMultiply(GetTexture(def)) ?? BaseContent.Textures.BadTexture;
        Cache[key] = texture;
        return texture;
    }

    public static Texture2D GetIcon(this Entity entity) => entity.Def.GetIcon();

    public static IImage GetIconImage(this EntityDef def)
    {
        if (def.TexturePath == null)
        {
            return new TextureRegion(BaseContent.Textures.BadTexture);
        }

        var key = "iconImage:" + def.TexturePath;
        if (IconImageCache.TryGetValue(key, out var cached))
        {
            return cached;
        }

        var frames = DiscoverIconFrames(def);
        IImage image = frames.Count <= 1
            ? new TextureRegion(frames[0])
            : new AnimatedTextureRegion(frames.ToArray(), def.IconFrameRate, def.IconPingPong);
        IconImageCache[key] = image;
        return image;
    }

    public static IImage GetIconImage(this Entity entity) => entity.Def.GetIconImage();

    private static List<Texture2D> DiscoverIconFrames(EntityDef def)
    {
        var frames = new List<Texture2D> { def.GetIcon() };
        for (var i = 1; ; i++)
        {
            if (!TryLoadPremultiplied(def.TexturePath + "_" + i, out var frame))
            {
                break;
            }

            frames.Add(frame);
        }

        return frames;
    }

    public static Texture2D GetWhiteIcon(this BodyPartDef def)
    {
        if (def.WhiteIconTexturePath == null)
        {
            return def.GetIcon();
        }

        return LoadPremultiplied(def.WhiteIconTexturePath);
    }

    public static Texture2D GetWhiteIcon(this BodyPart part) => part.BodyPartDef.GetWhiteIcon();

    public static Texture2D GetIcon(this BodyPart part) => part.Def.GetIcon();

    public static Texture2D GetTexture(this BodyEffectDef def) => LoadPremultiplied(def.TexturePath);

    public static Texture2D GetTexture(this BodyStanceDef def) => LoadPremultiplied(def.TexturePath);

    public static Texture2D GetIcon(this LootBoxDef def) => LoadPremultiplied(def.TexturePath);

    public static Texture2D GetBackground(this ZoneDef def) => Load("Zones/" + def.Moniker);

    public static Texture2D GetIcon(this ZoneDef def) => Load("Zones/Icons/" + def.Moniker);

    public static Texture2D Load(string? path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return BaseContent.Textures.BadTexture;
        }

        if (Cache.TryGetValue(path, out var cached))
        {
            return cached;
        }

        var texture = Core.Content.Load<Texture2D>(path) ?? BaseContent.Textures.BadTexture;
        Cache[path] = texture;
        return texture;
    }

    public static Texture2D LoadPremultiplied(string? path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return BaseContent.Textures.BadTexture;
        }

        var key = "pre:" + path;
        if (Cache.TryGetValue(key, out var cached))
        {
            return cached;
        }

        var texture = TextureUtils.PreMultiply(Load(path)) ?? BaseContent.Textures.BadTexture;
        Cache[key] = texture;
        return texture;
    }

    private static bool TryLoadPremultiplied(string? path, out Texture2D texture)
    {
        if (string.IsNullOrEmpty(path))
        {
            texture = null!;
            return false;
        }

        var key = "pre:" + path;
        if (Cache.TryGetValue(key, out var cached))
        {
            texture = cached;
            return true;
        }

        if (!Core.Content.TryLoad<Texture2D>(path, out var loaded) || loaded == null)
        {
            texture = null!;
            return false;
        }

        texture = TextureUtils.PreMultiply(loaded) ?? loaded;
        Cache[key] = texture;
        return true;
    }
}
