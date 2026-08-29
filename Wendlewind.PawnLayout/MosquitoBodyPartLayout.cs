namespace Wendlewind.PawnLayout;

/// <summary>
/// Body part layout for mosquito pawns.
/// Positions are specified in a 512x512 coordinate space.
/// </summary>
public class MosquitoBodyPartLayout : BodyPartLayoutBase
{
    // Body part positions (native coordinates)
    private static readonly Dictionary<string, BodyPartLayoutData> PartLayoutMap = new()
    {
        { "Front Right Leg", new BodyPartLayoutData(new Vector2(176f, 335f), 0, 0.75f, 0.3491f, flipHorizontal: true) },
        { "Middle Right Leg", new BodyPartLayoutData(new Vector2(229f, 367f), 0, 0.75f, 0.5236f) },
        { "Rear Right Leg", new BodyPartLayoutData(new Vector2(271f, 387f), 0, 0.75f, 0.6981f) },
        { "Abdomen", new BodyPartLayoutData(new Vector2(295f, 297f), 16, 1.40f, 0.1745f) },
        { "Left Wing", new BodyPartLayoutData(new Vector2(210f, 185f), 18, 1.05f, 0.6283f, flipHorizontal: true) },
        { "Thorax", new BodyPartLayoutData(new Vector2(192f, 271f), 20, 1.20f, 0.0698f) },
        { "Rear Left Leg", new BodyPartLayoutData(new Vector2(297f, 384f), 21, 0.80f, -0.0873f) },
        { "Middle Left Leg", new BodyPartLayoutData(new Vector2(222f, 388f), 25, 0.80f, 0.8378f) },
        { "Right Wing", new BodyPartLayoutData(new Vector2(268f, 164f), 30, 1.40f, -0.2618f) },
        { "Right Eye", new BodyPartLayoutData(new Vector2(157f, 254f), 31, 0.75f, -0.0873f) },
        { "Front Left Leg", new BodyPartLayoutData(new Vector2(179f, 375f), 34, 0.80f, -0.2443f, flipHorizontal: true) },
        { "Proboscis", new BodyPartLayoutData(new Vector2(103f, 316f), 36, 0.85f, -1.5184f, flipHorizontal: true) },
        { "Head", new BodyPartLayoutData(new Vector2(119f, 216f), 40, 1.45f, 0.7156f) },
        { "Left Antenna", new BodyPartLayoutData(new Vector2(143f, 161f), 45, 1.05f, -0.0524f, flipHorizontal: true) },
        { "Right Antenna", new BodyPartLayoutData(new Vector2(109f, 156f), 45, 1.05f, -0.5061f, flipHorizontal: true) },
        { "Left Eye", new BodyPartLayoutData(new Vector2(193f, 274f), 50, 0.75f, 0.0698f) },
    };

    protected override IReadOnlyDictionary<string, BodyPartLayoutData> Map => PartLayoutMap;
}

