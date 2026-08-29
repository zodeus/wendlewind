namespace Wendlewind.Scenes.MainGameScene.Gui.Widgets.CombatWidgets.BodyPartLayouts;

/// <summary>
/// Body part layout for rabbit pawns (bunnies, rabbits, etc.).
/// Positions are specified in a 512x512 coordinate space.
/// Rabbit is viewed from the side with head on the right.
/// </summary>
public class RabbitBodyPartLayout : IBodyPartLayout
{
    // Body part positions (native coordinates)
    private static readonly Dictionary<string, BodyPartLayoutData> PartLayoutMap = new()
    {
        { "Rear Right Leg", new BodyPartLayoutData(new Vector2(240f, 424f), 1, 0.40f, 0f, flipHorizontal: true) },
        { "Rear Right Paw", new BodyPartLayoutData(new Vector2(261f, 462f), 2, 0.22f) },
        { "Front Right Paw", new BodyPartLayoutData(new Vector2(212f, 448f), 7, 0.22f, 0.1963f) },
        { "Front Right Leg", new BodyPartLayoutData(new Vector2(205f, 405f), 8, 0.40f, 0.5400f, flipHorizontal: true) },
        { "Front Left Paw", new BodyPartLayoutData(new Vector2(230f, 457f), 13, 0.25f) },
        { "Right Eye", new BodyPartLayoutData(new Vector2(176f, 346f), 14, 0.15f, -3.1416f, flipHorizontal: true, flipVertical: true) },
        { "Torso", new BodyPartLayoutData(new Vector2(212f, 339f), 15, 1.05f, 1.1781f, flipHorizontal: true) },
        { "Neck", new BodyPartLayoutData(new Vector2(205f, 348f), 16, 0.42f, 2.1108f, flipHorizontal: true) },
        { "Head", new BodyPartLayoutData(new Vector2(156f, 289f), 18, 0.80f) },
        { "Rear Left Paw", new BodyPartLayoutData(new Vector2(283f, 465f), 18, 0.25f) },
        { "Left Eye", new BodyPartLayoutData(new Vector2(204f, 343f), 19, 0.15f, -0.7363f) },
        { "Rear Left Leg", new BodyPartLayoutData(new Vector2(259f, 424f), 21, 0.44f, 0f, flipHorizontal: true) },
        { "Front Left Leg", new BodyPartLayoutData(new Vector2(215f, 414f), 31, 0.44f, 0.3436f, flipHorizontal: true) },
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


