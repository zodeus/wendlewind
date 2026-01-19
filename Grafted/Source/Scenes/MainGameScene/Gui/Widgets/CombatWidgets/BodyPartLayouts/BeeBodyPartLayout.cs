namespace Grafted.Scenes.MainGameScene.Gui.Widgets.CombatWidgets.BodyPartLayouts;

/// <summary>
/// Body part layout for bee pawns.
/// Positions are specified in a 512x512 coordinate space.
/// Keys use InternalLabel format (Moniker_Position) for stable lookups.
/// </summary>
public class BeeBodyPartLayout : IBodyPartLayout
{
    // Body part positions (native coordinates) - keyed by InternalLabel
    private static readonly Dictionary<string, BodyPartLayoutData> PartLayoutMap = new()
    {
        { "BeeLeg_MiddleRight", new BodyPartLayoutData(new Vector2(169f, 357f), 0, 0.75f, 0.5236f) },
        { "BeeLeg_FrontRight", new BodyPartLayoutData(new Vector2(116f, 325f), 0, 0.75f, 0.3491f, flipHorizontal: true) },
        { "BeeLeg_RearRight", new BodyPartLayoutData(new Vector2(211f, 377f), 0, 0.75f, 0.6981f) },
        { "BeeAbdomen_AbdomenSocket", new BodyPartLayoutData(new Vector2(245f, 277f), 16, 1.40f, -0.2269f) },
        { "BeeWing_Left", new BodyPartLayoutData(new Vector2(150f, 175f), 18, 1.05f, 0.6283f, flipHorizontal: true) },
        { "BeeThorax_ThoraxSocket", new BodyPartLayoutData(new Vector2(132f, 261f), 20, 1.20f, 0.0698f) },
        { "BeeLeg_RearLeft", new BodyPartLayoutData(new Vector2(237f, 374f), 21, 0.80f, -0.0873f) },
        { "BeeLeg_MiddleLeft", new BodyPartLayoutData(new Vector2(162f, 378f), 25, 0.80f, 0.8378f) },
        { "BeeWing_Right", new BodyPartLayoutData(new Vector2(208f, 154f), 30, 1.40f, -0.2618f) },
        { "Eye_Right", new BodyPartLayoutData(new Vector2(93f, 264f), 31, 0.40f, -0.0873f) },
        { "BeeLeg_FrontLeft", new BodyPartLayoutData(new Vector2(119f, 365f), 34, 0.80f, -0.2443f, flipHorizontal: true) },
        { "BeeHead_HeadSocket", new BodyPartLayoutData(new Vector2(59f, 206f), 40, 1.45f, 0.3665f) },
        { "BeeAntenna_Right", new BodyPartLayoutData(new Vector2(49f, 146f), 45, 1.05f, -0.5061f, flipHorizontal: true) },
        { "BeeAntenna_Left", new BodyPartLayoutData(new Vector2(83f, 151f), 45, 1.05f, -0.0524f, flipHorizontal: true) },
        { "Eye_Left", new BodyPartLayoutData(new Vector2(124f, 265f), 50, 0.45f, -1.0123f) },
        { "BeeDrone_M1", new BodyPartLayoutData(new Vector2(184f, 32f), 55, 0.70f, 0.1396f) },
        { "BeeDrone_M2", new BodyPartLayoutData(new Vector2(36f, 398f), 55, 0.55f, 0.0349f) },
        { "BeeDrone_M3", new BodyPartLayoutData(new Vector2(46f, 36f), 55, 0.60f, -0.3665f) },
        { "BeeDrone_M4", new BodyPartLayoutData(new Vector2(391f, 239f), 55, 0.50f, 0.2094f) },
    };


    public int NativeSize => 512;

    public BodyPartRenderInfo? GetRenderInfo(BodyPart part)
    {
        if (!PartLayoutMap.TryGetValue(part.InternalLabel, out var layoutData))
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

