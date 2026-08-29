namespace Wendlewind.Scenes.MainGameScene.Gui.Widgets.CombatWidgets.BodyPartLayouts;

/// <summary>
/// Body part layout for wearbear pawns.
/// Positions are specified in a 512x512 coordinate space.
/// Wearbear is viewed from the side with head on the right.
/// </summary>
public class WearbearBodyPartLayout : IBodyPartLayout
{
 // Body part positions (native coordinates)
private static readonly Dictionary<string, BodyPartLayoutData> PartLayoutMap = new()
{
    { "Right Foot", new BodyPartLayoutData(new Vector2(238f, 402f), 2, 0.80f, 0.2967f) },
    { "Tail", new BodyPartLayoutData(new Vector2(310f, 251f), 3, 0.85f, -2.9147f) },
    { "Right Arm", new BodyPartLayoutData(new Vector2(113f, 107f), 8, 1.45f, -2.6878f, flipVertical: true) },
    { "Right Hand", new BodyPartLayoutData(new Vector2(111f, 259f), 9, 0.80f, 0f, flipHorizontal: true) },
    { "Torso", new BodyPartLayoutData(new Vector2(164f, 105f), 15, 1.80f, 0.0800f) },
    { "Right Leg", new BodyPartLayoutData(new Vector2(139f, 262f), 17, 1.45f) },
    { "Head", new BodyPartLayoutData(new Vector2(186f, 51f), 18, 1.15f, -0.1500f) },
    { "Left Eye", new BodyPartLayoutData(new Vector2(231f, 111f), 19, 0.12f, -0.6000f) },
    { "Right Eye", new BodyPartLayoutData(new Vector2(200f, 115f), 19, 0.12f, -0.5236f) },
    { "Left Leg", new BodyPartLayoutData(new Vector2(193f, 252f), 23, 1.50f, 0.0873f, flipHorizontal: true) },
    { "Left Hand", new BodyPartLayoutData(new Vector2(268f, 300f), 24, 0.80f, 0.3491f) },
    { "Left Foot", new BodyPartLayoutData(new Vector2(144f, 389f), 25, 0.85f, 0.2618f) },
    { "Left Arm", new BodyPartLayoutData(new Vector2(235f, 139f), 26, 1.50f, 0.1222f) },
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

