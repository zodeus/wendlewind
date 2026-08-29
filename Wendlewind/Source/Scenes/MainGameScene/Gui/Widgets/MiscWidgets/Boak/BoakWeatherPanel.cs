using Wendlewind.Scenes.MainGameScene.Gui.Widgets.PawnRenderer.Weather;

namespace Wendlewind.Scenes.MainGameScene.Gui.Widgets.MiscWidgets.Boak;

/// <summary>
/// Panel displaying all weather types with live previews.
/// </summary>
internal sealed class BoakWeatherPanel : VerticalStackPanel, IDisposable
{
    private readonly List<WeatherPreviewWidget> _previews = [];
    private bool _disposed;

    public BoakWeatherPanel()
    {
        Spacing = 20;
        var maxColumns = 5;
        var weatherRows = new HorizontalStackPanel
        {
            Spacing = 20
        };
        Widgets.Add(weatherRows);
        // Create a preview for each weather type
        foreach (WeatherType weatherType in Enum.GetValues<WeatherType>())
        {
            weatherRows.Widgets.Add(CreateWeatherRow(weatherType));
            if (weatherRows.Widgets.Count >= maxColumns)
            {
                weatherRows = new HorizontalStackPanel
                {
                    Spacing = 20
                };
                Widgets.Add(weatherRows);
            }
        }
    }

    private VerticalStackPanel CreateWeatherRow(WeatherType weatherType)
    {
        var preview = new WeatherPreviewWidget(weatherType, 300, 300);
        _previews.Add(preview);

        var label = new Label(BaseContent.Styles.Label.Large)
        {
            Text = FormatWeatherName(weatherType),
            HorizontalAlignment = HorizontalAlignment.Center,
        };

        return new VerticalStackPanel
        {
            Spacing = 5,
            Widgets =
            {
                new Panel
                {
                    Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.DeepGold],
                    Padding = new Thickness(5),
                    Widgets = { preview }
                },
                label,
            }
        };
    }

    private static string FormatWeatherName(WeatherType type)
    {
        // Convert PascalCase to spaced words
        var name = type.ToString();
        return string.Concat(name.Select((c, i) => i > 0 && char.IsUpper(c) ? " " + c : c.ToString()));
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        foreach (var preview in _previews)
        {
            preview.Dispose();
        }
        _previews.Clear();
    }
}

/// <summary>
/// A widget that renders a live preview of a weather effect.
/// Pre-renders to a texture before Myra's render pass to avoid render target issues.
/// </summary>
internal sealed class WeatherPreviewWidget : Widget, IDisposable
{
    private static readonly List<WeatherPreviewWidget> AllPreviews = [];
    private static long _lastPreRenderFrame = -1;
    
    private readonly IWeatherEffect _effect;
    private readonly int _width;
    private readonly int _height;
    
    private RenderTarget2D? _renderTarget;
    private SpriteBatch? _spriteBatch;
    private float _spawnAccumulator;
    private bool _disposed;

    /// <summary>
    /// Pre-renders all weather previews. Call this BEFORE Myra's Desktop.Render().
    /// </summary>
    public static void PreRenderAll(float deltaTime)
    {
        var currentFrame = Core.FrameCounter.TotalFrames;
        if (_lastPreRenderFrame == currentFrame) return;
        _lastPreRenderFrame = currentFrame;
        
        foreach (var preview in AllPreviews)
        {
            preview.UpdateAndRender(deltaTime);
        }
    }

    public WeatherPreviewWidget(WeatherType weatherType, int width, int height)
    {
        _width = width;
        _height = height;
        Width = width;
        Height = height;
        
        _effect = CreateEffect(weatherType);
        _effect.SetDimensions(width, height);
        
        // Pre-populate particles
        for (int i = 0; i < _effect.PrePopulateCount; i++)
        {
            _effect.SpawnParticle(distributeAcrossScreen: true);
        }
        
        AllPreviews.Add(this);
    }

    private static IWeatherEffect CreateEffect(WeatherType type) => type switch
    {
        WeatherType.Showers => new ShowersEffect(),
        WeatherType.Storm => new StormEffect(),
        WeatherType.Snow => new SnowEffect(),
        WeatherType.SmokeEmbers => new SmokeEmbersEffect(),
        WeatherType.BloodRain => new BloodRainEffect(),
        WeatherType.Fireflies => new FirefliesEffect(),
        WeatherType.FallingLeaves => new FallingLeavesEffect(),
        WeatherType.HallowedRain => new HallowedRainEffect(),
        WeatherType.AcidDrips => new AcidDripsEffect(),
        WeatherType.Neutral => new NeutralEffect(),
        _ => new NeutralEffect()
    };

    private void EnsureInitialized()
    {
        if (_renderTarget != null) return;
        
        _renderTarget = new RenderTarget2D(
            Core.GraphicsDevice,
            _width,
            _height,
            false,
            SurfaceFormat.Color,
            DepthFormat.None,
            0,
            RenderTargetUsage.PreserveContents);
        
        _spriteBatch = new SpriteBatch(Core.GraphicsDevice);
    }

    private void UpdateAndRender(float deltaTime)
    {
        if (_disposed) return;
        
        EnsureInitialized();
        if (_renderTarget == null || _spriteBatch == null) return;
        
        // Spawn new particles
        _spawnAccumulator += _effect.SpawnRate * deltaTime;
        while (_spawnAccumulator >= 1f)
        {
            _effect.SpawnParticle(distributeAcrossScreen: false);
            _spawnAccumulator -= 1f;
        }
        
        _effect.Update(deltaTime);
        
        // Render to our target
        var previousTargets = Core.GraphicsDevice.GetRenderTargets();
        Core.GraphicsDevice.SetRenderTarget(_renderTarget);
        Core.GraphicsDevice.Clear(new Color(0, 0, 0)); // Dark background
        
        _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
        _effect.Render(_spriteBatch, 1f);
        _spriteBatch.End();
        
        Core.GraphicsDevice.SetRenderTargets(previousTargets);
    }

    public override void InternalRender(RenderContext context)
    {
        base.InternalRender(context);
        
        if (_renderTarget == null) return;
        
        var bounds = ActualBounds;
        var destRect = new Rectangle(bounds.X, bounds.Y, bounds.Width, bounds.Height);
        
        context.Draw(_renderTarget, destRect, Color.White);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        
        AllPreviews.Remove(this);
        _effect.Clear();
        _renderTarget?.Dispose();
        _spriteBatch?.Dispose();
        _renderTarget = null;
        _spriteBatch = null;
    }
}

