namespace Grafted.Scenes.MainGameScene.Gui.Widgets.CombatWidgets.BodyPartLayouts;

/// <summary>
/// Body part layout for orc pawns.
/// Positions are specified in a 512x512 coordinate space.
/// </summary>
public class OrcBodyPartLayout : IBodyPartLayout
{
    // Body part positions (native coordinates)
    private static readonly Dictionary<string, BodyPartLayoutData> PartLayoutMap = new()
    {
        { "Left Foot", new BodyPartLayoutData(new Vector2(239f, 395f), 0, 0.85f, -0.1222f) },
        { "Right Foot", new BodyPartLayoutData(new Vector2(150f, 405f), 0, 0.80f, 0.0873f) },
        { "Right Arm", new BodyPartLayoutData(new Vector2(72f, 145f), 7, 1.30f, -2.6529f, flipHorizontal: true, flipVertical: true) },
        { "Right Hand", new BodyPartLayoutData(new Vector2(54f, 264f), 11, 0.55f, -0.0175f) },
        { "Right Leg", new BodyPartLayoutData(new Vector2(137f, 284f), 38, 1.20f, -0.0349f) },
        { "Left Leg", new BodyPartLayoutData(new Vector2(211f, 285f), 45, 1.20f, -0.2618f) },
        { "Torso", new BodyPartLayoutData(new Vector2(114f, 138f), 50, 2.10f, 0.1396f) },
        { "Head", new BodyPartLayoutData(new Vector2(165f, 95f), 54, 0.95f, -0.1222f) },
        { "Left Arm", new BodyPartLayoutData(new Vector2(264f, 72f), 56, 1.30f, -2.1468f) },
        { "Left Hand", new BodyPartLayoutData(new Vector2(418f, 104f), 61, 0.55f, -0.3142f, flipHorizontal: true) },
        { "Left Eye", new BodyPartLayoutData(new Vector2(211f, 135f), 63, 0.14f, -0.6283f) },
        { "Right Eye", new BodyPartLayoutData(new Vector2(180f, 140f), 82, 0.14f, -0.4538f) },
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

