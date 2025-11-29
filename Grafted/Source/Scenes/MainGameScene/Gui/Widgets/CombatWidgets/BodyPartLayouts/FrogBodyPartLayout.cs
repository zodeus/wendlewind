namespace Grafted.Scenes.MainGameScene.Gui.Widgets.CombatWidgets.BodyPartLayouts;

/// <summary>
/// Body part layout for frog pawns.
/// Positions are specified in a 512x512 coordinate space.
/// Frog is viewed from the side with head on the right, in a crouched pose.
/// </summary>
public class FrogBodyPartLayout : IBodyPartLayout
{
    // Body part positions (native coordinates)
    private static readonly Dictionary<string, BodyPartLayoutData> PartLayoutMap = new()
    {
        { "Front Right Foot", new BodyPartLayoutData(new Vector2(173f, 328f), 0, 0.55f) },
        { "Rear Right Foot", new BodyPartLayoutData(new Vector2(334f, 337f), 0, 0.55f, -0.2793f) },
        { "Rear Right Leg", new BodyPartLayoutData(new Vector2(318f, 273f), 2, 0.70f, -0.8727f) },
        { "Front Right Leg", new BodyPartLayoutData(new Vector2(177f, 275f), 5, 0.75f, -0.1963f) },
        { "Rear Left Foot", new BodyPartLayoutData(new Vector2(277f, 340f), 11, 0.60f, 0.3142f) },
        { "Torso", new BodyPartLayoutData(new Vector2(227f, 179f), 15, 1.40f, 0.0982f) },
        { "Rear Left Leg", new BodyPartLayoutData(new Vector2(303f, 301f), 16, 0.75f, 0.3927f) },
        { "Front Left Foot", new BodyPartLayoutData(new Vector2(235f, 320f), 23, 0.60f) },
        { "Front Left Leg", new BodyPartLayoutData(new Vector2(249f, 269f), 26, 0.85f, 0.2094f) },
        { "Head", new BodyPartLayoutData(new Vector2(146f, 148f), 43, 1.60f, -0.0982f) },
        { "Right Eye", new BodyPartLayoutData(new Vector2(199f, 177f), 58, 0.20f, -0.1396f, flipHorizontal: true) },
        { "Left Eye", new BodyPartLayoutData(new Vector2(253f, 173f), 75, 0.20f, -0.1963f) },
    };


    public int NativeSize => 512;

    public bool SupportsBody(PawnBody body)
    {
        // Check if this is a frog body by looking at the torso
        var torso = body.AllExternalParts.FirstOrDefault(p => p.Type == BodyPartType.Torso);
        if (torso?.BodyPartDef.Moniker == "FrogTorso")
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

