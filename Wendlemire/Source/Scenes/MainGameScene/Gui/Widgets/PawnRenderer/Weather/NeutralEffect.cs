namespace Wendlemire.Scenes.MainGameScene.Gui.Widgets.PawnRenderer.Weather;

/// <summary>
/// Twinkling stars scattered across the night sky.
/// Creates a serene, magical atmosphere with gentle starlight.
/// </summary>
public class NeutralEffect : BaseWeatherEffect
{
    // Star colors - from cool blues to warm whites and hints of gold
    private readonly Color[] _starColors = new[]
    {
        new Color(180, 180, 190),  // Dim white
        new Color(140, 160, 200),  // Muted blue-white
        new Color(120, 140, 180),  // Dusty blue
        new Color(170, 165, 155),  // Faded warm
        new Color(160, 145, 120),  // Dim gold
        new Color(150, 170, 190),  // Pale steel blue
        new Color(165, 140, 110),  // Muted amber
    };
    
    public override WeatherType WeatherType => WeatherType.Neutral;
    public override int PrePopulateCount => 20; // Nice field of stars
    public override float SpawnRate => 4f; // Slow spawning - stars are persistent
    
    public override void SpawnParticle(bool distributeAcrossScreen)
    {
        if (!CanSpawnParticle()) return;
        
        // Stars appear across the entire sky
        var particle = new WeatherParticle
        {
            Position = new Vector2(
                (float)Random.NextDouble() * Width,
                (float)Random.NextDouble() * Height
            ),
            // Stars are stationary - only very slight drift
            Velocity = new Vector2(
                -0.5f + (float)Random.NextDouble() * 1f,
                0.2f + (float)Random.NextDouble() * 0.5f
            ),
            // Varying star sizes - mostly small with some brighter ones
            Size = Random.NextDouble() < 0.85f 
                ? 0.8f + (float)Random.NextDouble() * 1.2f  // Small stars
                : 1.5f + (float)Random.NextDouble() * 1.5f, // Brighter stars
            // Base opacity before twinkle modulation
            Opacity = 0.4f + (float)Random.NextDouble() * 0.5f,
            // Twinkle phase - each star twinkles at its own rhythm
            WobblePhase = (float)Random.NextDouble() * MathF.PI * 2,
            // Twinkle speed variation
            Wobble = 1.5f + (float)Random.NextDouble() * 2.5f,
            // Secondary twinkle phase for complex shimmer
            FlickerPhase = (float)Random.NextDouble() * MathF.PI * 2,
            // Color index encoded in MaxLifetime
            MaxLifetime = Random.Next(_starColors.Length),
            // Stars last a long time
            Lifetime = 15f + (float)Random.NextDouble() * 10f,
        };
        
        Particles.Add(particle);
    }
    
    protected override void UpdateParticle(WeatherParticle particle, float deltaTime)
    {
        // Very slow drift - stars mostly stay in place
        particle.Position.X += particle.Velocity.X * deltaTime;
        particle.Position.Y += particle.Velocity.Y * deltaTime;
        
        // Primary twinkle oscillation
        particle.WobblePhase += deltaTime * particle.Wobble;
        
        // Secondary faster flicker for sparkle effect
        particle.FlickerPhase += deltaTime * (particle.Wobble * 1.7f);
        
        // Complex twinkle: combine two sine waves for organic shimmer
        float primaryTwinkle = MathF.Sin(particle.WobblePhase);
        float secondaryTwinkle = MathF.Sin(particle.FlickerPhase * 2.3f) * 0.3f;
        float combinedTwinkle = (primaryTwinkle + secondaryTwinkle) * 0.5f + 0.5f;
        
        // Occasional bright flash (shooting star moment)
        float flashChance = MathF.Sin(particle.WobblePhase * 0.1f);
        float flash = flashChance > 0.98f ? 1.5f : 1f;
        
        // Lifetime fade
        particle.Lifetime -= deltaTime;
        float lifeRatio = Math.Max(0, particle.Lifetime / 25f);
        float fadeIn = Math.Min(1f, (25f - particle.Lifetime) / 2f); // Fade in over 2 seconds
        float fadeOut = lifeRatio < 0.1f ? lifeRatio / 0.1f : 1f;
        
        // Calculate final opacity with twinkle
        float baseOpacity = 0.4f + (particle.Size / 3f) * 0.4f; // Bigger stars are brighter
        particle.Opacity = baseOpacity * combinedTwinkle * flash * fadeIn * fadeOut;
        particle.Opacity = Math.Clamp(particle.Opacity, 0f, 0.95f);
    }
    
    protected override void RemoveDeadParticles()
    {
        for (int i = Particles.Count - 1; i >= 0; i--)
        {
            var particle = Particles[i];
            if (particle.IsOffScreen(Width, Height) || particle.Lifetime <= 0)
            {
                Particles.RemoveAt(i);
            }
        }
    }
    
    public override void Render(SpriteBatch spriteBatch, float scale)
    {
        var pixel = Core.Graphics.PixelTexture;
        
        foreach (var particle in Particles)
        {
            if (particle.Opacity < 0.02f) continue;
            
            var scaledPos = particle.Position * scale;
            var scaledSize = particle.Size * scale;
            
            // Get star color from encoded index
            int colorIndex = (int)particle.MaxLifetime % _starColors.Length;
            var starColor = _starColors[colorIndex] * particle.Opacity;
            
            // Outer glow - soft halo around brighter stars
            if (scaledSize >= 1.5f)
            {
                var glowSize = (int)(scaledSize * 3f);
                var glowRect = new Rectangle(
                    (int)(scaledPos.X - glowSize / 2f),
                    (int)(scaledPos.Y - glowSize / 2f),
                    glowSize,
                    glowSize
                );
                spriteBatch.Draw(pixel, glowRect, pixel.SourceRect, starColor * 0.15f);
            }
            
            // Star cross pattern for larger stars - creates that classic star twinkle shape
            var coreSize = Math.Max(1, (int)scaledSize);
            if (coreSize >= 2 && particle.Opacity > 0.3f)
            {
                // Horizontal ray
                var hRaySize = (int)(scaledSize * 1.8f);
                var hRayRect = new Rectangle(
                    (int)(scaledPos.X - hRaySize / 2f),
                    (int)(scaledPos.Y),
                    hRaySize,
                    1
                );
                spriteBatch.Draw(pixel, hRayRect, pixel.SourceRect, starColor * 0.5f);
                
                // Vertical ray
                var vRaySize = (int)(scaledSize * 1.8f);
                var vRayRect = new Rectangle(
                    (int)(scaledPos.X),
                    (int)(scaledPos.Y - vRaySize / 2f),
                    1,
                    vRaySize
                );
                spriteBatch.Draw(pixel, vRayRect, pixel.SourceRect, starColor * 0.5f);
            }
            
            // Core bright point
            var coreRect = new Rectangle(
                (int)(scaledPos.X - coreSize / 2f),
                (int)(scaledPos.Y - coreSize / 2f),
                coreSize,
                coreSize
            );
            spriteBatch.Draw(pixel, coreRect, pixel.SourceRect, starColor);
        }
    }
}
