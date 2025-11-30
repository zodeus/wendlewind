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
        { "Neck", new BodyPartLayoutData(new Vector2(188f, 74f), 0, 0.90f) },
        { "Right Hand", new BodyPartLayoutData(new Vector2(126f, 285f), 0, 0.50f, 0.1963f) },
        { "Left Foot", new BodyPartLayoutData(new Vector2(287f, 430f), 0, 0.60f, -0.0982f) },
        { "Right Foot", new BodyPartLayoutData(new Vector2(129f, 425f), 0, 0.60f, 0.1963f, flipHorizontal: true) },
        { "Head", new BodyPartLayoutData(new Vector2(199f, 27f), 10, 0.81f, -0.0491f) },
        { "Left Hand", new BodyPartLayoutData(new Vector2(309f, 286f), 19, 0.50f, -0.1963f, flipHorizontal: true) },
        { "Left Arm", new BodyPartLayoutData(new Vector2(230f, 123f), 20, 1.26f, 0.4909f, flipHorizontal: true) },
        { "Right Arm", new BodyPartLayoutData(new Vector2(105f, 122f), 21, 1.26f, -0.4909f) },
        { "Left Eye", new BodyPartLayoutData(new Vector2(261f, 66f), 31, 0.13f) },
        { "Right Eye", new BodyPartLayoutData(new Vector2(234f, 66f), 31, 0.13f, 0.0491f, flipHorizontal: true) },
        { "Torso", new BodyPartLayoutData(new Vector2(160f, 119f), 50, 1.42f) },
        { "Left Leg", new BodyPartLayoutData(new Vector2(206f, 261f), 50, 1.40f, -0.2454f) },
        { "Right Leg", new BodyPartLayoutData(new Vector2(110f, 258f), 50, 1.40f, 0.2454f, flipHorizontal: true) },
    };

    public int NativeSize => 512;
    
    public bool SupportsBody(PawnBody body)
    {
        // Check if this is a humanoid body by looking at the body def or pawn def
        var pawnDef = body.Pawn.PawnDef;
        
        // Check if the pawn uses human body parts
        // We can check this by seeing if the torso is a HumanTorso
        var torso = body.AllExternalParts.FirstOrDefault(p => p.Type == BodyPartType.Torso);
        if (torso?.BodyPartDef.Moniker == "HumanTorso")
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

