namespace Grafted.Scenes.MainGameScene.Gui.Widgets.PawnRenderer;

/// <summary>
/// The type of weather currently being rendered.
/// </summary>
public enum WeatherType
{
    Showers,
    Storm,
    Snow,
    SmokeEmbers
}

/// <summary>
/// Represents a single precipitation particle (rain drop, snowflake, smoke, or ember).
/// </summary>
public class PrecipitationParticle
{
    public Vector2 Position;
    public Vector2 Velocity;
    public float Size;
    public float Opacity;
    public float Rotation;       // For snowflakes
    public float RotationSpeed;  // For snowflakes
    public float Wobble;         // For snowflakes/smoke horizontal drift
    public float WobblePhase;    // Phase offset for wobble
    public bool IsEmber;         // True for ember, false for smoke
    public float FlickerPhase;   // For ember flickering
    public float Lifetime;       // For fading particles
    public float MaxLifetime;    // Original lifetime for fade calculation
    
    public void Update(float deltaTime, WeatherType weatherType)
    {
        Position += Velocity * deltaTime;
        
        if (weatherType == WeatherType.Snow)
        {
            // Snowflakes wobble horizontally
            WobblePhase += deltaTime * 3f;
            Position.X += MathF.Sin(WobblePhase) * Wobble * deltaTime;
            Rotation += RotationSpeed * deltaTime;
        }
        else if (weatherType == WeatherType.SmokeEmbers)
        {
            // Smoke and embers drift and wobble as they rise
            WobblePhase += deltaTime * 2f;
            Position.X += MathF.Sin(WobblePhase) * Wobble * deltaTime;
            
            if (IsEmber)
            {
                // Embers flicker
                FlickerPhase += deltaTime * 15f;
            }
            
            // Fade out over lifetime
            if (MaxLifetime > 0)
            {
                Lifetime -= deltaTime;
                Opacity = Math.Clamp(Lifetime / MaxLifetime, 0f, 1f) * 0.8f;
            }
        }
    }
}

/// <summary>
/// Represents a lightning bolt flash.
/// </summary>
public class LightningBolt
{
    public List<Vector2> Points = new();
    public float Lifetime;
    public float MaxLifetime;
    public float Brightness;
    public float Thickness;
    public List<List<Vector2>> Branches = new();
    
    public bool IsExpired => Lifetime <= 0;
    public float Alpha => Math.Clamp(Lifetime / (MaxLifetime * 0.3f), 0f, 1f);
    
    public void Update(float deltaTime)
    {
        Lifetime -= deltaTime;
        Brightness = Alpha;
    }
}

/// <summary>
/// Renders atmospheric weather effects including rain showers, storms with lightning, and snow.
/// </summary>
public class WeatherRenderer
{
    private readonly List<PrecipitationParticle> _particles = new();
    private readonly List<LightningBolt> _lightningBolts = new();
    private readonly Random _random = new();
    
    private WeatherType _currentWeather = WeatherType.Showers;
    private float _weatherTimer;
    private float _lightningCooldown;
    private float _screenFlashIntensity;
    private float _spawnAccumulator;
    private bool _hasInitialized;
    
    private const float WeatherCycleDuration = 10f;
    private const int MaxParticles = 500;
    
    // Render area dimensions (will be set dynamically)
    private int _width = 512;
    private int _height = 512;
    
    /// <summary>
    /// Current weather type being rendered.
    /// </summary>
    public WeatherType CurrentWeather => _currentWeather;
    
    /// <summary>
    /// Screen flash intensity from lightning (0-1).
    /// </summary>
    public float ScreenFlashIntensity => _screenFlashIntensity;
    
    public WeatherRenderer()
    {
        _weatherTimer = WeatherCycleDuration;
    }
    
    /// <summary>
    /// Sets the render area dimensions.
    /// </summary>
    public void SetDimensions(int width, int height)
    {
        _width = width;
        _height = height;
    }
    
