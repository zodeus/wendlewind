namespace Grafted.Scenes.MainGameScene.Gui.Widgets.PawnRenderer.Weather;

/// <summary>
/// Heavy storm weather effect with intense rain and lightning.
/// </summary>
public class StormEffect : BaseWeatherEffect
{
    private readonly List<LightningBolt> _lightningBolts = new();
    private float _lightningCooldown;
    private float _screenFlashIntensity;
    
    public override WeatherType WeatherType => WeatherType.Storm;
    public override int PrePopulateCount => 180;
    public override float SpawnRate => 150f;
    
    public override float ScreenFlashIntensity => _screenFlashIntensity;
    
    public override bool HasActiveEffects => 
        base.HasActiveEffects || _lightningBolts.Count > 0 || _screenFlashIntensity > 0.01f;
    
    public override void SpawnParticle(bool distributeAcrossScreen)
    {
        if (!CanSpawnParticle()) return;
        
        var particle = new WeatherParticle
        {
            Position = new Vector2(GetSpawnX(), GetSpawnY(distributeAcrossScreen)),
            Velocity = new Vector2(
                64f + (float)Random.NextDouble() * 48f,
                360f + (float)Random.NextDouble() * 120f
            ),
            Size = 2f + (float)Random.NextDouble() * 2f,
            Opacity = 0.7f + (float)Random.NextDouble() * 0.3f
        };
        
        Particles.Add(particle);
    }
    
    public override void Update(float deltaTime)
    {
        base.Update(deltaTime);
        UpdateLightning(deltaTime);
        
        // Decay screen flash
        _screenFlashIntensity = Math.Max(0, _screenFlashIntensity - deltaTime * 4f);
    }
    
    private void UpdateLightning(float deltaTime)
    {
        _lightningCooldown -= deltaTime;
        
        // Random chance to spawn lightning
        if (_lightningCooldown <= 0 && Random.NextDouble() < 0.03)
        {
            SpawnLightning();
            _lightningCooldown = 0.5f + (float)Random.NextDouble() * 2f;
        }
        
        // Update existing bolts
        for (int i = _lightningBolts.Count - 1; i >= 0; i--)
        {
            _lightningBolts[i].Update(deltaTime);
            if (_lightningBolts[i].IsExpired)
            {
                _lightningBolts.RemoveAt(i);
            }
        }
    }
    
    private void SpawnLightning()
    {
        var bolt = new LightningBolt
        {
            Lifetime = 0.15f + (float)Random.NextDouble() * 0.1f,
            MaxLifetime = 0.25f,
            Brightness = 1f,
            Thickness = 2f + (float)Random.NextDouble() * 2f
        };
        
        // Generate main bolt path
        float startX = Width * 0.2f + (float)Random.NextDouble() * Width * 0.6f;
        var currentPos = new Vector2(startX, 0);
        bolt.Points.Add(currentPos);
        
        int segments = 8 + Random.Next(6);
        float segmentLength = Height / (float)segments;
        
        for (int i = 0; i < segments; i++)
        {
            float offsetX = -30f + (float)Random.NextDouble() * 60f;
            currentPos = new Vector2(
                Math.Clamp(currentPos.X + offsetX, 20, Width - 20),
                currentPos.Y + segmentLength + (float)Random.NextDouble() * 20f
            );
            bolt.Points.Add(currentPos);
            
            // Chance to add a branch
            if (Random.NextDouble() < 0.3 && i > 1 && i < segments - 2)
            {
                var branch = GenerateBranch(currentPos);
                bolt.Branches.Add(branch);
            }
        }
        
        _lightningBolts.Add(bolt);
        _screenFlashIntensity = 0.4f + (float)Random.NextDouble() * 0.3f;
    }
    
