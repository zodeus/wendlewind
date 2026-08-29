namespace Wendlewind.Scenes.MainGameScene.Gui.Widgets.CombatWidgets.BodyPartLayouts;

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
        { "Rear Right Leg", new BodyPartLayoutData(new Vector2(251f, 406f), 1, 0.60f, 0.2793f, flipHorizontal: true) },
        { "Rear Right Hoof", new BodyPartLayoutData(new Vector2(290f, 476f), 3, 0.20f) },
        { "Front Right Leg", new BodyPartLayoutData(new Vector2(182f, 417f), 10, 0.52f, 0.5061f, flipHorizontal: true) },
        { "Tail", new BodyPartLayoutData(new Vector2(295f, 399f), 10, 0.40f, -1.6057f) },
        { "Front Left Hoof", new BodyPartLayoutData(new Vector2(206f, 478f), 17, 0.20f) },
        { "Torso", new BodyPartLayoutData(new Vector2(157f, 342f), 18, 1.27f, -0.3491f, flipHorizontal: true) },
        { "Neck", new BodyPartLayoutData(new Vector2(134f, 377f), 19, 0.50f, -0.9327f, flipHorizontal: true) },
        { "Front Left Leg", new BodyPartLayoutData(new Vector2(156f, 415f), 20, 0.56f, 0.7505f, flipHorizontal: true) },
        { "Rear Left Leg", new BodyPartLayoutData(new Vector2(225f, 411f), 21, 0.60f, 0.3491f, flipHorizontal: true) },
        { "Head", new BodyPartLayoutData(new Vector2(94f, 370f), 22, 0.75f) },
        { "Right Eye", new BodyPartLayoutData(new Vector2(113f, 401f), 36, 0.15f, 0f, flipHorizontal: true) },
        { "Rear Left Hoof", new BodyPartLayoutData(new Vector2(259f, 478f), 42, 0.20f) },
        { "Left Eye", new BodyPartLayoutData(new Vector2(149f, 399f), 57, 0.15f) },
        { "Front Right Hoof", new BodyPartLayoutData(new Vector2(171f, 480f), 66, 0.20f) },
    };

    public int NativeSize => 512;

    public BodyPartRenderInfo? GetRenderInfo(BodyPart part)
    {
        if (!PartLayoutMap.TryGetValue(part.Label, out var layoutData))
        {
            return null;
        }

        if (part.GetIcon() == null)
        {
            return null;
        }

        return new BodyPartRenderInfo(part.GetIcon(), layoutData);
    }
}

