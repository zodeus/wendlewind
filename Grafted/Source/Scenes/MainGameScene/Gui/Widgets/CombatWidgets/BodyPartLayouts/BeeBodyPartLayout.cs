namespace Grafted.Scenes.MainGameScene.Gui.Widgets.CombatWidgets.BodyPartLayouts;

/// <summary>
/// Body part layout for bee pawns.
/// Positions are specified in a 512x512 coordinate space.
/// </summary>
public class BeeBodyPartLayout : IBodyPartLayout
{
    // Body part positions (native coordinates)
    private static readonly Dictionary<string, BodyPartLayoutData> PartLayoutMap = new()
    {
        { "Front Right Leg", new BodyPartLayoutData(new Vector2(116f, 325f), 0, 0.75f, 0.3491f, flipHorizontal: true) },
        { "Middle Right Leg", new BodyPartLayoutData(new Vector2(169f, 357f), 0, 0.75f, 0.5236f) },
        { "Rear Right Leg", new BodyPartLayoutData(new Vector2(211f, 377f), 0, 0.75f, 0.6981f) },
        { "Abdomen", new BodyPartLayoutData(new Vector2(235f, 287f), 16, 1.40f, 0.1745f) },
        { "Left Wing", new BodyPartLayoutData(new Vector2(150f, 175f), 18, 1.05f, 0.6283f, flipHorizontal: true) },
        { "Thorax", new BodyPartLayoutData(new Vector2(132f, 261f), 20, 1.20f, 0.0698f) },
        { "Rear Left Leg", new BodyPartLayoutData(new Vector2(237f, 374f), 21, 0.80f, -0.0873f) },
        { "Middle Left Leg", new BodyPartLayoutData(new Vector2(162f, 378f), 25, 0.80f, 0.8378f) },
        { "Right Wing", new BodyPartLayoutData(new Vector2(208f, 154f), 30, 1.40f, -0.2618f) },
        { "Right Eye", new BodyPartLayoutData(new Vector2(97f, 244f), 31, 0.75f, -0.0873f) },
        { "Front Left Leg", new BodyPartLayoutData(new Vector2(119f, 365f), 34, 0.80f, -0.2443f, flipHorizontal: true) },
        { "Head", new BodyPartLayoutData(new Vector2(59f, 206f), 40, 1.45f, 0.3665f) },
        { "Left Antenna", new BodyPartLayoutData(new Vector2(83f, 151f), 45, 1.05f, -0.0524f, flipHorizontal: true) },
        { "Right Antenna", new BodyPartLayoutData(new Vector2(49f, 146f), 45, 1.05f, -0.5061f, flipHorizontal: true) },
        { "Left Eye", new BodyPartLayoutData(new Vector2(138f, 258f), 50, 0.75f, 0.0698f) },
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

