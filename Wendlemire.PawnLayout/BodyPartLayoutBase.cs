namespace Wendlemire.PawnLayout;

/// <summary>
/// Shared implementation for body-type layouts that map a lookup key to <see cref="BodyPartLayoutData"/>.
/// </summary>
public abstract class BodyPartLayoutBase : IBodyPartLayout
{
    public virtual int NativeSize => 512;

    protected abstract IReadOnlyDictionary<string, BodyPartLayoutData> Map { get; }

    protected virtual string GetLookupKey(BodyPart part) => part.Label;

    public BodyPartLayoutData? GetLayoutData(BodyPart part)
    {
        return Map.TryGetValue(GetLookupKey(part), out var data) ? data : null;
    }

    public BodyPartRenderInfo? GetRenderInfo(BodyPart part)
    {
        var layoutData = GetLayoutData(part);
        if (layoutData == null)
        {
            return null;
        }

        var icon = PawnTextures.GetIcon(part);
        if (icon == null)
        {
            return null;
        }

        return new BodyPartRenderInfo(icon, layoutData.Value);
    }
}
