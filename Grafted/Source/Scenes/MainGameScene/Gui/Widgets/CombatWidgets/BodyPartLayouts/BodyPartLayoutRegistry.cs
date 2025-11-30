namespace Grafted.Scenes.MainGameScene.Gui.Widgets.CombatWidgets.BodyPartLayouts;

/// <summary>
/// Registry for body part layouts. Provides the appropriate layout for a given pawn body.
/// </summary>
public static class BodyPartLayoutRegistry
{
    private static readonly List<IBodyPartLayout> _layouts = new();
    private static bool _initialized;
    
    /// <summary>
    /// Initializes the registry with all available layouts.
    /// </summary>
    public static void Initialize()
    {
        if (_initialized) return;
        
        // Register layouts in priority order (more specific first)
        _layouts.Add(new HumanBodyPartLayout());
        _layouts.Add(new GhoulBodyPartLayout());
        _layouts.Add(new PigBodyPartLayout());
        _layouts.Add(new RabbitBodyPartLayout());
        _layouts.Add(new WolfBodyPartLayout());
        _layouts.Add(new FrogBodyPartLayout());
        _layouts.Add(new TreebornBodyPartLayout());
        _layouts.Add(new MushroomBodyPartLayout());
        
        _initialized = true;
    }
    
    /// <summary>
    /// Gets the appropriate layout for the given pawn body.
    /// </summary>
    /// <returns>The layout, or null if no layout supports this body type.</returns>
    public static IBodyPartLayout? GetLayoutFor(PawnBody body)
    {
        Initialize();
        
        foreach (var layout in _layouts)
        {
            if (layout.SupportsBody(body))
            {
                return layout;
            }
        }
        
        return null;
    }
}

