namespace Wendlewind.Scenes.MainGameScene.Gui.Widgets.CombatWidgets.BodyPartLayouts;

/// <summary>
/// Body part layout for frog pawns.
/// Positions are specified in a 512x512 coordinate space.
/// Frog is viewed from the side with head on the right, in a crouched pose.
/// </summary>
public class FrogBodyPartLayout : BodyPartLayoutBase
{
    // Body part positions (native coordinates)
    private static readonly Dictionary<string, BodyPartLayoutData> PartLayoutMap = new()
    {
        { "Front Right Foot", new BodyPartLayoutData(new Vector2(100f, 424f), 0, 0.55f) },
        { "Rear Right Foot", new BodyPartLayoutData(new Vector2(261f, 433f), 0, 0.55f, -0.2793f) },
        { "Rear Right Leg", new BodyPartLayoutData(new Vector2(245f, 369f), 2, 0.70f, -0.8727f) },
        { "Front Right Leg", new BodyPartLayoutData(new Vector2(104f, 371f), 5, 0.75f, -0.1963f) },
        { "Rear Left Foot", new BodyPartLayoutData(new Vector2(204f, 436f), 11, 0.60f, 0.3142f) },
        { "Torso", new BodyPartLayoutData(new Vector2(154f, 275f), 15, 1.40f, 0.0982f) },
        { "Rear Left Leg", new BodyPartLayoutData(new Vector2(230f, 397f), 16, 0.75f, 0.3927f) },
        { "Front Left Foot", new BodyPartLayoutData(new Vector2(162f, 416f), 23, 0.60f) },
        { "Front Left Leg", new BodyPartLayoutData(new Vector2(176f, 365f), 26, 0.85f, 0.2094f) },
        { "Head", new BodyPartLayoutData(new Vector2(73f, 244f), 43, 1.60f, -0.0982f) },
        { "Right Eye", new BodyPartLayoutData(new Vector2(126f, 273f), 58, 0.20f, -0.1396f, flipHorizontal: true) },
        { "Left Eye", new BodyPartLayoutData(new Vector2(180f, 269f), 75, 0.20f, -0.1963f) },
    };

    protected override IReadOnlyDictionary<string, BodyPartLayoutData> Map => PartLayoutMap;
}


