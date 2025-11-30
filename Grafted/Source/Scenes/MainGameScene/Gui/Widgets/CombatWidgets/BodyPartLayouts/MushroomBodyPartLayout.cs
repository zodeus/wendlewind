namespace Grafted.Scenes.MainGameScene.Gui.Widgets.CombatWidgets.BodyPartLayouts;

/// <summary>
/// Body part layout for mushroom pawns.
/// Positions are specified in a 512x512 coordinate space.
/// Mushroom is viewed from the front with cap on top and humanoid limbs.
/// </summary>
public class MushroomBodyPartLayout : IBodyPartLayout
{
    // Body part positions (native coordinates)
    private static readonly Dictionary<string, BodyPartLayoutData> PartLayoutMap = new()
    {
        // Feet
        { "Left Foot", new BodyPartLayoutData(new Vector2(310f, 450f), 0, 0.50f, -0.0982f) },
        { "Right Foot", new BodyPartLayoutData(new Vector2(160f, 450f), 0, 0.50f, 0.0982f, flipHorizontal: true) },
        
        // Hands
        { "Left Hand", new BodyPartLayoutData(new Vector2(380f, 300f), 2, 0.45f, -0.1963f) },
        { "Right Hand", new BodyPartLayoutData(new Vector2(90f, 300f), 2, 0.45f, 0.1963f, flipHorizontal: true) },
        
        // Legs
        { "Left Leg", new BodyPartLayoutData(new Vector2(280f, 340f), 5, 0.90f, -0.1963f) },
        { "Right Leg", new BodyPartLayoutData(new Vector2(190f, 340f), 5, 0.90f, 0.1963f, flipHorizontal: true) },
        
        // Arms
        { "Left Arm", new BodyPartLayoutData(new Vector2(320f, 200f), 8, 0.85f, 0.5890f) },
        { "Right Arm", new BodyPartLayoutData(new Vector2(150f, 200f), 8, 0.85f, -0.5890f, flipHorizontal: true) },
        
        // Stump (torso)
        { "Stump", new BodyPartLayoutData(new Vector2(190f, 180f), 15, 1.30f) },
        
        // Cap (head)
        { "Cap", new BodyPartLayoutData(new Vector2(160f, 50f), 20, 1.50f) },
        
        // Eyes on stump
        { "Left Eye", new BodyPartLayoutData(new Vector2(295f, 220f), 25, 0.18f) },
        { "Right Eye", new BodyPartLayoutData(new Vector2(235f, 218f), 25, 0.18f, 0f, flipHorizontal: true) },
    };

    public int NativeSize => 512;

    public bool SupportsBody(PawnBody body)
    {
        // Check if this is a mushroom body by looking at the torso
        var torso = body.AllExternalParts.FirstOrDefault(p => p.Type == BodyPartType.Torso);
        if (torso?.BodyPartDef.Moniker == "MushroomStump")
        {
            return true;
        }

        return false;
    }

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

