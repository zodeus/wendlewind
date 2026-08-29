namespace Wendlewind.Scenes.MainGameScene.Gui.Widgets.CombatWidgets.BodyPartLayouts;

/// <summary>
/// Body part layout for marionette pawns.
/// Positions are specified in a 512x512 coordinate space.
/// Marionette is a humanoid puppet with no neck.
/// </summary>
[UsedImplicitly]
public class MarionetteBodyPartLayout : IBodyPartLayout
{
    // Body part positions (native coordinates)
    private static readonly Dictionary<string, BodyPartLayoutData> PartLayoutMap = new()
    {
        { "Left Foot", new BodyPartLayoutData(new Vector2(280f, 440f), 0, 0.45f, 0.0349f, flipHorizontal: true) },
        { "Right Foot", new BodyPartLayoutData(new Vector2(161f, 446f), 0, 0.45f, 0.0175f) },
        { "Right Arm", new BodyPartLayoutData(new Vector2(126f, 207f), 14, 1.00f, -0.5236f) },
        { "Left Arm", new BodyPartLayoutData(new Vector2(251f, 207f), 20, 1.00f, 0.5236f, flipHorizontal: true) },
        { "Left Leg", new BodyPartLayoutData(new Vector2(223f, 314f), 21, 1.05f, 0.2443f, flipHorizontal: true) },
        { "Right Leg", new BodyPartLayoutData(new Vector2(138f, 313f), 24, 1.10f, -0.2443f) },
        { "Left Hand", new BodyPartLayoutData(new Vector2(334f, 306f), 30, 0.65f, -0.3142f, flipHorizontal: true) },
        { "Torso", new BodyPartLayoutData(new Vector2(176f, 187f), 32, 1.15f) },
        { "Head", new BodyPartLayoutData(new Vector2(192f, 111f), 38, 0.90f, -0.0524f) },
        { "Left Eye", new BodyPartLayoutData(new Vector2(243f, 157f), 52, 0.12f, 0.1047f) },
        { "Right Hand", new BodyPartLayoutData(new Vector2(93f, 304f), 59, 0.60f, 0.4189f) },
        { "Right Eye", new BodyPartLayoutData(new Vector2(218f, 159f), 66, 0.12f, 0.0524f) },
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
