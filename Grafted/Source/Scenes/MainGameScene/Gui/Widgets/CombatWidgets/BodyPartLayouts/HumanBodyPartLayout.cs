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
        { "Right Hand", new BodyPartLayoutData(new Vector2(126f, 288f), 0, 0.50f, 0.3840f) },
        { "Left Foot", new BodyPartLayoutData(new Vector2(273f, 429f), 0, 0.60f, -0.0982f) },
        { "Right Foot", new BodyPartLayoutData(new Vector2(131f, 426f), 0, 0.60f, 0.1963f, flipHorizontal: true) },
        { "Neck", new BodyPartLayoutData(new Vector2(207f, 128f), 4, 0.55f, 0.1047f) },
        { "Right Arm", new BodyPartLayoutData(new Vector2(121f, 155f), 7, 1.05f, -0.4909f) },
        { "Head", new BodyPartLayoutData(new Vector2(200f, 85f), 10, 0.65f, -0.0491f) },
        { "Left Hand", new BodyPartLayoutData(new Vector2(294f, 291f), 19, 0.50f, -0.2793f, flipHorizontal: true) },
        { "Left Arm", new BodyPartLayoutData(new Vector2(231f, 159f), 20, 1.05f, 0.4909f, flipHorizontal: true) },
        { "Left Eye", new BodyPartLayoutData(new Vector2(244f, 114f), 31, 0.13f, 0.1222f, flipHorizontal: true) },
        { "Right Eye", new BodyPartLayoutData(new Vector2(224f, 115f), 31, 0.13f, 0.0491f, flipHorizontal: true) },
        { "Torso", new BodyPartLayoutData(new Vector2(169f, 152f), 50, 1.20f) },
        { "Left Leg", new BodyPartLayoutData(new Vector2(200f, 274f), 50, 1.30f, -0.2454f) },
        { "Right Leg", new BodyPartLayoutData(new Vector2(117f, 271f), 50, 1.30f, 0.2454f, flipHorizontal: true) },
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

