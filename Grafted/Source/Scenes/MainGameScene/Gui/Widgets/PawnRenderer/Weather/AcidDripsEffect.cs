namespace Grafted.Scenes.MainGameScene.Gui.Widgets.PawnRenderer.Weather;

/// <summary>
/// Toxic acid drips weather effect for cave environments.
/// Slow-falling corrosive droplets with an eerie green glow.
/// </summary>
public class AcidDripsEffect : BaseWeatherEffect
{
    private float _glowPhase;
    private float _ambientGlow;
    
    public override WeatherType WeatherType => WeatherType.AcidDrips;
    public override int PrePopulateCount => 60;
    public override float SpawnRate => 25f;
    
    /// <summary>
    /// Ambient toxic glow intensity (0-1).
    /// </summary>
    public float AmbientGlow => _ambientGlow;
    
    public override void SpawnParticle(bool distributeAcrossScreen)
    {
        if (!CanSpawnParticle()) return;
        
        // Acid drops vary - some are large globs, most are smaller drips
        bool isLargeDrop = Random.NextDouble() < 0.2;
        
        var particle = new WeatherParticle
        {
            Position = new Vector2(GetSpawnX(), GetSpawnY(distributeAcrossScreen)),
            Opacity = 0.6f + (float)Random.NextDouble() * 0.4f
        };
        
        if (isLargeDrop)
        {
            // Large acid globs fall slower initially but accelerate
            particle.Velocity = new Vector2(
                -2f + (float)Random.NextDouble() * 4f,
                40f + (float)Random.NextDouble() * 30f
            );
            particle.Size = 4f + (float)Random.NextDouble() * 3f;
            particle.IsEmber = true; // Mark as large drop
        }
        else
        {
            // Smaller acid drips
            particle.Velocity = new Vector2(
                -1f + (float)Random.NextDouble() * 2f,
                60f + (float)Random.NextDouble() * 40f
            );
            particle.Size = 1.5f + (float)Random.NextDouble() * 2f;
        }
        
        // Slight wobble for organic dripping feel
        particle.WobblePhase = (float)Random.NextDouble() * MathF.PI * 2;
        particle.Wobble = 2f + (float)Random.NextDouble() * 3f;
        
        Particles.Add(particle);
    }
    
    protected override void UpdateParticle(WeatherParticle particle, float deltaTime)
    {
        // Apply gravity acceleration for realistic drip physics
        particle.Velocity.Y += 80f * deltaTime;
        
        base.UpdateParticle(particle, deltaTime);
        
        // Slight wobble for organic dripping movement
        particle.WobblePhase += deltaTime * 1.5f;
        particle.Position.X += MathF.Sin(particle.WobblePhase) * particle.Wobble * deltaTime;
    }
    
    public override void Update(float deltaTime)
    {
        base.Update(deltaTime);
        
        // Pulsing toxic glow for eerie cave atmosphere
        _glowPhase += deltaTime * 1.2f;
        _ambientGlow = 0.04f + MathF.Sin(_glowPhase) * 0.02f;
    }
    
    public override void Render(SpriteBatch spriteBatch, float scale)
    {
        var pixel = Core.Graphics.PixelTexture;
        
        // Render subtle toxic green ambient overlay
        if (_ambientGlow > 0.01f)
        {
            var overlayColor = new Color(30, 60, 10) * _ambientGlow;
            spriteBatch.Draw(
                pixel,
                new Rectangle(0, 0, (int)(Width * scale), (int)(Height * scale)),
                pixel.SourceRect,
                overlayColor
            );
        }
        
        // Render acid drops
        foreach (var particle in Particles)
        {
            RenderAcidDrop(spriteBatch, pixel, particle, scale);
        }
    }
    
    private void RenderAcidDrop(SpriteBatch spriteBatch, Grafted.Graphics.Sprite pixel,
        WeatherParticle particle, float scale)
    {
        var scaledPos = particle.Position * scale;
        var scaledSize = particle.Size * scale;
        
        if (particle.IsEmber) // Large acid glob
        {
            // Dark green core
            var coreColor = new Color(40, 90, 20) * particle.Opacity;
            var coreSize = Math.Max(2, (int)scaledSize);
            var coreRect = new Rectangle(
                (int)(scaledPos.X - coreSize / 2f),
                (int)scaledPos.Y,
                coreSize,
                Math.Max(2, (int)(scaledSize * 2f))
            );
            spriteBatch.Draw(pixel, coreRect, pixel.SourceRect, coreColor);
            
            // Bright yellow-green highlight for toxic glow
            var highlightColor = new Color(150, 200, 50) * particle.Opacity * 0.8f;
            var highlightRect = new Rectangle(
                (int)(scaledPos.X - coreSize / 4f),
                (int)(scaledPos.Y + scaledSize * 0.2f),
                Math.Max(1, coreSize / 2),
                Math.Max(1, (int)(scaledSize * 1.2f))
            );
            spriteBatch.Draw(pixel, highlightRect, pixel.SourceRect, highlightColor);
        }
        else
        {
            // Normal acid drip - elongated green streak
            var acidColor = new Color(80, 140, 40) * particle.Opacity;
            var dropRect = new Rectangle(
                (int)scaledPos.X,
                (int)scaledPos.Y,
                Math.Max(1, (int)scaledSize),
                Math.Max(1, (int)(scaledSize * 3.5f))
            );
            spriteBatch.Draw(pixel, dropRect, pixel.SourceRect, acidColor);
            
            // Brighter tip for glow effect
            var tipColor = new Color(120, 180, 60) * particle.Opacity * 0.7f;
            var tipRect = new Rectangle(
                (int)scaledPos.X,
                (int)(scaledPos.Y + scaledSize * 2.5f),
                Math.Max(1, (int)(scaledSize * 0.8f)),
                Math.Max(1, (int)(scaledSize * 1f))
            );
            spriteBatch.Draw(pixel, tipRect, pixel.SourceRect, tipColor);
        }
    }
    
    public override void Clear()
    {
        base.Clear();
        _glowPhase = 0;
        _ambientGlow = 0;
    }
}





