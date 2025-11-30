namespace Grafted.Scenes.MainGameScene.Gui.Widgets.CombatWidgets.BodyPartLayouts;

/// <summary>
/// Registry for body part layouts. Provides the appropriate layout for a given pawn body.
/// </summary>
public static class BodyPartLayoutRegistry
{
    /// <summary>
    /// Gets the appropriate layout for the given pawn body from its BodyDef.
    /// </summary>
    /// <returns>The layout, or null if no layout is configured for this body type.</returns>
    public static IBodyPartLayout? GetLayoutFor(PawnBody body)
    {
        return body.Def.Layout;
    }
}

