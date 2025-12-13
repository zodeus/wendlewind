namespace Grafted.Scenes.MainGameScene.Gui.Widgets.PawnRenderer.Weather;

/// <summary>
/// Interface for weather effects that can spawn, update, and render particles.
/// </summary>
public interface IWeatherEffect
{
    /// <summary>
    /// The type of weather this effect represents.
    /// </summary>
    WeatherType WeatherType { get; }
    
    /// <summary>
    /// Number of particles to spawn when pre-populating the screen.
    /// </summary>
    int PrePopulateCount { get; }
    
    /// <summary>
    /// Spawn rate in particles per second.
    /// </summary>
    float SpawnRate { get; }
    
    /// <summary>
    /// Returns true if there are active effects to render.
    /// </summary>
    bool HasActiveEffects { get; }
    
    /// <summary>
    /// Screen flash intensity (0-1), used for lightning effects.
    /// </summary>
    float ScreenFlashIntensity { get; }
    
    /// <summary>
    /// Sets the render area dimensions.
    /// </summary>
    void SetDimensions(int width, int height);
    
    /// <summary>
    /// Spawns a new particle, optionally distributed across the screen.
    /// </summary>
    void SpawnParticle(bool distributeAcrossScreen);
    
    /// <summary>
    /// Updates all particles and effect-specific logic.
    /// </summary>
    void Update(float deltaTime);
    
    /// <summary>
    /// Renders all particles and effect-specific visuals.
    /// </summary>
    void Render(SpriteBatch spriteBatch, float scale);
    
    /// <summary>
    /// Clears all particles and resets the effect state.
    /// </summary>
    void Clear();
}


