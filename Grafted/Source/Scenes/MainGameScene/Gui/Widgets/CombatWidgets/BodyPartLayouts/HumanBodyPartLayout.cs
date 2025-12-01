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
        { "Right Hand", new BodyPartLayoutData(new Vector2(125f, 298f), 0, 0.50f, 0.3840f) },
        { "Left Foot", new BodyPartLayoutData(new Vector2(287f, 430f), 0, 0.60f, -0.0982f) },
        { "Right Foot", new BodyPartLayoutData(new Vector2(129f, 425f), 0, 0.60f, 0.1963f, flipHorizontal: true) },
        { "Neck", new BodyPartLayoutData(new Vector2(201f, 115f), 4, 0.70f, 0.1047f) },
        { "Right Arm", new BodyPartLayoutData(new Vector2(117f, 142f), 7, 1.20f, -0.4909f) },
        { "Head", new BodyPartLayoutData(new Vector2(206f, 72f), 10, 0.65f, -0.0491f) },
        { "Left Hand", new BodyPartLayoutData(new Vector2(306f, 302f), 19, 0.50f, -0.2793f, flipHorizontal: true) },
        { "Left Arm", new BodyPartLayoutData(new Vector2(229f, 145f), 20, 1.20f, 0.4909f, flipHorizontal: true) },
        { "Left Eye", new BodyPartLayoutData(new Vector2(250f, 101f), 31, 0.13f, 0.1222f, flipHorizontal: true) },
        { "Right Eye", new BodyPartLayoutData(new Vector2(230f, 102f), 31, 0.13f, 0.0491f, flipHorizontal: true) },
        { "Torso", new BodyPartLayoutData(new Vector2(175f, 139f), 50, 1.20f) },
        { "Left Leg", new BodyPartLayoutData(new Vector2(206f, 261f), 50, 1.40f, -0.2454f) },
        { "Right Leg", new BodyPartLayoutData(new Vector2(110f, 258f), 50, 1.40f, 0.2454f, flipHorizontal: true) },
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

