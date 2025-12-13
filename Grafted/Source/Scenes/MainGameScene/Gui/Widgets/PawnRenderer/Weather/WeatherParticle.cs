namespace Grafted.Scenes.MainGameScene.Gui.Widgets.PawnRenderer.Weather;

/// <summary>
/// Represents a single precipitation particle (rain drop, snowflake, smoke, or ember).
/// </summary>
public class WeatherParticle
{
    public Vector2 Position;
    public Vector2 Velocity;
    public float Size;
    public float Opacity;
    public float Rotation;
    public float RotationSpeed;
    public float Wobble;
    public float WobblePhase;
    public bool IsEmber;
    public float FlickerPhase;
    public float Lifetime;
    public float MaxLifetime;
    
    /// <summary>
    /// Basic position update - moves particle by velocity.
    /// </summary>
    public void UpdatePosition(float deltaTime)
    {
        Position += Velocity * deltaTime;
    }
    
    /// <summary>
    /// Checks if particle is off-screen.
    /// </summary>
    public bool IsOffScreen(int width, int height, float margin = 50f)
    {
        return Position.Y > height + margin || 
               Position.Y < -margin ||
               Position.X > width + margin || 
               Position.X < -margin;
    }
    
    /// <summary>
    /// Checks if particle lifetime has expired.
    /// </summary>
    public bool IsExpired => MaxLifetime > 0 && Lifetime <= 0;
}


