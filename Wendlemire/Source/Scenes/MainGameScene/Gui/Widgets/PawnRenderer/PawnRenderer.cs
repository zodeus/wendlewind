using Wendlemire.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets;
using Wendlemire.Scenes.MainGameScene.Gui.Widgets.PawnRenderer.Weather;


namespace Wendlemire.Scenes.MainGameScene.Gui.Widgets.PawnRenderer;

/// <summary>
/// Renders a pawn's body by compositing external body part textures
/// and implants flagged <see cref="BodyPartDef.ShowOnPawnBody"/>.
/// Uses body-type-specific layouts to determine which textures to use.
/// 
/// Rendering is optimized using a layered caching approach:
/// - Body parts are cached in a separate render target and only re-rendered when parts change
/// - Dynamic effects (weather, blood) are composited on top of the cached body each frame
/// - This avoids expensive body part re-rendering when only effects are animating
/// </summary>
public class PawnRenderer : IDisposable
{
    // Static tracking of all active renderers for pre-rendering
    private static readonly List<PawnRenderer> _allRenderers = new();
    private static long _lastPreRenderFrame = -1;
    
    /// <summary>
    /// Pre-renders all active body renderers. Must be called BEFORE Myra's Desktop.Render()
    /// to avoid render target switching during UI rendering which causes flickering.
    /// This method is idempotent within a frame - subsequent calls are no-ops.
    /// </summary>
    public static void PreRenderAll(float deltaTime)
    {
        // Prevent multiple pre-renders per frame (e.g., ZoneGui calls before background,
        // BaseGui calls again in base.Draw). Without this check, active blood spurts
        // would cause the second Update() to mark renderers dirty again, triggering
        // another render target switch that discards the backbuffer (including the background).
        var currentFrame = Core.FrameCounter.TotalFrames;
        if (_lastPreRenderFrame == currentFrame)
            return;
        _lastPreRenderFrame = currentFrame;
        
        var isPaused = Core.Context?.IsPaused ?? false;
        
        foreach (var renderer in _allRenderers)
        {
            // Skip updates when paused, but still render if needed
            if (!isPaused)
            {
                renderer.Update(deltaTime);
            }
            renderer.Render();
        }
    }
    
    private readonly Pawn _pawn;
    private readonly IBodyPartLayout? _layout;
    private readonly BloodSpurtRenderer _bloodSpurtRenderer;
    private readonly WeatherRenderer _weatherRenderer;
    
    // Cached body layer - only re-rendered when body parts change
    private RenderTarget2D? _bodyRenderTarget;
    private bool _bodyDirty = true;
    
    // Track subscribed parts to detect when new parts are added
    private HashSet<BodyPart> _subscribedParts = new();
    private int _equipmentSignature = int.MinValue;
    
    // Final composite - rendered when effects are active or health text changed,
    // otherwise the last composite is reused so health text stays visible
    private RenderTarget2D? _compositeRenderTarget;
    private bool _needsComposite = true;
    private bool _hasComposited;
    private int _lastHealth = int.MinValue;
    private int _lastMaxHealth = int.MinValue;
    private string _healthText = "";
    private Vector2 _healthTextSize;
    
    private SpriteBatch? _spriteBatch;
    private readonly int _renderSize;
    private readonly List<(BodyPart part, BodyPartRenderInfo info)> _renderList = new();
    private bool _flipHorizontal;
    
    /// <summary>
    /// The rendered texture containing the composited body parts and effects.
    /// Returns the composite when effects are active, otherwise returns the cached body.
    /// </summary>
    public Texture2D? RenderedTexture => _hasComposited ? _compositeRenderTarget : _bodyRenderTarget;
    
    /// <summary>
    /// Returns true if this renderer has a valid layout for the pawn's body type.
    /// </summary>
    public bool HasValidLayout => _layout != null;

    /// <summary>
    /// The native size of the layout (for coordinate conversion).
    /// </summary>
    public int NativeSize => _layout?.NativeSize ?? _renderSize;

