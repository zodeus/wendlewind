namespace Wendlemire.Scenes.MainGameScene.Gui.Widgets.PawnRenderer.Weather;

/// <summary>
/// Base class for weather effects providing common particle management functionality.
/// </summary>
public abstract class BaseWeatherEffect : IWeatherEffect
{
    protected readonly List<WeatherParticle> Particles = new();
    protected readonly Random Random = new();
    
    protected int Width = 512;
    protected int Height = 512;
    
    protected const int MaxParticles = 500;
    
    public abstract WeatherType WeatherType { get; }
    public abstract int PrePopulateCount { get; }
    public abstract float SpawnRate { get; }
    
    public virtual float ScreenFlashIntensity => 0f;
    
    public virtual bool HasActiveEffects => Particles.Count > 0;
    
    public void SetDimensions(int width, int height)
    {
        Width = width;
        Height = height;
    }
    
    public abstract void SpawnParticle(bool distributeAcrossScreen);
    
    public virtual void Update(float deltaTime)
    {
        UpdateParticles(deltaTime);
        RemoveDeadParticles();
    }
    
    protected virtual void UpdateParticles(float deltaTime)
    {
        foreach (var particle in Particles)
        {
            UpdateParticle(particle, deltaTime);
        }
    }
    
    protected virtual void UpdateParticle(WeatherParticle particle, float deltaTime)
    {
        particle.UpdatePosition(deltaTime);
    }
    
    protected virtual void RemoveDeadParticles()
    {
        for (int i = Particles.Count - 1; i >= 0; i--)
        {
            var particle = Particles[i];
            if (particle.IsOffScreen(Width, Height) || particle.IsExpired)
            {
                Particles.RemoveAt(i);
            }
        }
    }
    
    public abstract void Render(SpriteBatch spriteBatch, float scale);
    
    public virtual void Clear()
    {
        Particles.Clear();
    }
    
    protected bool CanSpawnParticle() => Particles.Count < MaxParticles;
    
    /// <summary>
    /// Gets a random Y position for particle spawning.
    /// </summary>
    protected float GetSpawnY(bool distributeAcrossScreen, bool fromBottom = false)
    {
        if (distributeAcrossScreen)
        {
            return (float)Random.NextDouble() * Height;
        }
        
        if (fromBottom)
        {
            return Height - (float)Random.NextDouble() * 50f;
        }
        
        return (float)Random.NextDouble() * 50f;
    }
    
    /// <summary>
    /// Gets a random X position for particle spawning.
    /// </summary>
    protected float GetSpawnX()
    {
        return (float)Random.NextDouble() * (Width + 100) - 50;
    }
}




