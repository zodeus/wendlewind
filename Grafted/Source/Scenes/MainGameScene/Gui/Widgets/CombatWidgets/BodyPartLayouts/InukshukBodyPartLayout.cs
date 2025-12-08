namespace Grafted.Scenes.MainGameScene.Gui.Widgets.CombatWidgets.BodyPartLayouts;

/// <summary>
/// Body part layout for Inukshuk pawns.
/// Positions are specified in a 512x512 coordinate space.
/// Inukshuks are stone creatures viewed from the front with a humanoid stance.
/// </summary>
public class InukshukBodyPartLayout : IBodyPartLayout
{
    // Body part positions (native coordinates)
    private static readonly Dictionary<string, BodyPartLayoutData> PartLayoutMap = new()
    {
        { "Left Leg", new BodyPartLayoutData(new Vector2(202f, 375f), 5, 1.00f, -0.0500f) },
        { "Right Leg", new BodyPartLayoutData(new Vector2(114f, 372f), 5, 1.00f, 0.0500f) },
        { "Right Arm", new BodyPartLayoutData(new Vector2(80f, 230f), 7, 0.90f, 0.2000f) },
        { "Torso", new BodyPartLayoutData(new Vector2(135f, 235f), 10, 1.30f) },
        { "Left Arm", new BodyPartLayoutData(new Vector2(233f, 238f), 15, 0.90f, -0.2000f, flipHorizontal: true) },
        { "Head", new BodyPartLayoutData(new Vector2(150f, 155f), 20, 1.10f) },
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