    /// <summary>
    /// The render size of this renderer.
    /// </summary>
    public int RenderSize => _renderSize;

    /// <summary>
    /// The pawn being rendered.
    /// </summary>
    public Pawn Pawn => _pawn;

    /// <summary>
    /// The layout used for rendering.
    /// </summary>
    public IBodyPartLayout? Layout => _layout;

    /// <summary>
    /// Optional current/max overlay used when displayed health should differ from
    /// live body values (e.g. prep-screen meal Body Scale before combat applies it).
    /// </summary>
    public Func<(int Current, int Max)>? HealthOverride { get; set; }

    /// <summary>
    /// When true, the body (including equipped weapons) is mirrored horizontally
    /// so an opponent faces the player. Health text stays unflipped.
    /// </summary>
    public bool FlipHorizontal
    {
        get => _flipHorizontal;
        set
        {
            if (_flipHorizontal == value) return;
            _flipHorizontal = value;
            _needsComposite = true;
        }
    }

    public PawnRenderer(Pawn pawn, int renderSize = 512)
    {
        _pawn = pawn;
        _renderSize = renderSize;
        _layout = BodyPartLayoutRegistry.GetLayoutFor(pawn.Body);
        _bloodSpurtRenderer = new BloodSpurtRenderer(pawn, _layout);
        _weatherRenderer = new WeatherRenderer();
        _weatherRenderer.SetDimensions(NativeSize, NativeSize);
        
        if (_layout != null)
        {
            // Subscribe to body part changes to mark dirty
            SubscribeToNewParts();
        }

        _pawn.Equipment.EquipmentChanged += MarkDirty;
        _equipmentSignature = EquipmentSignature();
        
        // Register for pre-rendering
        _allRenderers.Add(this);
    }

    private void OnPartHealthChanged(BodyPart part)
    {
        _bodyDirty = true;
    }
    
    /// <summary>
    /// Subscribes to HealthChanged events for any new parts that haven't been subscribed to yet.
    /// Marks the body dirty if new parts are found.
    /// </summary>
    private void SubscribeToNewParts()
    {
        foreach (var part in OverlayBodyPartLayout.VisibleParts(_pawn.Body))
        {
            if (_subscribedParts.Add(part))
            {
                part.HealthChanged += OnPartHealthChanged;
                _bodyDirty = true;
            }
        }
    }

    /// <summary>
    /// Updates the blood spurt and weather renderers. Called each frame.
    /// </summary>
    private void Update(float deltaTime)
    {
        _bloodSpurtRenderer.Update(deltaTime);
        _weatherRenderer.Update(deltaTime);
        
        // Check for new parts (e.g., Hydra head regeneration) and subscribe to them
        if (_layout != null)
        {
            SubscribeToNewParts();
        }

        var equipmentSignature = EquipmentSignature();
        if (equipmentSignature != _equipmentSignature)
        {
            _equipmentSignature = equipmentSignature;
            _bodyDirty = true;
        }

        var (currentHealth, maxHealth) = HealthOverride?.Invoke()
            ?? ((int)Math.Ceiling(_pawn.Body.HitPoints), (int)_pawn.Body.MaxHitPoints);
        var healthChanged = currentHealth != _lastHealth || maxHealth != _lastMaxHealth;
        if (healthChanged)
        {
            _lastHealth = currentHealth;
            _lastMaxHealth = maxHealth;
            _healthText = $"{currentHealth}/{maxHealth}";
            _healthTextSize = Vector2.Zero;
        }

        _needsComposite = _needsComposite
            || _bodyDirty
            || !_hasComposited
            || healthChanged
            || _bloodSpurtRenderer.HasActiveSpurts
            || _weatherRenderer.HasActiveEffects;
    }

