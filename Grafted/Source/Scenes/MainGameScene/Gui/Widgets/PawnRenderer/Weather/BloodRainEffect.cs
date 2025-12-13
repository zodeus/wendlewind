namespace Grafted.Scenes.MainGameScene.Gui.Widgets.PawnRenderer.Weather;

/// <summary>
/// Ominous blood rain weather effect with dark crimson drops.
/// </summary>
public class BloodRainEffect : BaseWeatherEffect
{
    private float _pulsePhase;
    private float _ambientDarkness;
    
    public override WeatherType WeatherType => WeatherType.BloodRain;
    public override int PrePopulateCount => 140;
    public override float SpawnRate => 120f;
    
    /// <summary>
    /// Ambient darkness overlay intensity (0-1).
    /// </summary>
    public float AmbientDarkness => _ambientDarkness;
    
    public override void SpawnParticle(bool distributeAcrossScreen)
    {
        if (!CanSpawnParticle()) return;
        
        // Blood drops vary in size - some are small droplets, some are larger globs
        bool isLargeDrop = Random.NextDouble() < 0.15;
        
        var particle = new WeatherParticle
        {
            Position = new Vector2(GetSpawnX(), GetSpawnY(distributeAcrossScreen)),
            Opacity = 0.7f + (float)Random.NextDouble() * 0.3f
        };
        
        if (isLargeDrop)
        {
            // Large blood globs fall slower and are more visible
            particle.Velocity = new Vector2(
                8f + (float)Random.NextDouble() * 16f,
                180f + (float)Random.NextDouble() * 60f
            );
            particle.Size = 3f + (float)Random.NextDouble() * 3f;
            particle.IsEmber = true; // Reuse flag to mark as large drop
        }
        else
        {
            // Normal blood droplets
            particle.Velocity = new Vector2(
                12f + (float)Random.NextDouble() * 20f,
                260f + (float)Random.NextDouble() * 100f
            );
            particle.Size = 1.5f + (float)Random.NextDouble() * 2f;
        }
        
        // Slight wobble for organic feel
        particle.WobblePhase = (float)Random.NextDouble() * MathF.PI * 2;
        particle.Wobble = 3f + (float)Random.NextDouble() * 5f;
        
        Particles.Add(particle);
    }
    
    protected override void UpdateParticle(WeatherParticle particle, float deltaTime)
    {
        base.UpdateParticle(particle, deltaTime);
        
        // Slight wobble for organic movement
        particle.WobblePhase += deltaTime * 2f;
        particle.Position.X += MathF.Sin(particle.WobblePhase) * particle.Wobble * deltaTime;
    }
    
    public override void Update(float deltaTime)
    {
        base.Update(deltaTime);
        
        // Pulsing ambient darkness for ominous effect
        _pulsePhase += deltaTime * 0.8f;
        _ambientDarkness = 0.08f + MathF.Sin(_pulsePhase) * 0.03f;
    }
    
    public override void Render(SpriteBatch spriteBatch, float scale)
    {
        var pixel = Core.Graphics.PixelTexture;
        
        // Render dark red ambient overlay
        if (_ambientDarkness > 0.01f)
        {
            var overlayColor = new Color(60, 0, 0) * _ambientDarkness;
            spriteBatch.Draw(
                pixel,
                new Rectangle(0, 0, (int)(Width * scale), (int)(Height * scale)),
                pixel.SourceRect,
                overlayColor
            );
        }
        
        // Render blood drops
        foreach (var particle in Particles)
        {
            RenderBloodDrop(spriteBatch, pixel, particle, scale);
        }
    }
    
    private void RenderBloodDrop(SpriteBatch spriteBatch, Grafted.Graphics.Sprite pixel, 
        WeatherParticle particle, float scale)
    {
        var scaledPos = particle.Position * scale;
        var scaledSize = particle.Size * scale;
        
        if (particle.IsEmber) // Large blood glob
        {
            // Dark crimson core
            var coreColor = new Color(100, 10, 15) * particle.Opacity;
            var coreSize = Math.Max(2, (int)scaledSize);
            var coreRect = new Rectangle(
                (int)(scaledPos.X - coreSize / 2f),
                (int)scaledPos.Y,
                coreSize,
                Math.Max(2, (int)(scaledSize * 2.5f))
            );
            spriteBatch.Draw(pixel, coreRect, pixel.SourceRect, coreColor);
            
            // Brighter red highlight
            var highlightColor = new Color(160, 20, 30) * particle.Opacity * 0.7f;
            var highlightRect = new Rectangle(
                (int)(scaledPos.X - coreSize / 4f),
                (int)(scaledPos.Y + scaledSize * 0.3f),
                Math.Max(1, coreSize / 2),
                Math.Max(1, (int)(scaledSize * 1.5f))
            );
            spriteBatch.Draw(pixel, highlightRect, pixel.SourceRect, highlightColor);
        }
        else
        {
            // Normal blood drop - dark red elongated streak
            var bloodColor = new Color(120, 15, 20) * particle.Opacity;
            var dropRect = new Rectangle(
                (int)scaledPos.X,
                (int)scaledPos.Y,
                Math.Max(1, (int)scaledSize),
                Math.Max(1, (int)(scaledSize * 4.5f))
            );
            spriteBatch.Draw(pixel, dropRect, pixel.SourceRect, bloodColor);
            
            // Subtle darker trail
            var trailColor = new Color(80, 5, 10) * particle.Opacity * 0.5f;
            var trailRect = new Rectangle(
                (int)scaledPos.X,
                (int)(scaledPos.Y - scaledSize * 2f),
                Math.Max(1, (int)(scaledSize * 0.7f)),
                Math.Max(1, (int)(scaledSize * 2f))
            );
            spriteBatch.Draw(pixel, trailRect, pixel.SourceRect, trailColor);
        }
    }
    
    public override void Clear()
    {
        base.Clear();
        _pulsePhase = 0;
        _ambientDarkness = 0;
    }
}



