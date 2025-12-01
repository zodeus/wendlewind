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
        { "Left Foot", new BodyPartLayoutData(new Vector2(279f, 424f), 0, 0.60f, -0.0982f) },
        { "Left Leg", new BodyPartLayoutData(new Vector2(257f, 376f), 10, 0.60f, -0.2454f) },
        { "Right Arm", new BodyPartLayoutData(new Vector2(170f, 241f), 21, 0.55f, 0.1745f) },
        { "Torso", new BodyPartLayoutData(new Vector2(124f, 236f), 22, 1.70f) },
        { "Right Hand", new BodyPartLayoutData(new Vector2(128f, 210f), 30, 0.65f, 1.9548f, flipHorizontal: true) },
        { "Right Leg", new BodyPartLayoutData(new Vector2(168f, 382f), 31, 0.60f, 0.2443f) },
        { "Right Foot", new BodyPartLayoutData(new Vector2(167f, 428f), 33, 0.60f, 0.0698f) },
        { "Neck", new BodyPartLayoutData(new Vector2(204f, 190f), 50, 0.90f, -0.0873f) },
        { "Head", new BodyPartLayoutData(new Vector2(200f, 167f), 50, 0.95f, -0.0491f) },
        { "Left Arm", new BodyPartLayoutData(new Vector2(241f, 244f), 52, 0.60f, 0.0175f) },
        { "Left Hand", new BodyPartLayoutData(new Vector2(181f, 227f), 62, 0.70f, 1.3963f, flipHorizontal: true) },
        { "Right Eye", new BodyPartLayoutData(new Vector2(233f, 199f), 64, 0.13f, 0.0491f) },
        { "Left Eye", new BodyPartLayoutData(new Vector2(248f, 197f), 75, 0.13f, -0.2094f) },
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


