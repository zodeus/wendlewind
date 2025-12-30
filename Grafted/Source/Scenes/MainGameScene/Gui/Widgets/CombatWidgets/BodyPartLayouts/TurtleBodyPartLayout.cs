namespace Grafted.Scenes.MainGameScene.Gui.Widgets.CombatWidgets.BodyPartLayouts;

/// <summary>
/// Body part layout for turtle pawns.
/// Positions are specified in a 512x512 coordinate space.
/// Turtle is viewed from above/side with head on the left.
/// </summary>
public class TurtleBodyPartLayout : IBodyPartLayout
{
    // Body part positions (native coordinates)
    private static readonly Dictionary<string, BodyPartLayoutData> PartLayoutMap = new()
    {
        { "Rear Right Leg", new BodyPartLayoutData(new Vector2(295f, 390f), 1, 0.70f, -0.2269f) },
        { "Tail", new BodyPartLayoutData(new Vector2(292f, 336f), 5, 0.95f, -0.9076f, flipHorizontal: true) },
        { "Front Right Flipper", new BodyPartLayoutData(new Vector2(107f, 389f), 6, 0.70f, 1.3439f) },
        { "Shell", new BodyPartLayoutData(new Vector2(133f, 232f), 10, 2.00f, 0.1396f) },
        { "Left Eye", new BodyPartLayoutData(new Vector2(110f, 320f), 13, 0.12f) },
        { "Front Left Flipper", new BodyPartLayoutData(new Vector2(161f, 397f), 16, 0.75f, 0.4000f, flipHorizontal: true) },
        { "Rear Left Leg", new BodyPartLayoutData(new Vector2(252f, 397f), 16, 0.75f, 0.3000f) },
        { "Neck", new BodyPartLayoutData(new Vector2(135f, 321f), 18, 0.65f, 0.1000f) },
        { "Head", new BodyPartLayoutData(new Vector2(106f, 304f), 20, 0.70f, 0.0698f) },
        { "Right Eye", new BodyPartLayoutData(new Vector2(143f, 331f), 25, 0.12f, -0.7679f) },
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

