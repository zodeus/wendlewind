using Grafted.Scenes.MainGameScene.Gui.Widgets.CombatWidgets.BodyPartLayouts;
using Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets;
using Grafted.Scenes.MainGameScene.Gui.Widgets.PawnRenderer.Weather;


namespace Grafted.Scenes.MainGameScene.Gui.Widgets.PawnRenderer;

/// <summary>
/// Renders a pawn's body by compositing all external body part textures.
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
    
    // Final composite - rendered each frame when effects are active, 
    // or just references body cache when no effects
    private RenderTarget2D? _compositeRenderTarget;
    private bool _hasActiveEffects;
    
    private SpriteBatch? _spriteBatch;
    private readonly int _renderSize;
    
    /// <summary>
    /// The rendered texture containing the composited body parts and effects.
    /// Returns the composite when effects are active, otherwise returns the cached body.
    /// </summary>
    public Texture2D? RenderedTexture => _hasActiveEffects ? _compositeRenderTarget : _bodyRenderTarget;
    
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
            foreach (var part in _pawn.Body.AllExternalParts)
            {
                part.HealthChanged += OnPartHealthChanged;
            }
        }
        
        // Register for pre-rendering
        _allRenderers.Add(this);
    }

    private void OnPartHealthChanged(BodyPart part)
    {
        _bodyDirty = true;
    }

    /// <summary>
    /// Updates the blood spurt and weather renderers. Called each frame.
    /// </summary>
    private void Update(float deltaTime)
    {
        _bloodSpurtRenderer.Update(deltaTime);
        _weatherRenderer.Update(deltaTime);
        
        // Track whether effects are currently active (determines which render target to return)
        _hasActiveEffects = _bloodSpurtRenderer.HasActiveSpurts || _weatherRenderer.HasActiveEffects;
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
        if (_bodyDirty)
        {
            RenderBodyToCache(previousRenderTargets, layoutScale);
            _bodyDirty = false;
        }
        
        // Step 2: If effects are active, composite body cache + effects
        // Otherwise, RenderedTexture already points to the body cache
        if (_hasActiveEffects)
        {
            RenderComposite(previousRenderTargets, layoutScale);
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
        
        // Draw cached body (single texture blit - very cheap)
        _spriteBatch.Draw(
            _bodyRenderTarget, 
            new Rectangle(0, 0, _renderSize, _renderSize), 
            Color.White);
        
        // Render blood spurts from open, unsealed sockets
        _bloodSpurtRenderer.Render(_spriteBatch, layoutScale);
        
        // Render weather effects overlay
        _weatherRenderer.Render(_spriteBatch, layoutScale);
        
        _spriteBatch.End();
        
        Core.GraphicsDevice.SetRenderTargets(previousTargets);
    }

    /// <summary>
    /// Renders all external body parts using the layout.
    /// </summary>
    private void RenderBodyParts(SpriteBatch spriteBatch)
    {
        if (_layout == null) return;
        
        var parts = _pawn.Body.AllExternalParts;
        
        // Get render info for all parts and sort by render order
        var renderList = new List<(BodyPart part, BodyPartRenderInfo info)>();
        
        foreach (var part in parts)
        {
            // Skip severed parts (physically removed), but keep destroyed parts (damaged but still attached)
            if (part.IsSevered) continue;
            
            var renderInfo = _layout.GetRenderInfo(part);
            if (renderInfo.HasValue)
            {
                renderList.Add((part, renderInfo.Value));
            }
        }
        
        // Sort by render order (back to front)
        renderList.Sort((a, b) => a.info.RenderOrder.CompareTo(b.info.RenderOrder));
        
        // Calculate scale to fit render target
        float layoutScale = (float)_renderSize / _layout.NativeSize;
        
        // Render each part using the shared helper
        foreach (var (part, info) in renderList)
        {
            var tint = BodyPartColor.Get(part);
            BodyPartRenderHelper.RenderBodyPart(spriteBatch, info, layoutScale: layoutScale, tint: tint);
        }
    }

    public void Dispose()
    {
        // Unregister from pre-rendering
        _allRenderers.Remove(this);
        
        // Unsubscribe from events
        if (_layout != null)
        {
            foreach (var part in _pawn.Body.AllExternalParts)
            {
                part.HealthChanged -= OnPartHealthChanged;
            }
        }
        
        _bodyRenderTarget?.Dispose();
        _compositeRenderTarget?.Dispose();
        _spriteBatch?.Dispose();
        _bodyRenderTarget = null;
        _compositeRenderTarget = null;
        _spriteBatch = null;
    }
}
