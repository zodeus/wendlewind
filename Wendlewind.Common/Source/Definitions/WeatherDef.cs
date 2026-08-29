namespace Wendlewind.Definitions;

public class WeatherDef : Def
{
    /// <summary>
    /// The internal weather effect type used by the rendering system.
    /// </summary>
    public WeatherType EffectType = WeatherType.Neutral;
    
    /// <summary>
    /// The display color used in UI elements.
    /// </summary>
    public Color DisplayColor = new(180, 180, 180);
}

