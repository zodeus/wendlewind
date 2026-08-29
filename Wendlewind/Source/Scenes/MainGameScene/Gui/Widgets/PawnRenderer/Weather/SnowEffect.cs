namespace Wendlewind.Scenes.MainGameScene.Gui.Widgets.PawnRenderer.Weather;

/// <summary>
/// Snowfall weather effect with drifting snowflakes.
/// </summary>
public class SnowEffect : BaseWeatherEffect
{
    public override WeatherType WeatherType => WeatherType.Snow;
    public override int PrePopulateCount => 12;
    public override float SpawnRate => 8f;
    
    public override void SpawnParticle(bool distributeAcrossScreen)
    {
        if (!CanSpawnParticle()) return;
        
        var particle = new WeatherParticle
        {
            Position = new Vector2(GetSpawnX(), GetSpawnY(distributeAcrossScreen)),
            Velocity = new Vector2(
                -10f + (float)Random.NextDouble() * 20f,
                40f + (float)Random.NextDouble() * 30f
            ),
            Size = 2f + (float)Random.NextDouble() * 4f,
            Opacity = 0.6f + (float)Random.NextDouble() * 0.4f,
            Wobble = 20f + (float)Random.NextDouble() * 40f,
            WobblePhase = (float)Random.NextDouble() * MathF.PI * 2
        };
        
        Particles.Add(particle);
    }
    
    protected override void UpdateParticle(WeatherParticle particle, float deltaTime)
    {
        base.UpdateParticle(particle, deltaTime);
        
        // Snowflakes wobble horizontally
        particle.WobblePhase += deltaTime * 3f;
        particle.Position.X += MathF.Sin(particle.WobblePhase) * particle.Wobble * deltaTime;
    }
    
    public override void Render(SpriteBatch spriteBatch, float scale)
    {
        var pixel = Core.Graphics.PixelTexture;
        
        foreach (var particle in Particles)
        {
            var scaledPos = particle.Position * scale;
            var scaledSize = particle.Size * scale;
            
            // White snowflakes with slight blue tint
            var color = new Color(240, 245, 255) * particle.Opacity;
            
            // Snowflakes are more square/circular
            var snowSize = Math.Max(2, (int)scaledSize);
            var snowRect = new Rectangle(
                (int)(scaledPos.X - snowSize / 2f),
                (int)(scaledPos.Y - snowSize / 2f),
                snowSize,
                snowSize
            );
            
            spriteBatch.Draw(pixel, snowRect, pixel.SourceRect, color);
            
            // Add a softer glow for larger snowflakes
            if (particle.Size > 3f)
            {
                var glowSize = snowSize + 2;
                var glowRect = new Rectangle(
                    (int)(scaledPos.X - glowSize / 2f),
                    (int)(scaledPos.Y - glowSize / 2f),
                    glowSize,
                    glowSize
                );
                spriteBatch.Draw(pixel, glowRect, pixel.SourceRect, color * 0.3f);
            }
        }
    }
}


