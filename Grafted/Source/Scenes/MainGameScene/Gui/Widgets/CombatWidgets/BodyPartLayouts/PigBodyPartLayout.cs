namespace Grafted.Scenes.MainGameScene.Gui.Widgets.CombatWidgets.BodyPartLayouts;

/// <summary>
/// Body part layout for pig pawns (boars, pigs, etc.).
/// Positions are specified in a 512x512 coordinate space.
/// Pig is viewed from the side with head on the right.
/// </summary>
public class PigBodyPartLayout : IBodyPartLayout
{
    // Body part positions (native coordinates)
    private static readonly Dictionary<string, BodyPartLayoutData> PartLayoutMap = new()
    {
        { "Rear Right Leg", new BodyPartLayoutData(new Vector2(258f, 260f), 1, 0.56f, 0f, flipHorizontal: true) },
        { "Rear Right Hoof", new BodyPartLayoutData(new Vector2(299f, 313f), 3, 0.26f) },
        { "Tail", new BodyPartLayoutData(new Vector2(313f, 255f), 4, 0.40f) },
        { "Rear Left Leg", new BodyPartLayoutData(new Vector2(248f, 277f), 7, 0.52f, 0f, flipHorizontal: true) },
        { "Rear Left Hoof", new BodyPartLayoutData(new Vector2(274f, 320f), 9, 0.28f) },
        { "Front Right Leg", new BodyPartLayoutData(new Vector2(157f, 251f), 10, 0.52f, 0f, flipHorizontal: true) },
        { "Front Left Leg", new BodyPartLayoutData(new Vector2(181f, 248f), 15, 0.56f, 0f, flipHorizontal: true) },
        { "Front Right Hoof", new BodyPartLayoutData(new Vector2(193f, 302f), 15, 0.25f) },
        { "Front Left Hoof", new BodyPartLayoutData(new Vector2(217f, 300f), 17, 0.29f) },
        { "Torso", new BodyPartLayoutData(new Vector2(174f, 168f), 18, 1.27f, 0f, flipHorizontal: true) },
        { "Neck", new BodyPartLayoutData(new Vector2(171f, 193f), 19, 0.50f, -0.9327f, flipHorizontal: true) },
        { "Head", new BodyPartLayoutData(new Vector2(116f, 169f), 20, 0.85f) },
        { "Left Eye", new BodyPartLayoutData(new Vector2(178f, 203f), 21, 0.17f) },
        { "Right Eye", new BodyPartLayoutData(new Vector2(137f, 203f), 21, 0.17f, 0f, flipHorizontal: true) },
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

