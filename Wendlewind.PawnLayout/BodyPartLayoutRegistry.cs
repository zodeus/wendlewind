namespace Wendlewind.PawnLayout;

/// <summary>
/// Resolves the layout for a pawn body. Prefers an authored
/// <see cref="BodyPartLayoutDef"/>; otherwise instantiates the body's
/// <see cref="BodyDef.LayoutClass"/> (or a type named {Moniker}PartLayout
/// in this assembly).
/// </summary>
public static class BodyPartLayoutRegistry
{
    private static readonly Dictionary<string, Type> TypesByName = typeof(BodyPartLayoutRegistry).Assembly
        .GetTypes()
        .Where(t => typeof(IBodyPartLayout).IsAssignableFrom(t) && !t.IsAbstract && t.GetConstructor(Type.EmptyTypes) != null)
        .ToDictionary(t => t.Name, t => t, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Forces this assembly (and every layout type) into the load context so
    /// <c>GenTypes</c> can resolve <c>LayoutClass</c> names from XML.
    /// Call before <c>DataLoader.Load()</c>.
    /// </summary>
    public static void EnsureLoaded()
    {
        _ = TypesByName.Count;
    }

    public static IBodyPartLayout? GetLayoutFor(PawnBody body) => GetLayoutFor(body.Def);

    public static IBodyPartLayout? GetLayoutFor(BodyDef body)
    {
        var authored = BodyPartLayoutDef.ForBody(body);
        if (authored != null)
        {
            return new XmlBodyPartLayout(authored);
        }

        var type = body.LayoutClass ?? ResolveCodeType(body);
        if (type == null)
        {
            return null;
        }

        return (IBodyPartLayout)Activator.CreateInstance(type)!;
    }

    private static Type? ResolveCodeType(BodyDef body)
    {
        if (TypesByName.TryGetValue(body.Moniker + "PartLayout", out var byMoniker))
        {
            return byMoniker;
        }

        return null;
    }
}
