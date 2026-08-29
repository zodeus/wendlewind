namespace Wendlewind.Scenes.MainGameScene.Gui.Widgets.CombatWidgets.BodyPartLayouts;

/// <summary>
/// Body part layout for mushroom pawns.
/// Positions are specified in a 512x512 coordinate space.
/// Mushroom is viewed from the front with cap on top and humanoid limbs.
/// </summary>
public class MushroomBodyPartLayout : IBodyPartLayout
{
    // Body part positions (native coordinates)
    private static readonly Dictionary<string, BodyPartLayoutData> PartLayoutMap = new()
    {
        { "Left Hand", new BodyPartLayoutData(new Vector2(118f, 344f), 0, 0.45f, -1.6755f, flipVertical: true) },
        { "Left Foot", new BodyPartLayoutData(new Vector2(257f, 447f), 0, 0.50f, -0.0982f, flipHorizontal: true) },
        { "Right Foot", new BodyPartLayoutData(new Vector2(179f, 447f), 0, 0.50f, 0.0982f, flipHorizontal: true) },
        { "Right Hand", new BodyPartLayoutData(new Vector2(62f, 321f), 2, 0.45f, 1.4137f, flipHorizontal: true) },
        { "Cap", new BodyPartLayoutData(new Vector2(120f, 77f), 4, 2.00f, 0.2793f, flipHorizontal: true) },
        { "Left Leg", new BodyPartLayoutData(new Vector2(147f, 351f), 5, 0.90f, 0.3316f, flipHorizontal: true) },
        { "Right Leg", new BodyPartLayoutData(new Vector2(214f, 347f), 5, 0.90f, 0.1963f, flipHorizontal: true) },
        { "Right Arm", new BodyPartLayoutData(new Vector2(104f, 261f), 8, 0.85f, 0.3840f, flipHorizontal: true) },
        { "Stump", new BodyPartLayoutData(new Vector2(139f, 206f), 15, 1.30f, 0f, flipHorizontal: true) },
        { "Left Eye", new BodyPartLayoutData(new Vector2(217f, 215f), 25, 0.18f) },
        { "Right Eye", new BodyPartLayoutData(new Vector2(177f, 219f), 25, 0.18f) },
        { "Left Arm", new BodyPartLayoutData(new Vector2(160f, 278f), 29, 0.85f, 0.2094f, flipHorizontal: true) },
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


