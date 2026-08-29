namespace Wendlewind.Scenes.MainGameScene.Gui.Widgets.PawnRenderer.Weather;

/// <summary>
/// Hallowed rain with golden droplets and rising motes of divine light.
/// Creates a blessed, sacred atmosphere—the antithesis of blood rain.
/// </summary>
public class HallowedRainEffect : BaseWeatherEffect
{
    private float _pulsePhase;
    private float _ambientWarmth;
    private float _moteSpawnAccumulator;
    
    // Rising sacred motes (sparkles that float upward)
    private readonly List<WeatherParticle> _sacredMotes = new();
    
    public override WeatherType WeatherType => WeatherType.HallowedRain;
    public override int PrePopulateCount => 60;
    public override float SpawnRate => 45f;
    
    /// <summary>
    /// Ambient golden warmth overlay intensity (0-1).
    /// </summary>
    public float AmbientWarmth => _ambientWarmth;
    
    public override bool HasActiveEffects => base.HasActiveEffects || _sacredMotes.Count > 0;
    
    public override void SpawnParticle(bool distributeAcrossScreen)
    {
        if (!CanSpawnParticle()) return;
        
        // Some drops shimmer more brightly
        bool isBlessed = Random.NextDouble() < 0.2;
        
        var particle = new WeatherParticle
        {
            Position = new Vector2(GetSpawnX(), GetSpawnY(distributeAcrossScreen)),
            Opacity = 0.5f + (float)Random.NextDouble() * 0.4f,
            IsEmber = isBlessed,
            FlickerPhase = (float)Random.NextDouble() * MathF.PI * 2
        };
        
        if (isBlessed)
        {
            // Blessed droplets: larger, slower, more luminous
            particle.Velocity = new Vector2(
                5f + (float)Random.NextDouble() * 12f,
                120f + (float)Random.NextDouble() * 50f
            );
            particle.Size = 2.5f + (float)Random.NextDouble() * 2f;
        }
        else
        {
            // Normal golden rain
            particle.Velocity = new Vector2(
                8f + (float)Random.NextDouble() * 16f,
                180f + (float)Random.NextDouble() * 80f
            );
            particle.Size = 1f + (float)Random.NextDouble() * 1.5f;
        }
        
        Particles.Add(particle);
    }
    
    private void SpawnSacredMote()
    {
        if (_sacredMotes.Count >= 30) return;
        
        var mote = new WeatherParticle
        {
            Position = new Vector2(
                (float)Random.NextDouble() * Width,
                Height * 0.5f + (float)Random.NextDouble() * Height * 0.5f
            ),
            // Rise upward with gentle drift
            Velocity = new Vector2(
                -15f + (float)Random.NextDouble() * 30f,
                -40f - (float)Random.NextDouble() * 30f
            ),
            Size = 1.5f + (float)Random.NextDouble() * 2.5f,
            Opacity = 0f,
            WobblePhase = (float)Random.NextDouble() * MathF.PI * 2,
            Wobble = 20f + (float)Random.NextDouble() * 30f,
            MaxLifetime = 2f + (float)Random.NextDouble() * 2f,
            Lifetime = 0f
        };
        mote.Lifetime = mote.MaxLifetime;
        
        _sacredMotes.Add(mote);
    }
    
    public override void Update(float deltaTime)
    {
        base.Update(deltaTime);
        
        // Gentle pulsing warmth
        _pulsePhase += deltaTime * 1.2f;
        _ambientWarmth = 0.06f + MathF.Sin(_pulsePhase) * 0.025f;
        
        // Spawn rising sacred motes
        _moteSpawnAccumulator += deltaTime * 6f;
        while (_moteSpawnAccumulator >= 1f)
        {
            SpawnSacredMote();
            _moteSpawnAccumulator -= 1f;
        }
        
        // Update sacred motes
        for (int i = _sacredMotes.Count - 1; i >= 0; i--)
        {
            var mote = _sacredMotes[i];
            
            // Wobble as they rise
            mote.WobblePhase += deltaTime * 2.5f;
            mote.Position.X += MathF.Sin(mote.WobblePhase) * mote.Wobble * deltaTime;
            mote.Position += mote.Velocity * deltaTime;
            
            // Lifetime fade
            mote.Lifetime -= deltaTime;
            float lifeRatio = mote.Lifetime / mote.MaxLifetime;
            mote.Opacity = lifeRatio < 0.3f 
                ? lifeRatio / 0.3f 
                : lifeRatio > 0.7f 
                    ? (1f - lifeRatio) / 0.3f 
                    : 1f;
            mote.Opacity *= 0.8f;
            
            if (mote.Lifetime <= 0 || mote.Position.Y < -50)
            {
                _sacredMotes.RemoveAt(i);
            }
        }
    }
    
