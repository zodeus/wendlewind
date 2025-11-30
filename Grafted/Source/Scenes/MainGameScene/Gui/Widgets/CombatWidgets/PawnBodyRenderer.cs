using Grafted.Scenes.MainGameScene.Gui.Widgets.CombatWidgets.BodyPartLayouts;
using Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets;

namespace Grafted.Scenes.MainGameScene.Gui.Widgets.CombatWidgets;

/// <summary>
/// Renders a pawn's body by compositing all external body part textures.
/// Uses body-type-specific layouts to determine which textures to use.
/// </summary>
public class PawnBodyRenderer : IDisposable
{
    // Static tracking of all active renderers for pre-rendering
    private static readonly List<PawnBodyRenderer> _allRenderers = new();
    
    /// <summary>
    /// Pre-renders all active body renderers. Must be called BEFORE Myra's Desktop.Render()
    /// to avoid render target switching during UI rendering which causes flickering.
    /// </summary>
    public static void PreRenderAll()
    {
        foreach (var renderer in _allRenderers)
        {
            renderer.Render();
        }
    }
    
    private readonly Pawn _pawn;
    private readonly IBodyPartLayout? _layout;
    private RenderTarget2D? _renderTarget;
    private SpriteBatch? _spriteBatch;
    private bool _isDirty = true;
    private readonly int _renderSize;
    
    /// <summary>
    /// The rendered texture containing the composited body parts.
    /// </summary>
    public Texture2D? RenderedTexture => _renderTarget;
    
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

    public PawnBodyRenderer(Pawn pawn, int renderSize = 512)
    {
        _pawn = pawn;
        _renderSize = renderSize;
        _layout = BodyPartLayoutRegistry.GetLayoutFor(pawn.Body);
        
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
        _isDirty = true;
    }

    /// <summary>
    /// Ensures the render target and sprite batch are initialized.
    /// </summary>
    private void EnsureInitialized()
    {
        if (_renderTarget == null)
        {
            _renderTarget = new RenderTarget2D(
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
    /// Marks the renderer as needing to re-render.
    /// </summary>
    public void MarkDirty()
    {
        _isDirty = true;
    }

    /// <summary>
    /// Renders the body parts to the render target if dirty.
    /// Should be called during the game's Draw phase.
    /// </summary>
    public void Render()
    {
        if (!_isDirty || _layout == null) return;
        
        EnsureInitialized();
        
        var previousRenderTargets = Core.GraphicsDevice.GetRenderTargets();
        
        Core.GraphicsDevice.SetRenderTarget(_renderTarget);
        Core.GraphicsDevice.Clear(Color.Transparent);
        
        _spriteBatch!.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
        
        RenderBodyParts(_spriteBatch);
        
        _spriteBatch.End();
        
        // Restore previous render targets
        Core.GraphicsDevice.SetRenderTargets(previousRenderTargets);
        
        _isDirty = false;
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
        
        _renderTarget?.Dispose();
        _spriteBatch?.Dispose();
        _renderTarget = null;
        _spriteBatch = null;
    }
}
