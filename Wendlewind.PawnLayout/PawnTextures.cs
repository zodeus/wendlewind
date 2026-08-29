namespace Wendlewind.PawnLayout;

/// <summary>
/// Texture loading for pawn body parts and equipment. Backed by
/// <see cref="RenderHost.Content"/> so both the client and editor can render
/// the same sprites without depending on client <c>Core.Content</c>.
/// </summary>
public static class PawnTextures
{
    private static readonly Dictionary<string, Texture2D> Cache = new(StringComparer.OrdinalIgnoreCase);
    private static Texture2D? _fallback;

    public static Texture2D Fallback
    {
        get
        {
            if (_fallback != null)
            {
                return _fallback;
            }

            _fallback = new Texture2D(RenderHost.GraphicsDevice, 1, 1);
            _fallback.SetData([Color.Magenta]);
            return _fallback;
        }
    }

    public static Texture2D GetTexture(EntityDef def) => Load(def.TexturePath);

    public static Texture2D GetIcon(EntityDef def)
    {
        if (def.TexturePath == null)
        {
            return Fallback;
        }

        var key = "icon:" + def.TexturePath;
        if (Cache.TryGetValue(key, out var cached))
        {
            return cached;
        }

        var texture = TextureUtils.PreMultiply(GetTexture(def)) ?? Fallback;
        Cache[key] = texture;
        return texture;
    }

    public static Texture2D GetIcon(Entity entity) => GetIcon(entity.Def);

    public static Texture2D GetIcon(BodyPart part) => GetIcon(part.Def);

    public static Texture2D GetWhiteIcon(BodyPartDef def)
    {
        if (def.WhiteIconTexturePath == null)
        {
            return GetIcon(def);
        }

        return LoadPremultiplied(def.WhiteIconTexturePath);
    }

    public static Texture2D GetWhiteIcon(BodyPart part) => GetWhiteIcon(part.BodyPartDef);

    public static Texture2D Load(string? path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return Fallback;
        }

        if (Cache.TryGetValue(path, out var cached))
        {
            return cached;
        }

        try
        {
            var texture = RenderHost.Content.Load<Texture2D>(path);
            Cache[path] = texture;
            return texture;
        }
        catch
        {
            return Fallback;
        }
    }

    public static Texture2D LoadPremultiplied(string? path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return Fallback;
        }

        var key = "pre:" + path;
        if (Cache.TryGetValue(key, out var cached))
        {
            return cached;
        }

        var texture = TextureUtils.PreMultiply(Load(path)) ?? Fallback;
        Cache[key] = texture;
        return texture;
    }
}
