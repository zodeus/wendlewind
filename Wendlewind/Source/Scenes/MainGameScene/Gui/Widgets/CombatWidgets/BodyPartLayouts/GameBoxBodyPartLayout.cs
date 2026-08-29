namespace Wendlewind.Scenes.MainGameScene.Gui.Widgets.CombatWidgets.BodyPartLayouts;

/// <summary>
/// Body part layout for GameBox pawns (retro game console constructs).
/// Positions are specified in a 512x512 coordinate space.
/// </summary>
public class GameBoxBodyPartLayout : BodyPartLayoutBase
{
    // Body part positions (native coordinates)
    private static readonly Dictionary<string, BodyPartLayoutData> PartLayoutMap = new()
    {
        { "Right Hand", new BodyPartLayoutData(new Vector2(65f, 278f), 0, 0.35f, 1.4312f, flipHorizontal: true, equipmentAttachment: new EquipmentAttachmentData(new Vector2(35f, 30f), -0.1745f, 0.85f, false)) },
        { "Right Arm", new BodyPartLayoutData(new Vector2(91f, 160f), 1, 0.65f, 0.5061f) },
        { "Right Leg", new BodyPartLayoutData(new Vector2(124f, 318f), 1, 0.65f, 0.0873f) },
        { "Left Leg", new BodyPartLayoutData(new Vector2(203f, 329f), 3, 0.65f) },
        { "Torso", new BodyPartLayoutData(new Vector2(152f, 147f), 6, 0.85f) },
        { "Controls", new BodyPartLayoutData(new Vector2(147f, 149f), 32, 0.85f) },
        { "Left Hand", new BodyPartLayoutData(new Vector2(228f, 312f), 39, 0.30f, 0.1745f, equipmentAttachment: new EquipmentAttachmentData(new Vector2(35f, 30f), 0.1745f, 0.85f, false)) },
        { "Left Arm", new BodyPartLayoutData(new Vector2(231f, 189f), 45, 0.65f, 0.2618f) },
        { "Head", new BodyPartLayoutData(new Vector2(154f, 162f), 60, 0.55f) }
    };

    protected override IReadOnlyDictionary<string, BodyPartLayoutData> Map => PartLayoutMap;
}