    /// <summary>
    /// Ensures the render targets and sprite batch are initialized.
    /// </summary>
    private void EnsureInitialized()
    {
        // Body render target - caches body parts, only re-rendered when parts change
        if (_bodyRenderTarget == null)
        {
            _bodyRenderTarget = new RenderTarget2D(
                Core.GraphicsDevice,
                _renderSize,
                _renderSize,
                false,
                SurfaceFormat.Color,
                DepthFormat.None,
                0,
                RenderTargetUsage.PreserveContents);
        }
        
        // Composite render target - used when effects need to overlay on body
        if (_compositeRenderTarget == null)
        {
            _compositeRenderTarget = new RenderTarget2D(
                Core.GraphicsDevice,
                _renderSize,
                _renderSize,
                false,
                SurfaceFormat.Color,
                DepthFormat.None,
                0,
                RenderTargetUsage.PreserveContents);
        }

        _spriteBatch ??= new SpriteBatch(Core.GraphicsDevice);
    }

    /// <summary>
    /// Marks the body as needing to re-render (e.g., after equipment changes).
    /// </summary>
    public void MarkDirty()
    {
        _bodyDirty = true;
        _equipmentSignature = EquipmentSignature();
    }

    private int EquipmentSignature()
    {
        var hash = 17;
        foreach (var part in _pawn.Body.AllExternalParts)
        {
            foreach (var (slot, item) in part.Equipment)
            {
                hash = HashCode.Combine(hash, part.Id, (int)slot, item?.Id ?? 0);
            }
        }

        return hash;
    }
    
    /// <summary>
    /// Sets the weather effect for this renderer, disabling automatic weather cycling.
    /// </summary>
    public void SetWeather(WeatherType weatherType)
    {
        _weatherRenderer.SetWeather(weatherType);
    }
    
    /// <summary>
    /// Sets the weather effect from a WeatherDef.
    /// </summary>
    public void SetWeather(WeatherDef weatherDef)
    {
        _weatherRenderer.SetWeather(weatherDef);
    }

    /// <summary>
    /// Renders the pawn using a layered caching approach:
    /// 1. Body parts are cached and only re-rendered when they change
    /// 2. Dynamic effects are composited on top each frame (only when active)
    /// </summary>
    public void Render()
    {
        if (_layout == null) return;
        
        EnsureInitialized();
        
        var previousRenderTargets = Core.GraphicsDevice.GetRenderTargets();
        var layoutScale = (float)_renderSize / _layout.NativeSize;
        
        // Step 1: Render body to cache if dirty (expensive, but rare)
        var bodyWasDirty = _bodyDirty;
        if (_bodyDirty)
        {
            RenderBodyToCache(previousRenderTargets, layoutScale);
            _bodyDirty = false;
        }
        
        // Step 2: Composite only when blood, weather, health text, or body cache changed
        if (_needsComposite || bodyWasDirty)
        {
            RenderComposite(previousRenderTargets, layoutScale);
            _hasComposited = true;
            _needsComposite = false;
        }
    }
    
    /// <summary>
    /// Renders body parts to the body cache render target.
    /// Only called when body parts have changed.
    /// </summary>
    private void RenderBodyToCache(RenderTargetBinding[] previousTargets, float layoutScale)
    {
        Core.GraphicsDevice.SetRenderTarget(_bodyRenderTarget);
        Core.GraphicsDevice.Clear(Color.Transparent);
        
        _spriteBatch!.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
        RenderBodyParts(_spriteBatch);
        _spriteBatch.End();
        
        Core.GraphicsDevice.SetRenderTargets(previousTargets);
    }
    
    /// <summary>
    /// Composites the cached body with dynamic effects (blood, weather).
    /// Called each frame when effects are active.
    /// </summary>
    private void RenderComposite(RenderTargetBinding[] previousTargets, float layoutScale)
    {
        Core.GraphicsDevice.SetRenderTarget(_compositeRenderTarget);
        Core.GraphicsDevice.Clear(Color.Transparent);
        
        _spriteBatch!.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
        
        // Draw cached body (single texture blit - very cheap). Flip so opponents face the player.
        var bodyEffects = _flipHorizontal ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
        _spriteBatch.Draw(
            _bodyRenderTarget,
            new Rectangle(0, 0, _renderSize, _renderSize),
            null,
            Color.White,
            0f,
            Vector2.Zero,
            bodyEffects,
            0f);
        
        // Render blood spurts from open, unsealed sockets
        _bloodSpurtRenderer.Render(_spriteBatch, layoutScale, _flipHorizontal, _renderSize);
        
        // Render weather effects overlay
        _weatherRenderer.Render(_spriteBatch, layoutScale);
        
        // Render health text at top center
        RenderHealthText(_spriteBatch);
        
        _spriteBatch.End();
        
        Core.GraphicsDevice.SetRenderTargets(previousTargets);
    }

