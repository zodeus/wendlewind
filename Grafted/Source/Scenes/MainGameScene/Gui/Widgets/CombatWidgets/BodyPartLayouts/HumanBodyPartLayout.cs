namespace Grafted.Scenes.MainGameScene.Gui.Widgets.CombatWidgets.BodyPartLayouts;

/// <summary>
/// Body part layout for humanoid pawns (humans, ghouls, etc.).
/// Positions are specified in a 512x512 coordinate space.
/// </summary>
public class HumanBodyPartLayout : IBodyPartLayout
{
    // Body part layout info: position, render order, scale, and flip options
    // Positions are in native 512x512 coordinates (centered around 256, 256)
    private static readonly Dictionary<string, BodyPartLayoutData> PartLayoutMap = new()
    {
        // Legs (back layer) - positioned below torso, hanging from hips
        { "Left Leg", new BodyPartLayoutData(new Vector2(270, 290), 0, 0.9f) },
        { "Right Leg", new BodyPartLayoutData(new Vector2(150, 290), 0, 0.9f, flipHorizontal: true) },
        
        // Feet - at the bottom of legs
        { "Left Foot", new BodyPartLayoutData(new Vector2(260, 420), 2, 0.5f) },
        { "Right Foot", new BodyPartLayoutData(new Vector2(140, 420), 3, 0.5f, flipHorizontal: true) },
        
        { "Left Eye", new BodyPartLayoutData(new Vector2(230, 100), 31, 0.20f ) },
        { "Right Eye", new BodyPartLayoutData(new Vector2(195, 100), 31, 0.20f,flipHorizontal: true) },
        
        // Head (front layer) - at the top, centered above torso
        { "Head", new BodyPartLayoutData(new Vector2(195, 80), 30, 0.75f) },
        
        // Neck - connects head to torso (behind head)
        { "Neck", new BodyPartLayoutData(new Vector2(215, 140), 9, 0.35f) },

        // Torso (middle layer) - center of the body
        { "Torso", new BodyPartLayoutData(new Vector2(180, 160), 10, 1f) },
        
        // Arms - attached to shoulders
        { "Left Arm", new BodyPartLayoutData(new Vector2(320, 180), 20, 0.8f, flipHorizontal: true) },
        { "Right Arm", new BodyPartLayoutData(new Vector2(20, 180), 21, 0.8f) },
        
        // Hands - at the end of arms
        { "Left Hand", new BodyPartLayoutData(new Vector2(340, 300), 22, 0.5f, flipHorizontal: true) },
        { "Right Hand", new BodyPartLayoutData(new Vector2(40, 300), 23, 0.5f) },
        
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