    protected override void UpdateParticle(WeatherParticle particle, float deltaTime)
    {
        base.UpdateParticle(particle, deltaTime);
        
        // Blessed drops shimmer
        if (particle.IsEmber)
        {
            particle.FlickerPhase += deltaTime * 8f;
        }
    }
    
    public override void Render(SpriteBatch spriteBatch, float scale)
    {
        var pixel = Core.Graphics.PixelTexture;
        
        // Warm golden ambient overlay
        if (_ambientWarmth > 0.01f)
        {
            var overlayColor = new Color(255, 220, 120) * _ambientWarmth;
            spriteBatch.Draw(
                pixel,
                new Rectangle(0, 0, (int)(Width * scale), (int)(Height * scale)),
                pixel.SourceRect,
                overlayColor
            );
        }
        
        // Render sacred motes (rising sparkles)
        foreach (var mote in _sacredMotes)
        {
            RenderSacredMote(spriteBatch, pixel, mote, scale);
        }
        
        // Render rain drops
        foreach (var particle in Particles)
        {
            RenderRainDrop(spriteBatch, pixel, particle, scale);
        }
    }
    
    private void RenderSacredMote(SpriteBatch spriteBatch, Wendlewind.Graphics.Sprite pixel,
        WeatherParticle mote, float scale)
    {
        if (mote.Opacity < 0.05f) return;
        
        var scaledPos = mote.Position * scale;
        var scaledSize = mote.Size * scale;
        
        // Soft golden glow
        var glowColor = new Color(255, 240, 180) * mote.Opacity;
        
        // Outer glow
        var outerSize = (int)(scaledSize * 3f);
        var outerRect = new Rectangle(
            (int)(scaledPos.X - outerSize / 2f),
            (int)(scaledPos.Y - outerSize / 2f),
            outerSize,
            outerSize
        );
        spriteBatch.Draw(pixel, outerRect, pixel.SourceRect, glowColor * 0.25f);
        
        // Bright core
        var coreSize = Math.Max(2, (int)scaledSize);
        var coreRect = new Rectangle(
            (int)(scaledPos.X - coreSize / 2f),
            (int)(scaledPos.Y - coreSize / 2f),
            coreSize,
            coreSize
        );
        var coreColor = new Color(255, 255, 230) * mote.Opacity;
        spriteBatch.Draw(pixel, coreRect, pixel.SourceRect, coreColor);
    }
    
    private void RenderRainDrop(SpriteBatch spriteBatch, Wendlewind.Graphics.Sprite pixel,
        WeatherParticle particle, float scale)
    {
        var scaledPos = particle.Position * scale;
        var scaledSize = particle.Size * scale;
        
        if (particle.IsEmber) // Blessed drop
        {
            // Shimmering golden droplet
            float shimmer = 0.7f + MathF.Sin(particle.FlickerPhase) * 0.3f;
            var dropColor = new Color(
                (int)(255 * shimmer),
                (int)(220 * shimmer),
                (int)(100 * shimmer)
            ) * particle.Opacity;
            
            // Main drop
            int dropWidth = Math.Max(2, (int)(scaledSize * 0.8f));
            int dropHeight = Math.Max(3, (int)(scaledSize * 3f));
            var dropRect = new Rectangle(
                (int)(scaledPos.X - dropWidth / 2f),
                (int)scaledPos.Y,
                dropWidth,
                dropHeight
            );
            spriteBatch.Draw(pixel, dropRect, pixel.SourceRect, dropColor);
            
            // Luminous trail
            var trailColor = new Color(255, 245, 200) * particle.Opacity * 0.4f;
            var trailRect = new Rectangle(
                (int)(scaledPos.X - dropWidth / 4f),
                (int)(scaledPos.Y - scaledSize * 2f),
                Math.Max(1, dropWidth / 2),
                Math.Max(2, (int)(scaledSize * 2.5f))
            );
            spriteBatch.Draw(pixel, trailRect, pixel.SourceRect, trailColor);
        }
        else
        {
            // Normal golden rain drop
            var dropColor = new Color(230, 200, 100) * particle.Opacity;
            var dropRect = new Rectangle(
                (int)scaledPos.X,
                (int)scaledPos.Y,
                Math.Max(1, (int)(scaledSize * 0.7f)),
                Math.Max(2, (int)(scaledSize * 4f))
            );
            spriteBatch.Draw(pixel, dropRect, pixel.SourceRect, dropColor);
        }
    }
    
    public override void Clear()
    {
        base.Clear();
        _sacredMotes.Clear();
        _pulsePhase = 0;
        _ambientWarmth = 0;
        _moteSpawnAccumulator = 0;
    }
}

