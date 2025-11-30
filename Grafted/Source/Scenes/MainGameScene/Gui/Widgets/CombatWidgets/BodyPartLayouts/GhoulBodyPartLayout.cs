namespace Grafted.Scenes.MainGameScene.Gui.Widgets.CombatWidgets.BodyPartLayouts;

/// <summary>
/// Body part layout for ghoul pawns.
/// Positions are specified in a 512x512 coordinate space.
/// </summary>
public class GhoulBodyPartLayout : IBodyPartLayout
{
    // Body part positions (native coordinates)
    private static readonly Dictionary<string, BodyPartLayoutData> PartLayoutMap = new()
    {
        { "Neck", new BodyPartLayoutData(new Vector2(203f, 85f), 0, 0.50f) },
        { "Right Hand", new BodyPartLayoutData(new Vector2(118f, 280f), 0, 0.50f, 0.1963f) },
        { "Left Foot", new BodyPartLayoutData(new Vector2(279f, 424f), 0, 0.60f, -0.0982f) },
        { "Right Foot", new BodyPartLayoutData(new Vector2(122f, 428f), 0, 0.60f, 0.1963f, flipHorizontal: true) },
        { "Head", new BodyPartLayoutData(new Vector2(192f, 22f), 10, 0.81f, -0.0491f) },
        { "Left Leg", new BodyPartLayoutData(new Vector2(204f, 269f), 10, 1.40f, -0.2454f) },
        { "Right Leg", new BodyPartLayoutData(new Vector2(102f, 265f), 17, 1.40f, 0.2454f, flipHorizontal: true) },
        { "Left Hand", new BodyPartLayoutData(new Vector2(301f, 281f), 19, 0.50f, -0.1963f, flipHorizontal: true) },
        { "Left Arm", new BodyPartLayoutData(new Vector2(222f, 118f), 20, 1.26f, 0.4909f, flipHorizontal: true) },
        { "Right Arm", new BodyPartLayoutData(new Vector2(97f, 117f), 21, 1.26f, -0.4909f) },
        { "Left Eye", new BodyPartLayoutData(new Vector2(253f, 61f), 31, 0.13f) },
        { "Right Eye", new BodyPartLayoutData(new Vector2(223f, 59f), 31, 0.13f, 0.0491f, flipHorizontal: true) },
        { "Torso", new BodyPartLayoutData(new Vector2(152f, 110f), 50, 1.42f) },
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


