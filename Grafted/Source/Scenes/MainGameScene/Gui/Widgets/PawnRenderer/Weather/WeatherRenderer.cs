namespace Grafted.Scenes.MainGameScene.Gui.Widgets.PawnRenderer.Weather;

/// <summary>
/// Renders atmospheric weather effects by delegating to specific weather effect implementations.
/// Cycles between rain showers, storms with lightning, snow, and smoke/embers.
/// </summary>
public class WeatherRenderer
{
    private readonly Dictionary<WeatherType, IWeatherEffect> _effects;
    private IWeatherEffect? _currentEffect;
    
    private float _spawnAccumulator;
    private bool _hasInitialized;
    
    private int _width = 512;
    private int _height = 512;
    
    /// <summary>
    /// Current weather type being rendered.
    /// </summary>
    public WeatherType? CurrentWeather => _currentEffect?.WeatherType;
    
    /// <summary>
    /// Screen flash intensity from lightning (0-1).
    /// </summary>
    public float ScreenFlashIntensity => _currentEffect?.ScreenFlashIntensity ?? 0f;
    
    public WeatherRenderer()
    {
        _effects = new Dictionary<WeatherType, IWeatherEffect>
        {
            { WeatherType.Showers, new ShowersEffect() },
            { WeatherType.Storm, new StormEffect() },
            { WeatherType.Snow, new SnowEffect() },
            { WeatherType.SmokeEmbers, new SmokeEmbersEffect() },
            { WeatherType.BloodRain, new BloodRainEffect() },
            { WeatherType.Fireflies, new FirefliesEffect() },
            { WeatherType.FallingLeaves, new FallingLeavesEffect() },
            { WeatherType.HallowedRain, new HallowedRainEffect() },
            { WeatherType.AcidDrips, new AcidDripsEffect() },
            { WeatherType.Neutral, new NeutralEffect() }
        };
    }
    
    /// <summary>
    /// Sets a fixed weather type, disabling automatic weather cycling.
    /// </summary>
    public void SetWeather(WeatherType weatherType)
    {
        if (_effects.TryGetValue(weatherType, out var effect))
        {
            _currentEffect?.Clear();
            _currentEffect = effect;
            _currentEffect.SetDimensions(_width, _height);
            _spawnAccumulator = 0f;
            _hasInitialized = false; // Force re-initialization on next update
        }
    }
    
    /// <summary>
    /// Sets weather from a WeatherDef.
    /// </summary>
    public void SetWeather(WeatherDef weatherDef)
    {
        SetWeather(weatherDef.EffectType);
    }
    
    /// <summary>
    /// Sets the render area dimensions.
    /// </summary>
    public void SetDimensions(int width, int height)
    {
        _width = width;
        _height = height;
        
        foreach (var effect in _effects.Values)
        {
            effect.SetDimensions(width, height);
        }
    }
    
    /// <summary>
    /// Updates weather simulation and cycles between weather types.
    /// </summary>
    public void Update(float deltaTime)
    {
        // Pre-populate on first update to avoid initial wave effect
        if (!_hasInitialized)
        {
            _hasInitialized = true;
            PrePopulateParticles();
        }
        
        // Spawn new particles
        SpawnParticles(deltaTime);
        
        // Update current effect
        _currentEffect?.Update(deltaTime);
    }
    
    /// <summary>
    /// Pre-populates particles across the entire screen when weather changes.
    /// This prevents the "wave" effect of all particles starting at the top.
    /// </summary>
    private void PrePopulateParticles()
    {
        if (_currentEffect == null) return;
        for (int i = 0; i < _currentEffect.PrePopulateCount; i++)
        {
            _currentEffect.SpawnParticle(distributeAcrossScreen: true);
        }
    }
    
    private void SpawnParticles(float deltaTime)
    {
        if (_currentEffect == null) return;
        _spawnAccumulator += _currentEffect.SpawnRate * deltaTime;
        
        while (_spawnAccumulator >= 1f)
        {
            _currentEffect.SpawnParticle(distributeAcrossScreen: false);
            _spawnAccumulator -= 1f;
        }
    }
    
    /// <summary>
    /// Renders the weather effects.
    /// </summary>
    public void Render(SpriteBatch spriteBatch, float scale = 1f)
    {
        _currentEffect?.Render(spriteBatch, scale);
    }
    
    /// <summary>
    /// Returns true if there are active weather effects to render.
    /// </summary>
    public bool HasActiveEffects => _currentEffect?.HasActiveEffects ?? false;
}
