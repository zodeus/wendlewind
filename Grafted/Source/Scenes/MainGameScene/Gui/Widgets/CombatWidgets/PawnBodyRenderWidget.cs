using Myra.Graphics2D.Brushes;

namespace Grafted.Scenes.MainGameScene.Gui.Widgets.CombatWidgets;

/// <summary>
/// A Myra widget that displays a rendered pawn body.
/// Uses PawnBodyRenderer to composite all body part textures.
/// Falls back to the pawn's icon if no body layout is available.
/// </summary>
public class PawnBodyRenderWidget : Widget, IDisposable
{
    private readonly PawnBodyRenderer _renderer;
    private readonly Pawn _pawn;
    private readonly Texture2D? _fallbackIcon;
    private bool _isDisposed;

    public Pawn Pawn => _pawn;
    
    /// <summary>
    /// Returns true if this widget is using the composited body renderer.
    /// False means it's falling back to the pawn icon.
    /// </summary>
    public bool HasBodyRenderer => _renderer.HasValidLayout;

    /// <summary>
    /// Event fired when the widget is clicked.
    /// </summary>
    public event EventHandler<EventArgs>? Clicked;

    public PawnBodyRenderWidget(Pawn pawn, int renderSize = 512)
    {
        BorderThickness = new Thickness(2);
        Border = new SolidBrush(Color.Pink);
        _pawn = pawn;
        _renderer = new PawnBodyRenderer(pawn, renderSize);
        
        // If no valid layout, prepare fallback icon
        if (!_renderer.HasValidLayout)
        {
            _fallbackIcon = pawn.Icon;
        }
        
        // Set default size
        Width = BaseContent.IconSizes.Portrait;
        Height = BaseContent.IconSizes.Portrait;
        
        // Handle click events
        TouchDown += OnTouchDown;
    }

    private void OnTouchDown(object? sender, EventArgs e)
    {
        Clicked?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Forces the body to re-render on the next draw call.
    /// </summary>
    public void MarkDirty()
    {
        _renderer.MarkDirty();
    }

    /// <summary>
    /// Updates the renderer. Call this during the game update phase.
    /// </summary>
    public void Update()
    {
        // The renderer will automatically update when body parts change
    }

    public override void InternalRender(RenderContext context)
    {
        base.InternalRender(context);
        
        var bounds = ActualBounds;
        var destRect = new Rectangle(bounds.X, bounds.Y, bounds.Width, bounds.Height);
        
        if (_renderer.HasValidLayout)
        {
            // Render the body parts to the render target
            _renderer.Render();
            
            var texture = _renderer.RenderedTexture;
            if (texture != null)
            {
                context.Draw(texture, destRect, Color.White);
            }
        }
        else if (_fallbackIcon != null)
        {
            // Fall back to pawn icon
            context.Draw(_fallbackIcon, destRect, Color.White);
        }
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;
        
        TouchDown -= OnTouchDown;
        _renderer.Dispose();
    }
}
