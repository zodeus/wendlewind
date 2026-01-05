namespace Grafted.Scenes.MainGameScene.Gui.Widgets.CombatWidgets.BodyPartLayouts;

/// <summary>
/// Body part layout for humanoid pawns (humans, ghouls, etc.).
/// Positions are specified in a 512x512 coordinate space.
/// </summary>
public class HumanBodyPartLayout : IBodyPartLayout
{
    // Body part positions (native coordinates)
    private static readonly Dictionary<string, BodyPartLayoutData> PartLayoutMap = new()
    {
        { "Left Foot", new BodyPartLayoutData(new Vector2(273f, 429f), 0, 0.60f, -0.0982f) },
        { "Right Foot", new BodyPartLayoutData(new Vector2(131f, 426f), 0, 0.60f, 0.1963f, flipHorizontal: true) },
        { "Neck", new BodyPartLayoutData(new Vector2(207f, 128f), 4, 0.55f, 0.1047f) },
        { "Head", new BodyPartLayoutData(new Vector2(200f, 85f), 10, 0.65f, -0.0491f, equipmentAttachment: new EquipmentAttachmentData(new Vector2(-6f, -7f), 0.0000f, 0.75f, true)) },
        { "Right Arm", new BodyPartLayoutData(new Vector2(121f, 155f), 14, 1.05f, -0.4909f) },
        { "Left Hand", new BodyPartLayoutData(new Vector2(294f, 291f), 19, 0.50f, -0.2793f, flipHorizontal: true, equipmentAttachment: new EquipmentAttachmentData(new Vector2(40f, 29f), 0.2793f, 0.90f, false, renderArmor: false)) },
        { "Left Arm", new BodyPartLayoutData(new Vector2(231f, 159f), 20, 1.05f, 0.4909f, flipHorizontal: true) },
        { "Left Eye", new BodyPartLayoutData(new Vector2(244f, 114f), 31, 0.13f, 0.1222f, flipHorizontal: true) },
        { "Right Eye", new BodyPartLayoutData(new Vector2(224f, 115f), 31, 0.13f, 0.0491f, flipHorizontal: true) },
        { "Torso", new BodyPartLayoutData(new Vector2(169f, 152f), 32, 1.20f) },
        { "Left Leg", new BodyPartLayoutData(new Vector2(200f, 274f), 50, 1.30f, -0.2454f) },
        { "Right Leg", new BodyPartLayoutData(new Vector2(118f, 271f), 50, 1.30f, 0.2454f, flipHorizontal: true) },
        { "Right Hand", new BodyPartLayoutData(new Vector2(127f, 286f), 59, 0.50f, 0.3840f, equipmentAttachment: new EquipmentAttachmentData(new Vector2(36f, 33f), 0.2269f, 1.00f, false, renderArmor: false)) },
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