    /// <summary>
    /// Updates weather simulation and cycles between weather types.
    /// </summary>
    public void Update(float deltaTime)
    {
        // Pre-populate on first update to avoid initial wave effect
        if (!_hasInitialized)
        {
            _hasInitialized = true;
            PrePopulateParticles();
        }
        
        _weatherTimer -= deltaTime;
        
        // Cycle to next weather type
        if (_weatherTimer <= 0)
        {
            _weatherTimer = WeatherCycleDuration;
            _currentWeather = _currentWeather switch
            {
                WeatherType.Showers => WeatherType.Storm,
                WeatherType.Storm => WeatherType.Snow,
                WeatherType.Snow => WeatherType.SmokeEmbers,
                WeatherType.SmokeEmbers => WeatherType.Showers,
                _ => WeatherType.Showers
            };
            
            // Clear particles and pre-populate with distributed particles
            _particles.Clear();
            _spawnAccumulator = 0f;
            PrePopulateParticles();
        }
        
        // Spawn new particles
        SpawnParticles(deltaTime);
        
        // Update existing particles
        UpdateParticles(deltaTime);
        
        // Handle lightning for storms
        if (_currentWeather == WeatherType.Storm)
        {
            UpdateLightning(deltaTime);
        }
        else
        {
            _lightningBolts.Clear();
            _screenFlashIntensity = 0;
        }
        
        // Decay screen flash
        _screenFlashIntensity = Math.Max(0, _screenFlashIntensity - deltaTime * 4f);
    }
    
    /// <summary>
    /// Pre-populates particles across the entire screen when weather changes.
    /// This prevents the "wave" effect of all particles starting at the top.
    /// </summary>
    private void PrePopulateParticles()
    {
        int targetCount = _currentWeather switch
        {
            WeatherType.Showers => 120,
            WeatherType.Storm => 180,
            WeatherType.Snow => 30,
            WeatherType.SmokeEmbers => 40,
            _ => 100
        };
        
        for (int i = 0; i < targetCount; i++)
        {
            SpawnParticle(distributeAcrossScreen: true);
        }
    }
    
    private void SpawnParticles(float deltaTime)
    {
        if (_particles.Count >= MaxParticles) return;
        
        // Spawn rate is particles per second, multiply by deltaTime to get per-frame count
        // Use accumulator to handle fractional spawns properly
        float spawnRate = _currentWeather switch
        {
            WeatherType.Showers => 100f,  // 100 particles per second
            WeatherType.Storm => 150f,    // 150 particles per second  
            WeatherType.Snow => 25f,      // 25 particles per second
            WeatherType.SmokeEmbers => 35f, // 35 particles per second
            _ => 0f
        };
        
        _spawnAccumulator += spawnRate * deltaTime;
        
        while (_spawnAccumulator >= 1f && _particles.Count < MaxParticles)
        {
            SpawnParticle(distributeAcrossScreen: false);
            _spawnAccumulator -= 1f;
        }
    }
    
