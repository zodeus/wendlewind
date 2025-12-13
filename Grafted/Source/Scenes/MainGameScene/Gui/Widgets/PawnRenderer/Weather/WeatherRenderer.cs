namespace Grafted.Scenes.MainGameScene.Gui.Widgets.PawnRenderer.Weather;

/// <summary>
/// Renders atmospheric weather effects by delegating to specific weather effect implementations.
/// Cycles between rain showers, storms with lightning, snow, and smoke/embers.
/// </summary>
public class WeatherRenderer
{
    private readonly Dictionary<WeatherType, IWeatherEffect> _effects;
    private IWeatherEffect _currentEffect;
    
    private float _weatherTimer;
    private float _spawnAccumulator;
    private bool _hasInitialized;
    private int _currentEffectIndex;
    private const float WeatherCycleDuration = 10f;
    
    private int _width = 512;
    private int _height = 512;
    
    /// <summary>
    /// Current weather type being rendered.
    /// </summary>
    public WeatherType CurrentWeather => _currentEffect.WeatherType;
    
    /// <summary>
    /// Screen flash intensity from lightning (0-1).
    /// </summary>
    public float ScreenFlashIntensity => _currentEffect.ScreenFlashIntensity;
    
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
        
        _currentEffect = _effects[WeatherType.Showers];
        _weatherTimer = WeatherCycleDuration;
    }
    
    /// <summary>
    /// Sets a fixed weather type, disabling automatic weather cycling.
    /// </summary>
    public void SetWeather(WeatherType weatherType)
    {
        if (_effects.TryGetValue(weatherType, out var effect))
        {
            _currentEffect.Clear();
            _currentEffect = effect;
            _currentEffect.SetDimensions(_width, _height);
            _weatherTimer = float.MaxValue; // Disable cycling
            _spawnAccumulator = 0f;
            _hasInitialized = false; // Force re-initialization on next update
        }
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
        
        _weatherTimer -= deltaTime;
        
        // Cycle to next weather type
        if (_weatherTimer <= 0)
        {
            CycleToNextWeather();
        }
        
        // Spawn new particles
        SpawnParticles(deltaTime);
        
        // Update current effect
        _currentEffect.Update(deltaTime);
    }
    
    private void CycleToNextWeather()
    {
        _weatherTimer = WeatherCycleDuration;
        

        _currentEffectIndex++;
        if (_currentEffectIndex >= _effects.Count)
        {
            _currentEffectIndex = 0;
        }
        _currentEffect.Clear();
        _currentEffect = _effects.Values.ElementAt(_currentEffectIndex);
        _currentEffect.SetDimensions(_width, _height);
        _spawnAccumulator = 0f;
        
        PrePopulateParticles();
    }
    
    /// <summary>
    /// Pre-populates particles across the entire screen when weather changes.
    /// This prevents the "wave" effect of all particles starting at the top.
    /// </summary>
    private void PrePopulateParticles()
    {
        for (int i = 0; i < _currentEffect.PrePopulateCount; i++)
        {
            _currentEffect.SpawnParticle(distributeAcrossScreen: true);
        }
    }
    
    private void SpawnParticles(float deltaTime)
    {
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
        _currentEffect.Render(spriteBatch, scale);
    }
    
    /// <summary>
    /// Returns true if there are active weather effects to render.
    /// </summary>
    public bool HasActiveEffects => _currentEffect.HasActiveEffects;
    
    /// <summary>
    /// Gets the time remaining until the next weather type.
    /// </summary>
    public float TimeUntilNextWeather => _weatherTimer;
}
