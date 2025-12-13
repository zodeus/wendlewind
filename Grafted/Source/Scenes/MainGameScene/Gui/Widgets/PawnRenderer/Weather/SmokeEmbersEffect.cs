namespace Grafted.Scenes.MainGameScene.Gui.Widgets.PawnRenderer.Weather;

/// <summary>
/// Rising smoke and floating ember weather effect.
/// </summary>
public class SmokeEmbersEffect : BaseWeatherEffect
{
    public override WeatherType WeatherType => WeatherType.SmokeEmbers;
    public override int PrePopulateCount => 40;
    public override float SpawnRate => 35f;
    
    public override void SpawnParticle(bool distributeAcrossScreen)
    {
        if (!CanSpawnParticle()) return;
        
        // 30% chance to be an ember, 70% smoke
        bool isEmber = Random.NextDouble() < 0.3;
        
        var particle = new WeatherParticle
        {
            Position = new Vector2(GetSpawnX(), GetSpawnY(distributeAcrossScreen, fromBottom: true)),
            IsEmber = isEmber,
            WobblePhase = (float)Random.NextDouble() * MathF.PI * 2,
            Opacity = 0.6f + (float)Random.NextDouble() * 0.3f
        };
        
        if (isEmber)
        {
            // Embers: small, bright, rise faster
            particle.Velocity = new Vector2(
                -15f + (float)Random.NextDouble() * 30f,
                -80f - (float)Random.NextDouble() * 60f  // Negative Y = upward
            );
            particle.Size = 1.5f + (float)Random.NextDouble() * 2f;
            particle.Wobble = 15f + (float)Random.NextDouble() * 25f;
            particle.MaxLifetime = 2f + (float)Random.NextDouble() * 2f;
            particle.Lifetime = particle.MaxLifetime;
            particle.FlickerPhase = (float)Random.NextDouble() * MathF.PI * 2;
        }
        else
        {
            // Smoke: larger, slower, more drift
            particle.Velocity = new Vector2(
                -20f + (float)Random.NextDouble() * 40f,
                -40f - (float)Random.NextDouble() * 30f  // Negative Y = upward
            );
            particle.Size = 6f + (float)Random.NextDouble() * 10f;
            particle.Wobble = 25f + (float)Random.NextDouble() * 35f;
            particle.MaxLifetime = 3f + (float)Random.NextDouble() * 3f;
            particle.Lifetime = particle.MaxLifetime;
        }
        
        Particles.Add(particle);
    }
    
    protected override void UpdateParticle(WeatherParticle particle, float deltaTime)
    {
        base.UpdateParticle(particle, deltaTime);
        
        // Smoke and embers drift and wobble as they rise
        particle.WobblePhase += deltaTime * 2f;
        particle.Position.X += MathF.Sin(particle.WobblePhase) * particle.Wobble * deltaTime;
        
        if (particle.IsEmber)
        {
            // Embers flicker
            particle.FlickerPhase += deltaTime * 15f;
        }
        
        // Fade out over lifetime
        if (particle.MaxLifetime > 0)
        {
            particle.Lifetime -= deltaTime;
            particle.Opacity = Math.Clamp(particle.Lifetime / particle.MaxLifetime, 0f, 1f) * 0.8f;
        }
    }
    
    public override void Render(SpriteBatch spriteBatch, float scale)
    {
        var pixel = Core.Graphics.PixelTexture;
        
        foreach (var particle in Particles)
        {
            var scaledPos = particle.Position * scale;
            var scaledSize = particle.Size * scale;
            
            if (particle.IsEmber)
            {
                RenderEmber(spriteBatch, pixel, particle, scaledPos, scaledSize);
            }
            else
            {
                RenderSmoke(spriteBatch, pixel, particle, scaledPos, scaledSize);
            }
        }
    }
    
    private void RenderEmber(SpriteBatch spriteBatch, Grafted.Graphics.Sprite pixel, 
        WeatherParticle particle, Vector2 scaledPos, float scaledSize)
    {
        // Embers: bright orange/red with flickering
        float flicker = 0.7f + MathF.Sin(particle.FlickerPhase) * 0.3f;
        var emberColor = new Color(
            (int)(255 * flicker), 
            (int)(120 + 80 * flicker), 
            (int)(30 * flicker)
        ) * particle.Opacity;
        
        var emberSize = Math.Max(1, (int)scaledSize);
        var emberRect = new Rectangle(
            (int)(scaledPos.X - emberSize / 2f),
            (int)(scaledPos.Y - emberSize / 2f),
            emberSize,
            emberSize
        );
        spriteBatch.Draw(pixel, emberRect, pixel.SourceRect, emberColor);
        
        // Add glow around embers
        var emberGlowSize = emberSize + 3;
        var emberGlowRect = new Rectangle(
            (int)(scaledPos.X - emberGlowSize / 2f),
            (int)(scaledPos.Y - emberGlowSize / 2f),
            emberGlowSize,
            emberGlowSize
        );
        spriteBatch.Draw(pixel, emberGlowRect, pixel.SourceRect, emberColor * 0.4f);
    }
    
    private void RenderSmoke(SpriteBatch spriteBatch, Grafted.Graphics.Sprite pixel, 
        WeatherParticle particle, Vector2 scaledPos, float scaledSize)
    {
        // Smoke: dark gray, semi-transparent
        var smokeAlpha = particle.Opacity * 0.5f;
        var color = new Color(60, 55, 50) * smokeAlpha;
        
        var smokeSize = Math.Max(3, (int)scaledSize);
        var smokeRect = new Rectangle(
            (int)(scaledPos.X - smokeSize / 2f),
            (int)(scaledPos.Y - smokeSize / 2f),
            smokeSize,
            smokeSize
        );
        spriteBatch.Draw(pixel, smokeRect, pixel.SourceRect, color);
        
        // Softer outer layer for smoke
        var outerSize = smokeSize + 4;
        var outerRect = new Rectangle(
            (int)(scaledPos.X - outerSize / 2f),
            (int)(scaledPos.Y - outerSize / 2f),
            outerSize,
            outerSize
        );
        spriteBatch.Draw(pixel, outerRect, pixel.SourceRect, color * 0.3f);
    }
}


