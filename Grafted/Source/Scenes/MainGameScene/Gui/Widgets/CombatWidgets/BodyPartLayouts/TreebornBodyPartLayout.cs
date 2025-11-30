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
        { "Rear Right Leg Stump", new BodyPartLayoutData(new Vector2(249f, 398f), 0, 0.85f, -0.5585f) },
        { "Front Right Leg Stump", new BodyPartLayoutData(new Vector2(130f, 398f), 2, 0.80f, 0.3927f, flipHorizontal: true) },
        { "Front Right Arm Stump", new BodyPartLayoutData(new Vector2(112f, 328f), 2, 0.80f, -1.8151f, flipHorizontal: true, flipVertical: true) },
        { "Rear Right Arm Stump", new BodyPartLayoutData(new Vector2(141f, 291f), 12, 0.85f, 1.7279f) },
        { "Trunk", new BodyPartLayoutData(new Vector2(127f, 171f), 18, 1.95f) },
        { "Left Eye", new BodyPartLayoutData(new Vector2(236f, 247f), 20, 0.30f) },
        { "Right Eye", new BodyPartLayoutData(new Vector2(185f, 251f), 20, 0.25f, -0.1396f, flipHorizontal: true) },
        { "Front Left Leg Stump", new BodyPartLayoutData(new Vector2(160f, 394f), 23, 0.90f, 0.2094f) },
        { "Front Left Arm Stump", new BodyPartLayoutData(new Vector2(138f, 347f), 23, 0.90f, 1.1345f) },
        { "Rear Left Leg Stump", new BodyPartLayoutData(new Vector2(210f, 394f), 24, 0.90f) },
        { "Rear Left Arm Stump", new BodyPartLayoutData(new Vector2(178f, 328f), 24, 0.90f, 1.6930f) },
    };



    public int NativeSize => 512;

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

