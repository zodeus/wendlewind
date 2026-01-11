namespace Grafted.Scenes.MainGameScene.Gui.Widgets.CombatWidgets.BodyPartLayouts;

/// <summary>
/// Body part layout for RustyDoll pawns.
/// Positions are specified in a 512x512 coordinate space.
/// RustyDoll is a simple creature with just a head and torso - no limbs.
/// </summary>
[UsedImplicitly]
public class RustyDollBodyPartLayout : IBodyPartLayout
{
    // Body part positions (native coordinates) - keyed by Moniker
    private static readonly Dictionary<string, BodyPartLayoutData> PartLayoutMap1 = new()
    {
        { "RustyDollCore_TorsoSocket", new BodyPartLayoutData(new Vector2(-19f, 15f), 0, 1.00f) },
        { "RustyDollMinion_M1", new BodyPartLayoutData(new Vector2(128f, 15f), 0, 1.00f) },
        { "RustyDollMinion_M2", new BodyPartLayoutData(new Vector2(274f, 15f), 0, 1.00f) },
        { "RustyDollMinion_M3", new BodyPartLayoutData(new Vector2(-18f, 137f), 0, 1.00f) },
        { "RustyDollMinion_M4", new BodyPartLayoutData(new Vector2(130f, 137f), 0, 1.00f) },
        { "RustyDollMinion_M5", new BodyPartLayoutData(new Vector2(275f, 136f), 0, 1.00f) },
        { "RustyDollMinion_M6", new BodyPartLayoutData(new Vector2(-18f, 259f), 0, 1.00f) },
        { "RustyDollMinion_M7", new BodyPartLayoutData(new Vector2(130f, 259f), 0, 1.00f) },
        { "RustyDollMinion_M8", new BodyPartLayoutData(new Vector2(274f, 259f), 0, 1.00f) },
    };

    // Body part positions (native coordinates) - keyed by Moniker
    private static readonly Dictionary<string, BodyPartLayoutData> PartLayoutMap2 = new()
    {
        { "RustyDollMinion_M1", new BodyPartLayoutData(new Vector2(-57f, 13f), 0, 1.30f) },
        { "RustyDollMinion_M2", new BodyPartLayoutData(new Vector2(256f, 16f), 0, 1.30f) },
        { "RustyDollCore_TorsoSocket", new BodyPartLayoutData(new Vector2(99f, 12f), 1, 1.30f) },
        { "RustyDollMinion_M5", new BodyPartLayoutData(new Vector2(249f, 167f), 3, 1.00f) },
        { "RustyDollMinion_M4", new BodyPartLayoutData(new Vector2(25f, 165f), 4, 1.00f) },
        { "RustyDollMinion_M3", new BodyPartLayoutData(new Vector2(135f, 153f), 5, 1.00f) },
        { "RustyDollMinion_M8", new BodyPartLayoutData(new Vector2(243f, 314f), 5, 0.75f) },
        { "RustyDollMinion_M7", new BodyPartLayoutData(new Vector2(82f, 315f), 8, 0.75f) },
        { "RustyDollMinion_M6", new BodyPartLayoutData(new Vector2(165f, 319f), 10, 0.75f) },
    };

    public int NativeSize => 512;

    public BodyPartRenderInfo? GetRenderInfo(BodyPart part)
    {
        if (!PartLayoutMap2.TryGetValue(part.InternalLabel ?? "", out var layoutData))
        {
            Log.Error($"RustyDollBodyPartLayout: No layout data found for part: {part.BodyPartDef.Moniker}");
            return null;
        }

        if (part.Image == null)
        {
            return null;
        }

        return new BodyPartRenderInfo(part.Image, layoutData);
    }
}
