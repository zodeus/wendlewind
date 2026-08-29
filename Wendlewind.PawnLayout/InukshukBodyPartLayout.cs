namespace Wendlewind.PawnLayout;

/// <summary>
/// Body part layout for Inukshuk pawns.
/// Positions are specified in a 512x512 coordinate space.
/// Inukshuks are stone creatures viewed from the front with a humanoid stance.
/// </summary>
public class InukshukBodyPartLayout : BodyPartLayoutBase
{
    // Body part positions (native coordinates)
    private static readonly Dictionary<string, BodyPartLayoutData> PartLayoutMap = new()
    {
        { "Left Leg", new BodyPartLayoutData(new Vector2(244f, 376f), 5, 1.00f, -0.0500f) },
        { "Right Leg", new BodyPartLayoutData(new Vector2(156f, 373f), 5, 1.00f, 0.0500f) },
        { "Right Arm", new BodyPartLayoutData(new Vector2(122f, 231f), 7, 0.90f, 0.2000f) },
        { "Torso", new BodyPartLayoutData(new Vector2(177f, 236f), 10, 1.30f) },
        { "Left Arm", new BodyPartLayoutData(new Vector2(275f, 239f), 15, 0.90f, -0.2000f, flipHorizontal: true) },
        { "Head", new BodyPartLayoutData(new Vector2(192f, 156f), 20, 1.10f) },
    };

    protected override IReadOnlyDictionary<string, BodyPartLayoutData> Map => PartLayoutMap;
}

