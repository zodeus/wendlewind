namespace Wendlewind.Scenes.MainGameScene.Gui.Widgets.PawnRenderer.Weather;

/// <summary>
/// Magical fireflies/spirit wisps drifting with pulsing bioluminescent glow.
/// Creates an enchanted twilight atmosphere.
/// </summary>
public class FirefliesEffect : BaseWeatherEffect
{
    public override WeatherType WeatherType => WeatherType.Fireflies;
    public override int PrePopulateCount => 18;
    public override float SpawnRate => 4f;
    
    public override void SpawnParticle(bool distributeAcrossScreen)
    {
        if (!CanSpawnParticle()) return;
        
        // Fireflies spawn anywhere on screen, not just top
        var particle = new WeatherParticle
        {
            Position = new Vector2(
                (float)Random.NextDouble() * Width,
                distributeAcrossScreen 
                    ? (float)Random.NextDouble() * Height 
                    : Height * 0.3f + (float)Random.NextDouble() * Height * 0.6f
            ),
            // Slow, lazy movement - can go any direction
            Velocity = new Vector2(
                -20f + (float)Random.NextDouble() * 40f,
                -15f + (float)Random.NextDouble() * 30f
            ),
            Size = 2f + (float)Random.NextDouble() * 3f,
            Opacity = 0f, // Start dark, will pulse in
            WobblePhase = (float)Random.NextDouble() * MathF.PI * 2,
            Wobble = 30f + (float)Random.NextDouble() * 40f,
            FlickerPhase = (float)Random.NextDouble() * MathF.PI * 2,
            MaxLifetime = 4f + (float)Random.NextDouble() * 4f,
            Lifetime = 0f, // Counts up for fireflies
            IsEmber = Random.NextDouble() < 0.3 // 30% are warmer amber, 70% are green-yellow
        };
        
        particle.Lifetime = particle.MaxLifetime;
        
        Particles.Add(particle);
    }
    
    protected override void UpdateParticle(WeatherParticle particle, float deltaTime)
    {
        // Fireflies wander - occasionally change direction
        particle.WobblePhase += deltaTime * 1.5f;
        
        // Organic wandering motion
        float wanderX = MathF.Sin(particle.WobblePhase) * particle.Wobble;
        float wanderY = MathF.Cos(particle.WobblePhase * 0.7f) * particle.Wobble * 0.6f;
        
        particle.Position.X += (particle.Velocity.X + wanderX) * deltaTime;
        particle.Position.Y += (particle.Velocity.Y + wanderY) * deltaTime;
        
        // Slowly drift velocity for more organic movement
        particle.Velocity.X += (-10f + (float)Random.NextDouble() * 20f) * deltaTime;
        particle.Velocity.Y += (-8f + (float)Random.NextDouble() * 16f) * deltaTime;
        
        // Clamp velocity so they don't get too fast
        particle.Velocity.X = Math.Clamp(particle.Velocity.X, -35f, 35f);
        particle.Velocity.Y = Math.Clamp(particle.Velocity.Y, -25f, 25f);
        
        // Pulsing glow - breathing effect
        particle.FlickerPhase += deltaTime * (2f + (float)Random.NextDouble() * 2f);
        
        // Lifetime-based fade (fade in at start, fade out at end)
        particle.Lifetime -= deltaTime;
        float lifeRatio = particle.Lifetime / particle.MaxLifetime;
        float fadeEnvelope = lifeRatio < 0.2f 
            ? lifeRatio / 0.2f  // Fade out in last 20%
            : lifeRatio > 0.8f 
                ? (1f - lifeRatio) / 0.2f  // Fade in during first 20%
                : 1f;
        
        // Combine envelope with pulsing glow
        float pulse = 0.4f + MathF.Sin(particle.FlickerPhase) * 0.6f;
        particle.Opacity = fadeEnvelope * pulse;
    }
    
    public override void Render(SpriteBatch spriteBatch, float scale)
    {
        var pixel = Core.Graphics.PixelTexture;
        
        foreach (var particle in Particles)
        {
            if (particle.Opacity < 0.05f) continue;
            
            var scaledPos = particle.Position * scale;
            var scaledSize = particle.Size * scale;
            
            // Color varies between yellow-green and warm amber
            Color glowColor;
            if (particle.IsEmber)
            {
                // Warm amber firefly
                glowColor = new Color(255, 180, 60) * particle.Opacity;
            }
            else
            {
                // Yellow-green bioluminescent
                glowColor = new Color(180, 255, 100) * particle.Opacity;
            }
            
            // Outer glow (large, soft)
            var outerGlowSize = (int)(scaledSize * 4f);
            var outerGlowRect = new Rectangle(
                (int)(scaledPos.X - outerGlowSize / 2f),
                (int)(scaledPos.Y - outerGlowSize / 2f),
                outerGlowSize,
                outerGlowSize
            );
            spriteBatch.Draw(pixel, outerGlowRect, pixel.SourceRect, glowColor * 0.15f);
            
            // Middle glow
            var midGlowSize = (int)(scaledSize * 2.5f);
            var midGlowRect = new Rectangle(
                (int)(scaledPos.X - midGlowSize / 2f),
                (int)(scaledPos.Y - midGlowSize / 2f),
                midGlowSize,
                midGlowSize
            );
            spriteBatch.Draw(pixel, midGlowRect, pixel.SourceRect, glowColor * 0.35f);
            
            // Bright core
            var coreSize = Math.Max(2, (int)scaledSize);
            var coreRect = new Rectangle(
                (int)(scaledPos.X - coreSize / 2f),
                (int)(scaledPos.Y - coreSize / 2f),
                coreSize,
                coreSize
            );
            
            // Core is brighter/whiter
            var coreColor = particle.IsEmber 
                ? new Color(255, 240, 200) * particle.Opacity 
                : new Color(240, 255, 220) * particle.Opacity;
            spriteBatch.Draw(pixel, coreRect, pixel.SourceRect, coreColor);
        }
    }
}

