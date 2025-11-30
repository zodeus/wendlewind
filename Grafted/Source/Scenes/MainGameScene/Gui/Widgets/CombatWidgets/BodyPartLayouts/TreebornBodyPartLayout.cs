namespace Grafted.Scenes.MainGameScene.Gui.Widgets.CombatWidgets.BodyPartLayouts;

/// <summary>
/// Body part layout for treeborn pawns.
/// Positions are specified in a 512x512 coordinate space.
/// Tree is viewed from the front with trunk in center and stumps as legs.
/// </summary>
public class TreebornBodyPartLayout : IBodyPartLayout
{
    // Body part positions (native coordinates)
    private static readonly Dictionary<string, BodyPartLayoutData> PartLayoutMap = new()
    {
        { "Rear Right Stump", new BodyPartLayoutData(new Vector2(285f, 394f), 0, 0.85f, -0.5585f) },
        { "Front Right Stump", new BodyPartLayoutData(new Vector2(180f, 391f), 2, 0.80f, 0.3927f, flipHorizontal: true) },
        { "Trunk", new BodyPartLayoutData(new Vector2(178f, 188f), 15, 1.80f) },
        { "Left Eye", new BodyPartLayoutData(new Vector2(277f, 257f), 20, 0.30f) },
        { "Right Eye", new BodyPartLayoutData(new Vector2(229f, 259f), 20, 0.25f, -0.1396f, flipHorizontal: true) },
        { "Front Left Stump", new BodyPartLayoutData(new Vector2(199f, 393f), 23, 0.90f, 0.2094f) },
        { "Rear Left Stump", new BodyPartLayoutData(new Vector2(251f, 395f), 24, 0.90f) },
    };


    public int NativeSize => 512;

    public bool SupportsBody(PawnBody body)
    {
        // Check if this is a treeborn body by looking at the torso
        var torso = body.AllExternalParts.FirstOrDefault(p => p.Type == BodyPartType.Torso);
        if (torso?.BodyPartDef.Moniker == "TreeTrunk")
        {
            return true;
        }

        return false;
    }

    public BodyPartRenderInfo? GetRenderInfo(BodyPart part)
    {
        if (!PartLayoutMap.TryGetValue(part.Label, out var layoutData))
        {
            return null;
        }

        if (part.Image == null)
        {
            return null;
        }

        return new BodyPartRenderInfo(part.Image, layoutData);
    }
}