    /// <summary>
    /// Renders the health text (current/max) at the top center of the render target.
    /// </summary>
    private void RenderHealthText(SpriteBatch spriteBatch)
    {
        // Scale font based on render size (Medium = 30pt is baseline for 512px render target)
        var font = _renderSize switch
        {
            <= 128 => BaseContent.Fonts.Default.VerySmall,
            <= 256 => BaseContent.Fonts.Default.Small,
            <= 384 => BaseContent.Fonts.Default.Normal,
            _ => BaseContent.Fonts.Default.Large
        };
        if (_healthTextSize == Vector2.Zero && !string.IsNullOrEmpty(_healthText))
        {
            _healthTextSize = font.MeasureString(_healthText);
        }
        
        // Position at top center with padding scaled to render size
        var x = (_renderSize - _healthTextSize.X) / 2f;
        var y = _renderSize * 0.02f; // 2% padding from top
        
        // Draw shadow/outline for readability
        var shadowOffset = Math.Max(1, _renderSize / 256);
        var shadowColor = Color.Black * 0.8f;
        spriteBatch.DrawString(font, _healthText, new Vector2(x + shadowOffset, y + shadowOffset), shadowColor);
        
        // Draw main text with color based on health percentage
        var textColor = BodyPartColor.Get(_pawn.Body);
        spriteBatch.DrawString(font, _healthText, new Vector2(x, y), textColor);
    }

    /// <summary>
    /// Renders all external body parts using the layout.
    /// </summary>
    private void RenderBodyParts(SpriteBatch spriteBatch)
    {
        if (_layout == null) return;
        
        _renderList.Clear();
        foreach (var part in OverlayBodyPartLayout.VisibleParts(_pawn.Body))
        {
            var renderInfo = _layout.GetRenderInfo(part);
            if (renderInfo.HasValue)
            {
                _renderList.Add((part, renderInfo.Value));
            }
        }
        
        _renderList.Sort((a, b) => a.info.RenderOrder.CompareTo(b.info.RenderOrder));
        
        // Calculate scale to fit render target
        float layoutScale = (float)_renderSize / _layout.NativeSize;
        
        // Render each part and its equipped weapons/armor
        foreach (var (part, info) in _renderList)
        {
            // Render equipped weapons BEFORE the body part (so they appear behind/underneath)
            BodyPartRenderHelper.RenderEquippedWeapons(spriteBatch, part, info, layoutScale: layoutScale);
            
            var tint = part.BodyPartDef.ShowOnPawnBody && !part.IsDestroyed
                ? Color.White
                : BodyPartColor.Get(part);
            BodyPartRenderHelper.RenderBodyPart(spriteBatch, info, layoutScale: layoutScale, tint: tint);
            
            // Render equipped armor AFTER the body part (so it appears on top)
            BodyPartRenderHelper.RenderEquippedArmor(spriteBatch, part, info, layoutScale: layoutScale);
        }
    }

    public void Dispose()
    {
        // Unregister from pre-rendering
        _allRenderers.Remove(this);
        _pawn.Equipment.EquipmentChanged -= MarkDirty;
        
        // Unsubscribe from all tracked parts
        foreach (var part in _subscribedParts)
        {
            part.HealthChanged -= OnPartHealthChanged;
        }
        _subscribedParts.Clear();
        
        _bodyRenderTarget?.Dispose();
        _compositeRenderTarget?.Dispose();
        _spriteBatch?.Dispose();
        _bodyRenderTarget = null;
        _compositeRenderTarget = null;
        _spriteBatch = null;
    }
}
