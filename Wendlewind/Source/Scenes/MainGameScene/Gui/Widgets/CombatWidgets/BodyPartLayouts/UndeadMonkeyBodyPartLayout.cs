namespace Wendlewind.Scenes.MainGameScene.Gui.Widgets.CombatWidgets.BodyPartLayouts;

/// <summary>
/// Body part layout for undead monkey pawns.
/// Positions are specified in a 512x512 coordinate space.
/// Monkey is viewed from the front, slightly hunched forward.
/// </summary>
[UsedImplicitly]
public class UndeadMonkeyBodyPartLayout : BodyPartLayoutBase
{
    // Body part positions (native coordinates)
    private static readonly Dictionary<string, BodyPartLayoutData> PartLayoutMap = new()
    {
        { "Tail", new BodyPartLayoutData(new Vector2(248f, 280f), 0, 0.55f, -1.0647f) },
        { "Left Foot", new BodyPartLayoutData(new Vector2(240f, 427f), 5, 0.35f, 0.2618f) },
        { "Right Foot", new BodyPartLayoutData(new Vector2(170f, 437f), 5, 0.30f, 0.3142f) },
        { "Torso", new BodyPartLayoutData(new Vector2(166f, 244f), 20, 0.65f, -0.0349f) },
        { "Right Arm", new BodyPartLayoutData(new Vector2(94f, 168f), 22, 0.55f, 2.5133f) },
        { "Right Hand", new BodyPartLayoutData(new Vector2(81f, 105f), 25, 0.30f, 2.4086f) },
        { "Right Leg", new BodyPartLayoutData(new Vector2(162f, 371f), 25, 0.40f, -0.0349f) },
        { "Left Leg", new BodyPartLayoutData(new Vector2(214f, 364f), 28, 0.45f, -0.4189f) },
        { "Head", new BodyPartLayoutData(new Vector2(198f, 172f), 30, 0.35f, -0.0800f) },
        { "Left Arm", new BodyPartLayoutData(new Vector2(254f, 171f), 32, 0.55f, -2.4260f, flipHorizontal: true) },
        { "Left Eye", new BodyPartLayoutData(new Vector2(230f, 194f), 35, 0.20f) },
        { "Right Eye", new BodyPartLayoutData(new Vector2(207f, 195f), 35, 0.20f) },
        { "Left Hand", new BodyPartLayoutData(new Vector2(333f, 120f), 43, 0.25f, -2.6704f, flipHorizontal: true) },
    };

    protected override IReadOnlyDictionary<string, BodyPartLayoutData> Map => PartLayoutMap;
}