    private void SpawnParticle(bool distributeAcrossScreen)
    {
        // When distributing across screen, randomize Y across full height
        // Otherwise spawn at appropriate edge based on weather type
        float startY;
        if (distributeAcrossScreen)
        {
            startY = (float)_random.NextDouble() * _height;
        }
        else if (_currentWeather == WeatherType.SmokeEmbers)
        {
            // Smoke/embers spawn from bottom and rise upward
            startY = _height - (float)_random.NextDouble() * 50f;
        }
        else
        {
            // Rain/snow spawn from top
            startY = (float)_random.NextDouble() * 50f;
        }
        
        var particle = new PrecipitationParticle
        {
            Position = new Vector2(
                (float)_random.NextDouble() * (_width + 100) - 50,
                startY
            ),
            Opacity = 0.6f + (float)_random.NextDouble() * 0.4f
        };
        
        switch (_currentWeather)
        {
            case WeatherType.Showers:
                particle.Velocity = new Vector2(
                    16f + (float)_random.NextDouble() * 24f,
                    240f + (float)_random.NextDouble() * 80f
                );
                particle.Size = 1.5f + (float)_random.NextDouble() * 1.5f;
                break;
                
            case WeatherType.Storm:
                // Faster, more angled rain in storms
                particle.Velocity = new Vector2(
                    64f + (float)_random.NextDouble() * 48f,
                    360f + (float)_random.NextDouble() * 120f
                );
                particle.Size = 2f + (float)_random.NextDouble() * 2f;
                particle.Opacity = 0.7f + (float)_random.NextDouble() * 0.3f;
                break;
                
            case WeatherType.Snow:
                particle.Velocity = new Vector2(
                    -10f + (float)_random.NextDouble() * 20f,
                    40f + (float)_random.NextDouble() * 30f
                );
                particle.Size = 2f + (float)_random.NextDouble() * 4f;
                particle.Rotation = (float)_random.NextDouble() * MathF.PI * 2;
                particle.RotationSpeed = -1f + (float)_random.NextDouble() * 2f;
                particle.Wobble = 20f + (float)_random.NextDouble() * 40f;
                particle.WobblePhase = (float)_random.NextDouble() * MathF.PI * 2;
                break;
                
            case WeatherType.SmokeEmbers:
                // 30% chance to be an ember, 70% smoke
                particle.IsEmber = _random.NextDouble() < 0.3;
                
                if (particle.IsEmber)
                {
                    // Embers: small, bright, rise faster
                    particle.Velocity = new Vector2(
                        -15f + (float)_random.NextDouble() * 30f,
                        -80f - (float)_random.NextDouble() * 60f  // Negative Y = upward
                    );
                    particle.Size = 1.5f + (float)_random.NextDouble() * 2f;
                    particle.Wobble = 15f + (float)_random.NextDouble() * 25f;
                    particle.MaxLifetime = 2f + (float)_random.NextDouble() * 2f;
                    particle.Lifetime = particle.MaxLifetime;
                    particle.FlickerPhase = (float)_random.NextDouble() * MathF.PI * 2;
                }
                else
                {
                    // Smoke: larger, slower, more drift
                    particle.Velocity = new Vector2(
                        -20f + (float)_random.NextDouble() * 40f,
                        -40f - (float)_random.NextDouble() * 30f  // Negative Y = upward
                    );
                    particle.Size = 6f + (float)_random.NextDouble() * 10f;
                    particle.Wobble = 25f + (float)_random.NextDouble() * 35f;
                    particle.MaxLifetime = 3f + (float)_random.NextDouble() * 3f;
                    particle.Lifetime = particle.MaxLifetime;
                }
                
                particle.WobblePhase = (float)_random.NextDouble() * MathF.PI * 2;
                particle.Opacity = 0.6f + (float)_random.NextDouble() * 0.3f;
                break;
        }
        
        _particles.Add(particle);
    }
    
    private void UpdateParticles(float deltaTime)
    {
        for (int i = _particles.Count - 1; i >= 0; i--)
        {
            var particle = _particles[i];
            particle.Update(deltaTime, _currentWeather);
            
            // Remove particles that are off-screen or expired
            bool offScreen = particle.Position.Y > _height + 20 || 
                             particle.Position.Y < -50 ||  // Also check top for rising particles
                             particle.Position.X > _width + 50 || 
                             particle.Position.X < -50;
            bool expired = particle.MaxLifetime > 0 && particle.Lifetime <= 0;
            
            if (offScreen || expired)
            {
                _particles.RemoveAt(i);
            }
        }
    }
    
    private void UpdateLightning(float deltaTime)
    {
        _lightningCooldown -= deltaTime;
        
        // Random chance to spawn lightning
        if (_lightningCooldown <= 0 && _random.NextDouble() < 0.03)
        {
            SpawnLightning();
            _lightningCooldown = 0.5f + (float)_random.NextDouble() * 2f;
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
            Lifetime = 0.15f + (float)_random.NextDouble() * 0.1f,
            MaxLifetime = 0.25f,
            Brightness = 1f,
            Thickness = 2f + (float)_random.NextDouble() * 2f
        };
        
        // Generate main bolt path
        float startX = _width * 0.2f + (float)_random.NextDouble() * _width * 0.6f;
        var currentPos = new Vector2(startX, 0);
        bolt.Points.Add(currentPos);
        
        int segments = 8 + _random.Next(6);
        float segmentLength = _height / (float)segments;
        
        for (int i = 0; i < segments; i++)
        {
            float offsetX = -30f + (float)_random.NextDouble() * 60f;
            currentPos = new Vector2(
                Math.Clamp(currentPos.X + offsetX, 20, _width - 20),
                currentPos.Y + segmentLength + (float)_random.NextDouble() * 20f
            );
            bolt.Points.Add(currentPos);
            
            // Chance to add a branch
            if (_random.NextDouble() < 0.3 && i > 1 && i < segments - 2)
            {
                var branch = GenerateBranch(currentPos, i, segments);
                bolt.Branches.Add(branch);
            }
        }
        
        _lightningBolts.Add(bolt);
        _screenFlashIntensity = 0.4f + (float)_random.NextDouble() * 0.3f;
    }
    
