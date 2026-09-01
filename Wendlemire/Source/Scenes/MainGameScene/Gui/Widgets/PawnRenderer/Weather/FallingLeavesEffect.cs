namespace Wendlemire.Scenes.MainGameScene.Gui.Widgets.PawnRenderer.Weather;

/// <summary>
/// Autumn leaves tumbling and spiraling down with occasional gusts.
/// Creates a melancholic, seasonal atmosphere.
/// </summary>
public class FallingLeavesEffect : BaseWeatherEffect
{
    private float _gustTimer;
    private float _gustStrength;
    private float _gustDirection;
    
    // Warm autumn palette
    private readonly Color[] _leafColors = new[]
    {
        new Color(180, 70, 30),   // Burnt orange
        new Color(200, 50, 40),   // Crimson
        new Color(160, 120, 40),  // Golden brown
        new Color(220, 160, 50),  // Amber yellow
        new Color(140, 80, 50),   // Dark brown
        new Color(190, 100, 60),  // Rust
    };
    
    public override WeatherType WeatherType => WeatherType.FallingLeaves;
    public override int PrePopulateCount => 25;
    public override float SpawnRate => 10f;
    
    public override void SpawnParticle(bool distributeAcrossScreen)
    {
        if (!CanSpawnParticle()) return;
        
        var particle = new WeatherParticle
        {
            Position = new Vector2(GetSpawnX(), GetSpawnY(distributeAcrossScreen)),
            // Leaves fall slower than rain, with horizontal drift
            Velocity = new Vector2(
                -30f + (float)Random.NextDouble() * 60f,
                50f + (float)Random.NextDouble() * 40f
            ),
            // Larger particles for visible leaf shapes
            Size = 4f + (float)Random.NextDouble() * 6f,
            Opacity = 0.7f + (float)Random.NextDouble() * 0.3f,
            // Rotation phase (stored in WobblePhase)
            WobblePhase = (float)Random.NextDouble() * MathF.PI * 2,
            // Rotation speed (stored in Wobble)
            Wobble = 2f + (float)Random.NextDouble() * 3f,
            // Spiral/tumble frequency (stored in FlickerPhase)
            FlickerPhase = (float)Random.NextDouble() * MathF.PI * 2,
            // Color index (encoded in Lifetime since leaves don't expire by time)
            MaxLifetime = Random.Next(_leafColors.Length),
        };
        
        Particles.Add(particle);
    }
    
    public override void Update(float deltaTime)
    {
        base.Update(deltaTime);
        
        // Occasional gusts of wind
        _gustTimer -= deltaTime;
        if (_gustTimer <= 0)
        {
            // Start a new gust
            _gustTimer = 3f + (float)Random.NextDouble() * 5f;
            _gustStrength = 60f + (float)Random.NextDouble() * 80f;
            _gustDirection = Random.NextDouble() < 0.5 ? -1f : 1f;
        }
        
        // Decay gust strength
        _gustStrength = Math.Max(0, _gustStrength - deltaTime * 40f);
    }
    
    protected override void UpdateParticle(WeatherParticle particle, float deltaTime)
    {
        // Rotation
        particle.WobblePhase += particle.Wobble * deltaTime;
        
        // Tumbling spiral motion
        particle.FlickerPhase += deltaTime * 3f;
        float spiralX = MathF.Sin(particle.FlickerPhase) * 40f;
        float spiralY = MathF.Cos(particle.FlickerPhase * 0.5f) * 15f;
        
        // Apply gust
        float gustEffect = _gustStrength * _gustDirection * deltaTime;
        
        // Complex falling path
        particle.Position.X += (particle.Velocity.X + spiralX + gustEffect) * deltaTime;
        particle.Position.Y += (particle.Velocity.Y + spiralY) * deltaTime;
        
        // Leaves can catch air and slow down momentarily
        if (MathF.Sin(particle.FlickerPhase * 2f) > 0.8f)
        {
            particle.Position.Y -= 20f * deltaTime; // Brief lift
        }
    }
    
    public override void Render(SpriteBatch spriteBatch, float scale)
    {
        var pixel = Core.Graphics.PixelTexture;
        
        foreach (var particle in Particles)
        {
            var scaledPos = particle.Position * scale;
            var scaledSize = particle.Size * scale;
            
            // Get leaf color from the encoded index
            int colorIndex = (int)particle.MaxLifetime % _leafColors.Length;
            var leafColor = _leafColors[colorIndex] * particle.Opacity;
            
            // Calculate rotation for visual variety
            float rotation = particle.WobblePhase;
            
            // Leaves are rendered as small elongated rectangles that rotate
            // This creates a simple but effective leaf silhouette
            int leafWidth = Math.Max(2, (int)(scaledSize * 0.6f));
            int leafHeight = Math.Max(3, (int)scaledSize);
            
            // Main leaf body
            var leafRect = new Rectangle(
                (int)scaledPos.X,
                (int)scaledPos.Y,
                leafWidth,
                leafHeight
            );
            
            spriteBatch.Draw(
                pixel, 
                leafRect, 
                pixel.SourceRect, 
                leafColor, 
                rotation, 
                new Vector2(leafWidth / 2f, leafHeight / 2f), 
                SpriteEffects.None, 
                0
            );
            
            // Add a stem/vein for larger leaves
            if (particle.Size > 6f)
            {
                var stemColor = leafColor * 0.6f;
                int stemWidth = Math.Max(1, leafWidth / 3);
                int stemHeight = Math.Max(2, leafHeight - 2);
                
                var stemRect = new Rectangle(
                    (int)scaledPos.X,
                    (int)scaledPos.Y,
                    stemWidth,
                    stemHeight
                );
                
                spriteBatch.Draw(
                    pixel,
                    stemRect,
                    pixel.SourceRect,
                    stemColor,
                    rotation,
                    new Vector2(stemWidth / 2f, stemHeight / 2f),
                    SpriteEffects.None,
                    0
                );
            }
        }
    }
    
    public override void Clear()
    {
        base.Clear();
        _gustTimer = 0;
        _gustStrength = 0;
    }
}

