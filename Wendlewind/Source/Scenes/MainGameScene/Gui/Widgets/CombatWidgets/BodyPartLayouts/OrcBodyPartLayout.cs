namespace Wendlewind.Scenes.MainGameScene.Gui.Widgets.CombatWidgets.BodyPartLayouts;

/// <summary>
/// Body part layout for orc pawns.
/// Positions are specified in a 512x512 coordinate space.
/// </summary>
public class OrcBodyPartLayout : BodyPartLayoutBase
{
    // Body part positions (native coordinates) - keyed by Moniker
    private static readonly Dictionary<string, BodyPartLayoutData> PartLayoutMap = new()
    {
        { "OrcFoot_Left", new BodyPartLayoutData(new Vector2(239f, 395f), 0, 0.85f, -0.1222f) },
        { "OrcFoot_Right", new BodyPartLayoutData(new Vector2(150f, 405f), 0, 0.80f, 0.0873f) },
        { "OrcArm_Right", new BodyPartLayoutData(new Vector2(72f, 145f), 7, 1.30f, -2.6529f, flipHorizontal: true, flipVertical: true) },
        { "OrcHand_Right", new BodyPartLayoutData(new Vector2(54f, 264f), 11, 0.55f, -0.0175f, equipmentAttachment: new EquipmentAttachmentData(new Vector2(-54f, 0f), 0.0000f, 1.00f, true)) },
        { "OrcLeg_Right", new BodyPartLayoutData(new Vector2(137f, 284f), 38, 1.20f, -0.0349f) },
        { "OrcLeg_Left", new BodyPartLayoutData(new Vector2(211f, 285f), 45, 1.20f, -0.2618f) },
        { "OrcTorso_TorsoSocket", new BodyPartLayoutData(new Vector2(114f, 138f), 50, 2.10f, 0.1396f) },
        { "OrcHead_HeadSocket", new BodyPartLayoutData(new Vector2(165f, 95f), 54, 0.95f, -0.1222f) },
        { "OrcArm_Left", new BodyPartLayoutData(new Vector2(264f, 72f), 56, 1.30f, -2.1468f) },
        { "OrcHand_Left", new BodyPartLayoutData(new Vector2(418f, 104f), 61, 0.55f, -0.3142f, flipHorizontal: true, equipmentAttachment: new EquipmentAttachmentData(new Vector2(-32f, -9f), 0.7679f, 1.00f, true)) },
        { "Eye_Left", new BodyPartLayoutData(new Vector2(211f, 135f), 63, 0.14f, -0.6283f) },
        { "Eye_Right", new BodyPartLayoutData(new Vector2(180f, 140f), 82, 0.14f, -0.4538f) },
    };

    protected override IReadOnlyDictionary<string, BodyPartLayoutData> Map => PartLayoutMap;

    protected override string GetLookupKey(BodyPart part) => part.InternalLabel;
}

