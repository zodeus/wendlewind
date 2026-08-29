namespace Wendlewind.Scenes.MainGameScene.Gui.Widgets.CombatWidgets.BodyPartLayouts;

/// <summary>
/// Body part layout for bat pawns.
/// Positions are specified in a 512x512 coordinate space.
/// Bat is viewed from the front with wings spread.
/// </summary>
[UsedImplicitly]
public class BatBodyPartLayout : IBodyPartLayout
{
    // Body part positions (native coordinates)
    private static readonly Dictionary<string, BodyPartLayoutData> PartLayoutMap = new()
    {
        { "Torso", new BodyPartLayoutData(new Vector2(155f, 229f), 0, 0.85f) },
        { "Left Wing", new BodyPartLayoutData(new Vector2(287f, 221f), 10, 0.85f, 0.1000f) },
        { "Right Wing", new BodyPartLayoutData(new Vector2(-6f, 214f), 10, 0.90f, -0.0175f, flipHorizontal: true) },
        { "Head", new BodyPartLayoutData(new Vector2(151f, 107f), 30, 0.80f) },
        { "Left Eye", new BodyPartLayoutData(new Vector2(264f, 228f), 35, 0.15f, -0.2618f) },
        { "Right Eye", new BodyPartLayoutData(new Vector2(211f, 228f), 35, 0.15f) },
        { "Left Claw", new BodyPartLayoutData(new Vector2(231f, 373f), 35, 0.55f, -0.6807f) },
        { "Right Claw", new BodyPartLayoutData(new Vector2(130f, 376f), 40, 0.55f, 0.7854f, flipHorizontal: true) },
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
