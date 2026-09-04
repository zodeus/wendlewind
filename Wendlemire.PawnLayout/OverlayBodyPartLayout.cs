namespace Wendlemire.PawnLayout;

/// <summary>
/// Places implants flagged <see cref="BodyPartDef.ShowOnPawnBody"/> on the
/// nearest laid-out ancestor (usually the torso) when they have no cell of their own.
/// </summary>
public static class OverlayBodyPartLayout
{
    public const float DefaultScale = 0.26f;

    public static IEnumerable<BodyPart> VisibleParts(PawnBody body)
    {
        foreach (var part in body.AllParts)
        {
            if (!part.IsSevered && (part.IsExternal || part.BodyPartDef.ShowOnPawnBody))
            {
                yield return part;
            }
        }
    }

    public static BodyPartLayoutData? Resolve(
        BodyPart part,
        Func<BodyPart, BodyPartLayoutData?> exactLookup)
    {
        var exact = exactLookup(part);
        if (exact != null)
        {
            return exact;
        }

        if (!part.BodyPartDef.ShowOnPawnBody)
        {
            return null;
        }

        for (var host = part.Socket?.ParentPart; host != null; host = host.Socket?.ParentPart)
        {
            var hostLayout = exactLookup(host);
            if (hostLayout != null)
            {
                return OverlayOn(hostLayout.Value, part);
            }
        }

        var torso = part.Body?.AllExternalParts.FirstOrDefault(p => p.Type == BodyPartType.Torso);
        if (torso == null)
        {
            return null;
        }

        var torsoLayout = exactLookup(torso);
        return torsoLayout == null ? null : OverlayOn(torsoLayout.Value, part);
    }

    private static BodyPartLayoutData OverlayOn(BodyPartLayoutData host, BodyPart part)
    {
        var offsetX = part.Position == BodyPartPosition.Right ? 24f : 44f;
        return host with
        {
            Position = host.Position + new Vector2(offsetX, 24f),
            RenderOrder = host.RenderOrder + 1,
            ScaleMultiplier = DefaultScale,
            Rotation = 0f,
            FlipHorizontal = false,
            FlipVertical = false,
            EquipmentAttachment = null
        };
    }
}
