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
        { "Neck", new BodyPartLayoutData(new Vector2(239f, 120f), 0, 0.46f) },
        { "Right Hand", new BodyPartLayoutData(new Vector2(150f, 313f), 0, 0.50f, 0.1963f) },
        { "Left Foot", new BodyPartLayoutData(new Vector2(308f, 433f), 0, 0.60f, -0.0982f) },
        { "Right Foot", new BodyPartLayoutData(new Vector2(157f, 436f), 0, 0.60f, 0.1963f, flipHorizontal: true) },
        { "Head", new BodyPartLayoutData(new Vector2(221f, 49f), 10, 0.81f, -0.0491f) },
        { "Left Leg", new BodyPartLayoutData(new Vector2(241f, 297f), 10, 1.24f, -0.2454f) },
        { "Right Leg", new BodyPartLayoutData(new Vector2(143f, 296f), 17, 1.24f, 0.2454f, flipHorizontal: true) },
        { "Left Arm", new BodyPartLayoutData(new Vector2(254f, 151f), 20, 1.26f, 0.4909f, flipHorizontal: true) },
        { "Right Arm", new BodyPartLayoutData(new Vector2(129f, 150f), 21, 1.26f, -0.4909f) },
        { "Left Hand", new BodyPartLayoutData(new Vector2(332f, 314f), 22, 0.50f, -0.1963f, flipHorizontal: true) },
        { "Left Eye", new BodyPartLayoutData(new Vector2(284f, 89f), 31, 0.13f) },
        { "Right Eye", new BodyPartLayoutData(new Vector2(255f, 88f), 31, 0.13f, 0.0491f, flipHorizontal: true) },
        { "Torso", new BodyPartLayoutData(new Vector2(184f, 143f), 50, 1.42f) },
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

