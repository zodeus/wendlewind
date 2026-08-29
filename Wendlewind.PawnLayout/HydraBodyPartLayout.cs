namespace Wendlewind.PawnLayout;

/// <summary>
/// Body part layout for Hydra pawns (multi-headed flesh creatures).
/// Positions are specified in a 512x512 coordinate space.
/// The Hydra has torso as root with 3 heads attached to it.
/// </summary>
[UsedImplicitly]
public class HydraBodyPartLayout : BodyPartLayoutBase
{
    // Body part positions (native coordinates)
    private static readonly Dictionary<string, BodyPartLayoutData> PartLayoutMap = new()
    {
        { "Torso", new BodyPartLayoutData(new Vector2(83f, 179f), 50, 1.40f) },
        { "Right Head", new BodyPartLayoutData(new Vector2(268f, 217f), 64, 0.45f, 0.2500f, flipHorizontal: true) },
        { "Left Head", new BodyPartLayoutData(new Vector2(55f, 196f), 90, 0.65f, -0.7854f) },
        { "Head", new BodyPartLayoutData(new Vector2(122f, 173f), 100, 0.75f) },
    };

    protected override IReadOnlyDictionary<string, BodyPartLayoutData> Map => PartLayoutMap;
}
