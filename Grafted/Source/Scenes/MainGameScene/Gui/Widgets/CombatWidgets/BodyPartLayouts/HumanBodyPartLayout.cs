namespace Grafted.Scenes.MainGameScene.Gui.Widgets.CombatWidgets.BodyPartLayouts;

/// <summary>
/// Body part layout for humanoid pawns (humans, ghouls, etc.).
/// Positions are specified in a 512x512 coordinate space.
/// </summary>
public class HumanBodyPartLayout : IBodyPartLayout
{
    // Body part layout info: position, render order, scale, and flip options
    // Positions are in native 512x512 coordinates (centered around 256, 256)
    // Body part positions (native coordinates)
    private static readonly Dictionary<string, BodyPartLayoutData> PartLayoutMap = new()
    {
        { "Left Leg", new BodyPartLayoutData(new Vector2(214f, 288f), 0, 0.90f) },
        { "Right Leg", new BodyPartLayoutData(new Vector2(168f, 284f), 0, 0.90f, flipHorizontal: true) },
        { "Left Foot", new BodyPartLayoutData(new Vector2(245f, 382f), 2, 0.50f) },
        { "Right Foot", new BodyPartLayoutData(new Vector2(186f, 380f), 3, 0.50f, flipHorizontal: true) },
        { "Neck", new BodyPartLayoutData(new Vector2(224f, 170f), 9, 0.35f) },
        { "Torso", new BodyPartLayoutData(new Vector2(188f, 184f), 10, 1.00f) },
        { "Left Arm", new BodyPartLayoutData(new Vector2(263f, 183f), 20, 0.80f, flipHorizontal: true) },
        { "Right Arm", new BodyPartLayoutData(new Vector2(126f, 182f), 21, 0.80f) },
        { "Left Hand", new BodyPartLayoutData(new Vector2(316f, 263f), 22, 0.50f, flipHorizontal: true) },
        { "Right Hand", new BodyPartLayoutData(new Vector2(111f, 260f), 23, 0.50f) },
        { "Head", new BodyPartLayoutData(new Vector2(198f, 100f), 30, 0.75f) },
        { "Left Eye", new BodyPartLayoutData(new Vector2(252f, 129f), 31, 0.20f) },
        { "Right Eye", new BodyPartLayoutData(new Vector2(222f, 129f), 31, 0.20f, flipHorizontal: true) },
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