    private List<Vector2> GenerateBranch(Vector2 startPoint, int depth, int maxDepth)
    {
        var branch = new List<Vector2> { startPoint };
        
        float angle = -MathF.PI / 4 + (float)_random.NextDouble() * MathF.PI / 2;
        float length = 30f + (float)_random.NextDouble() * 50f;
        int branchSegments = 3 + _random.Next(3);
        
        var currentPos = startPoint;
        for (int i = 0; i < branchSegments; i++)
        {
            angle += -0.3f + (float)_random.NextDouble() * 0.6f;
            float segLen = length / branchSegments;
            currentPos = new Vector2(
                currentPos.X + MathF.Cos(angle) * segLen,
                currentPos.Y + MathF.Abs(MathF.Sin(angle)) * segLen + 10f
            );
            branch.Add(currentPos);
        }
        
        return branch;
    }
    
    /// <summary>
    /// Renders the weather effects.
    /// </summary>
    public void Render(SpriteBatch spriteBatch, float scale = 1f)
    {
        var pixel = Core.Graphics.PixelTexture;
        
        // Render screen flash for lightning
        if (_screenFlashIntensity > 0.01f)
        {
            var flashColor = new Color(200, 200, 255) * (_screenFlashIntensity * 0.15f);
            spriteBatch.Draw(
                pixel, 
                new Rectangle(0, 0, (int)(_width * scale), (int)(_height * scale)), 
                pixel.SourceRect, 
                flashColor
            );
        }
        
        // Render lightning bolts
        foreach (var bolt in _lightningBolts)
        {
            RenderLightningBolt(spriteBatch, bolt, pixel, scale);
        }
        
        // Render precipitation
        foreach (var particle in _particles)
        {
            RenderParticle(spriteBatch, particle, pixel, scale);
        }
    }
    
    private void RenderParticle(SpriteBatch spriteBatch, PrecipitationParticle particle, Grafted.Graphics.Sprite pixel, float scale)
    {
        var scaledPos = particle.Position * scale;
        var scaledSize = particle.Size * scale;
        
        Color color;
        
        switch (_currentWeather)
        {
            case WeatherType.Showers:
                // Light blue-gray rain
                color = new Color(150, 170, 200) * particle.Opacity;
                // Rain drops are elongated vertically with slight angle
                var rainRect = new Rectangle(
                    (int)scaledPos.X,
                    (int)scaledPos.Y,
                    Math.Max(1, (int)scaledSize),
                    Math.Max(1, (int)(scaledSize * 4f))
                );
                spriteBatch.Draw(pixel, rainRect, pixel.SourceRect, color);
                break;
                
            case WeatherType.Storm:
                // Brighter, more intense rain with hint of electric blue
                color = new Color(180, 190, 220) * particle.Opacity;
                // Steeper angle for storm rain
                var stormRect = new Rectangle(
                    (int)scaledPos.X,
                    (int)scaledPos.Y,
                    Math.Max(1, (int)(scaledSize * 0.8f)),
                    Math.Max(1, (int)(scaledSize * 5f))
                );
                spriteBatch.Draw(pixel, stormRect, pixel.SourceRect, color);
                break;
                
            case WeatherType.Snow:
                // White snowflakes with slight blue tint
                color = new Color(240, 245, 255) * particle.Opacity;
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
                break;
                
            case WeatherType.SmokeEmbers:
                if (particle.IsEmber)
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
                else
                {
                    // Smoke: dark gray, semi-transparent
                    var smokeAlpha = particle.Opacity * 0.5f;
                    color = new Color(60, 55, 50) * smokeAlpha;
                    
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
                break;
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
    
    private void DrawLightningPath(SpriteBatch spriteBatch, Grafted.Graphics.Sprite pixel, List<Vector2> points, float thickness, Color coreColor, Color glowColor, float scale)
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
    
    /// <summary>
    /// Returns true if there are active weather effects to render.
    /// </summary>
    public bool HasActiveEffects => _particles.Count > 0 || _lightningBolts.Count > 0 || _screenFlashIntensity > 0.01f;
    
    /// <summary>
    /// Gets the time remaining until the next weather type.
    /// </summary>
    public float TimeUntilNextWeather => _weatherTimer;
}

