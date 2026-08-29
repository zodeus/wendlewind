namespace Wendlewind.Scenes.MainGameScene.Gui.Widgets.CombatWidgets.BodyPartLayouts;

/// <summary>
/// Body part layout for TinMan pawns (mechanical humanoid constructs).
/// Positions are specified in a 512x512 coordinate space.
/// </summary>
public class TinManBodyPartLayout : BodyPartLayoutBase
{
    // Body part positions (native coordinates)
    private static readonly Dictionary<string, BodyPartLayoutData> PartLayoutMap = new()
    {
        { "Left Foot", new BodyPartLayoutData(new Vector2(232f, 439f), 0, 0.50f, 0.1571f) },
        { "Right Arm", new BodyPartLayoutData(new Vector2(109f, 212f), 7, 1.00f, -3.0194f, flipVertical: true) },
        { "Right Foot", new BodyPartLayoutData(new Vector2(165f, 434f), 11, 0.50f, 0.1396f) },
        { "Torso", new BodyPartLayoutData(new Vector2(182f, 208f), 50, 1.10f) },
        { "Left Leg", new BodyPartLayoutData(new Vector2(204f, 337f), 50, 1.00f, 0.1571f) },
        { "Right Leg", new BodyPartLayoutData(new Vector2(144f, 332f), 50, 1.00f, 0.2793f) },
        { "Left Arm", new BodyPartLayoutData(new Vector2(264f, 223f), 53, 1.00f, -0.0349f) },
        { "Head", new BodyPartLayoutData(new Vector2(193f, 133f), 54, 0.80f, -0.0491f) },
        { "Right Hand", new BodyPartLayoutData(new Vector2(283f, 335f), 62, 0.50f, 0.2793f) },
        { "Left Hand", new BodyPartLayoutData(new Vector2(140f, 323f), 69, 0.50f, 0.0524f, flipHorizontal: true) },
    };

    protected override IReadOnlyDictionary<string, BodyPartLayoutData> Map => PartLayoutMap;
}
