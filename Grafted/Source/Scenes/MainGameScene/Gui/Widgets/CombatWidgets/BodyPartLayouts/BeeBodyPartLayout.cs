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
        { "Middle Right Leg", new BodyPartLayoutData(new Vector2(169f, 357f), 0, 0.75f, 0.5236f) },
        { "Front Right Leg", new BodyPartLayoutData(new Vector2(116f, 325f), 0, 0.75f, 0.3491f, flipHorizontal: true) },
        { "Rear Right Leg", new BodyPartLayoutData(new Vector2(211f, 377f), 0, 0.75f, 0.6981f) },
        { "Abdomen", new BodyPartLayoutData(new Vector2(245f, 277f), 16, 1.40f, -0.2269f) },
        { "Left Wing", new BodyPartLayoutData(new Vector2(150f, 175f), 18, 1.05f, 0.6283f, flipHorizontal: true) },
        { "Thorax", new BodyPartLayoutData(new Vector2(132f, 261f), 20, 1.20f, 0.0698f) },
        { "Rear Left Leg", new BodyPartLayoutData(new Vector2(237f, 374f), 21, 0.80f, -0.0873f) },
        { "Middle Left Leg", new BodyPartLayoutData(new Vector2(162f, 378f), 25, 0.80f, 0.8378f) },
        { "Right Wing", new BodyPartLayoutData(new Vector2(208f, 154f), 30, 1.40f, -0.2618f) },
        { "Right Eye", new BodyPartLayoutData(new Vector2(93f, 264f), 31, 0.40f, -0.0873f) },
        { "Front Left Leg", new BodyPartLayoutData(new Vector2(119f, 365f), 34, 0.80f, -0.2443f, flipHorizontal: true) },
        { "Head", new BodyPartLayoutData(new Vector2(59f, 206f), 40, 1.45f, 0.3665f) },
        { "Right Antenna", new BodyPartLayoutData(new Vector2(49f, 146f), 45, 1.05f, -0.5061f, flipHorizontal: true) },
        { "Left Antenna", new BodyPartLayoutData(new Vector2(83f, 151f), 45, 1.05f, -0.0524f, flipHorizontal: true) },
        { "Left Eye", new BodyPartLayoutData(new Vector2(124f, 265f), 50, 0.45f, -1.0123f) },
        { "A Drone", new BodyPartLayoutData(new Vector2(184f, 32f), 55, 0.70f, 0.1396f) },
        { "B Drone", new BodyPartLayoutData(new Vector2(36f, 398f), 55, 0.55f, 0.0349f) },
        { "C Drone", new BodyPartLayoutData(new Vector2(46f, 36f), 55, 0.60f, -0.3665f) },
        { "D Drone", new BodyPartLayoutData(new Vector2(391f, 239f), 55, 0.50f, 0.2094f) },
    };


    public int NativeSize => 512;

    public BodyPartRenderInfo? GetRenderInfo(BodyPart part)
    {
        //Log.Info(part.Label);
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