    private List<Vector2> GenerateBranch(Vector2 startPoint)
    {
        var branch = new List<Vector2> { startPoint };
        
        float angle = -MathF.PI / 4 + (float)Random.NextDouble() * MathF.PI / 2;
        float length = 30f + (float)Random.NextDouble() * 50f;
        int branchSegments = 3 + Random.Next(3);
        
        var currentPos = startPoint;
        for (int i = 0; i < branchSegments; i++)
        {
            angle += -0.3f + (float)Random.NextDouble() * 0.6f;
            float segLen = length / branchSegments;
            currentPos = new Vector2(
                currentPos.X + MathF.Cos(angle) * segLen,
                currentPos.Y + MathF.Abs(MathF.Sin(angle)) * segLen + 10f
            );
            branch.Add(currentPos);
        }
        
        return branch;
    }
    
    public override void Render(SpriteBatch spriteBatch, float scale)
    {
        var pixel = Core.Graphics.PixelTexture;
        
        // Render screen flash for lightning
        if (_screenFlashIntensity > 0.01f)
        {
            var flashColor = new Color(200, 200, 255) * (_screenFlashIntensity * 0.15f);
            spriteBatch.Draw(
                pixel, 
                new Rectangle(0, 0, (int)(Width * scale), (int)(Height * scale)), 
                pixel.SourceRect, 
                flashColor
            );
        }
        
        // Render lightning bolts
        foreach (var bolt in _lightningBolts)
        {
            RenderLightningBolt(spriteBatch, bolt, pixel, scale);
        }
        
        // Render rain
        foreach (var particle in Particles)
        {
            var scaledPos = particle.Position * scale;
            var scaledSize = particle.Size * scale;
            
            // Brighter, more intense rain with hint of electric blue
            var color = new Color(180, 190, 220) * particle.Opacity;
            
            // Steeper angle for storm rain
            var stormRect = new Rectangle(
                (int)scaledPos.X,
                (int)scaledPos.Y,
                Math.Max(1, (int)(scaledSize * 0.8f)),
                Math.Max(1, (int)(scaledSize * 5f))
            );
            
            spriteBatch.Draw(pixel, stormRect, pixel.SourceRect, color);
        }
    }
    
    private void RenderLightningBolt(SpriteBatch spriteBatch, LightningBolt bolt, Grafted.Graphics.Sprite pixel, float scale)
    {
        // Core bolt color - bright electric blue/white
        var coreColor = new Color(220, 230, 255) * bolt.Alpha;
        var glowColor = new Color(100, 150, 255) * bolt.Alpha * 0.5f;
        
        // Draw main bolt
        DrawLightningPath(spriteBatch, pixel, bolt.Points, bolt.Thickness * scale, coreColor, glowColor, scale);
        
        // Draw branches (thinner)
        foreach (var branch in bolt.Branches)
        {
            DrawLightningPath(spriteBatch, pixel, branch, bolt.Thickness * 0.5f * scale, coreColor * 0.8f, glowColor * 0.6f, scale);
        }
    }
    
    private void DrawLightningPath(SpriteBatch spriteBatch, Grafted.Graphics.Sprite pixel, List<Vector2> points, 
        float thickness, Color coreColor, Color glowColor, float scale)
    {
        for (int i = 0; i < points.Count - 1; i++)
        {
            var start = points[i] * scale;
            var end = points[i + 1] * scale;
            
            var direction = end - start;
            var length = direction.Length();
            var angle = MathF.Atan2(direction.Y, direction.X);
            
            // Draw glow first
            var glowRect = new Rectangle(
                (int)start.X,
                (int)(start.Y - thickness),
                (int)length,
                (int)(thickness * 3)
            );
            spriteBatch.Draw(
                pixel, 
                glowRect, 
                pixel.SourceRect, 
                glowColor, 
                angle, 
                Vector2.Zero, 
                SpriteEffects.None, 
                0
            );
            
            // Draw core
            var coreRect = new Rectangle(
                (int)start.X,
                (int)(start.Y - thickness * 0.5f),
                (int)length,
                (int)Math.Max(1, thickness)
            );
            spriteBatch.Draw(
                pixel, 
                coreRect, 
                pixel.SourceRect, 
                coreColor, 
                angle, 
                Vector2.Zero, 
                SpriteEffects.None, 
                0
            );
        }
    }
    
    public override void Clear()
    {
        base.Clear();
        _lightningBolts.Clear();
        _screenFlashIntensity = 0;
    }
}

