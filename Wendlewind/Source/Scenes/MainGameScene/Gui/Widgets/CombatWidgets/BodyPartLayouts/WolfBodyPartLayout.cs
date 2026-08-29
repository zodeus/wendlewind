namespace Wendlewind.Scenes.MainGameScene.Gui.Widgets.CombatWidgets.BodyPartLayouts;

/// <summary>
/// Body part layout for wolf pawns (wolves, dogs, etc.).
/// Positions are specified in a 512x512 coordinate space.
/// Wolf is viewed from the side with head on the right.
/// </summary>
public class WolfBodyPartLayout : IBodyPartLayout
{
    // Body part positions (native coordinates)
    private static readonly Dictionary<string, BodyPartLayoutData> PartLayoutMap = new()
    {
        { "Rear Right Leg", new BodyPartLayoutData(new Vector2(297f, 400f), 1, 0.58f) },
        { "Tail", new BodyPartLayoutData(new Vector2(351f, 384f), 3, 0.50f, 0.0982f) },
        { "Front Right Leg", new BodyPartLayoutData(new Vector2(135f, 420f), 8, 0.55f) },
        { "Front Right Paw", new BodyPartLayoutData(new Vector2(153f, 478f), 9, 0.28f, 0f, flipHorizontal: true) },
        { "Rear Right Paw", new BodyPartLayoutData(new Vector2(295f, 466f), 11, 0.30f, 0f, flipHorizontal: true) },
        { "Torso", new BodyPartLayoutData(new Vector2(140f, 277f), 15, 1.81f, 0.0982f) },
        { "Neck", new BodyPartLayoutData(new Vector2(121f, 320f), 17, 0.83f, -1.8162f, flipHorizontal: true) },
        { "Head", new BodyPartLayoutData(new Vector2(67f, 303f), 18, 0.99f, -0.1963f) },
        { "Left Eye", new BodyPartLayoutData(new Vector2(140f, 343f), 19, 0.13f, -0.6872f) },
        { "Right Eye", new BodyPartLayoutData(new Vector2(106f, 350f), 19, 0.13f, 0.3436f, flipHorizontal: true) },
        { "Front Left Leg", new BodyPartLayoutData(new Vector2(161f, 417f), 21, 0.58f) },
        { "Rear Left Leg", new BodyPartLayoutData(new Vector2(284f, 407f), 23, 0.55f) },
        { "Front Left Paw", new BodyPartLayoutData(new Vector2(177f, 479f), 24, 0.30f, 0f, flipHorizontal: true) },
        { "Rear Left Paw", new BodyPartLayoutData(new Vector2(313f, 465f), 48, 0.32f, 0f, flipHorizontal: true) },
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


