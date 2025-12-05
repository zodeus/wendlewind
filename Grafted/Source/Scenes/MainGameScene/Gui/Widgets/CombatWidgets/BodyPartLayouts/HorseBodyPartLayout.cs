namespace Grafted.Scenes.MainGameScene.Gui.Widgets.CombatWidgets.BodyPartLayouts;

/// <summary>
/// Body part layout for horse pawns.
/// Positions are specified in a 512x512 coordinate space.
/// Horse is viewed from the side with head on the left.
/// </summary>
public class HorseBodyPartLayout : IBodyPartLayout
{
    // Body part positions (native coordinates)
    private static readonly Dictionary<string, BodyPartLayoutData> PartLayoutMap = new()
    {
        { "Front Left Hoof", new BodyPartLayoutData(new Vector2(191f, 435f), 0, 0.35f) },
        { "Rear Right Hoof", new BodyPartLayoutData(new Vector2(306f, 441f), 0, 0.35f) },
        { "Rear Right Leg", new BodyPartLayoutData(new Vector2(257f, 294f), 1, 1.30f, 0.2000f) },
        { "Front Right Leg", new BodyPartLayoutData(new Vector2(142f, 290f), 10, 1.30f, 0.1920f) },
        { "Front Right Hoof", new BodyPartLayoutData(new Vector2(139f, 445f), 14, 0.35f, 0.0349f) },
        { "Torso", new BodyPartLayoutData(new Vector2(152f, 134f), 18, 1.10f) },
        { "Neck", new BodyPartLayoutData(new Vector2(73f, 158f), 19, 1.25f, 0.0524f) },
        { "Front Left Leg", new BodyPartLayoutData(new Vector2(93f, 300f), 20, 1.35f, 0.2967f) },
        { "Head", new BodyPartLayoutData(new Vector2(-12f, 122f), 22, 1.20f, -0.0873f) },
        { "Rear Left Hoof", new BodyPartLayoutData(new Vector2(361f, 453f), 23, 0.35f) },
        { "Rear Left Leg", new BodyPartLayoutData(new Vector2(292f, 306f), 28, 1.35f, -0.0524f) },
        { "Tail", new BodyPartLayoutData(new Vector2(408f, 180f), 31, 0.80f, -0.5236f) },
        { "Right Eye", new BodyPartLayoutData(new Vector2(29f, 189f), 36, 0.10f) },
        { "Left Eye", new BodyPartLayoutData(new Vector2(58f, 189f), 57, 0.10f) },
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

