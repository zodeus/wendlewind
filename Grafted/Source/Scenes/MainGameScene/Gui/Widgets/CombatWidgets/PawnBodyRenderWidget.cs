using Myra.Graphics2D.Brushes;

namespace Grafted.Scenes.MainGameScene.Gui.Widgets.CombatWidgets;

/// <summary>
/// Internal widget that handles the actual body rendering.
/// </summary>
internal class PawnBodyRenderArea : Widget
{
    private readonly PawnBodyRenderer _renderer;
    private readonly Texture2D? _fallbackIcon;

    public PawnBodyRenderer Renderer => _renderer;
    public bool HasValidLayout => _renderer.HasValidLayout;

    public PawnBodyRenderArea(PawnBodyRenderer renderer, Texture2D? fallbackIcon)
    {
        _renderer = renderer;
        _fallbackIcon = fallbackIcon;
    }

    public override void InternalRender(RenderContext context)
    {
        base.InternalRender(context);
        
        var bounds = ActualBounds;
        var destRect = new Rectangle(bounds.X, bounds.Y, bounds.Width, bounds.Height);
        
        if (_renderer.HasValidLayout)
        {
            _renderer.Render();
            
            var texture = _renderer.RenderedTexture;
            if (texture != null)
            {
                context.Draw(texture, destRect, Color.White);
            }
        }
        else if (_fallbackIcon != null)
        {
            context.Draw(_fallbackIcon, destRect, Color.White);
        }
    }
}

/// <summary>
/// A Myra widget that displays a rendered pawn body.
/// Uses PawnBodyRenderer to composite all body part textures.
/// Falls back to the pawn's icon if no body layout is available.
/// Includes an Edit button that opens a separate editor window.
/// </summary>
public class PawnBodyRenderWidget : Panel, IDisposable
{
    private readonly PawnBodyRenderer _renderer;
    private readonly PawnBodyRenderArea _renderArea;
    private readonly Pawn _pawn;
    private readonly Button _editorButton;
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

    /// <summary>
    /// Provides access to the underlying renderer for advanced operations.
    /// </summary>
    public PawnBodyRenderer Renderer => _renderer;

    public PawnBodyRenderWidget(Pawn pawn, int renderSize = 512)
    {
        _pawn = pawn;
        _renderer = new PawnBodyRenderer(pawn, renderSize);
        
        // Get fallback icon if no valid layout
        Texture2D? fallbackIcon = null;
        if (!_renderer.HasValidLayout)
        {
            fallbackIcon = pawn.Icon;
        }
        
        // Create the render area
        _renderArea = new PawnBodyRenderArea(_renderer, fallbackIcon)
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };
        Widgets.Add(_renderArea);
        
        // Set default appearance
        BorderThickness = new Thickness(2);
        Border = new SolidBrush(Color.Pink);
        
        // Create edit button in top-right corner (only if we have a valid layout)
        if (_renderer.HasValidLayout)
        {
            var controlPanel = new VerticalStackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Spacing = 2,
                Margin = new Thickness(2)
            };
            
            _editorButton = new Button(BaseContent.Styles.Button.Small)
            {
                Content = new Label(BaseContent.Styles.Label.Small) { Text = "Edit" },
                Width = 50,
                Height = 20
            };
            _editorButton.Click += OnEditButtonClick;
            controlPanel.Widgets.Add(_editorButton);
            
            Widgets.Add(controlPanel);
        }
        else
        {
            _editorButton = null!;
        }
        
        // Handle click events
        TouchDown += OnTouchDown;
    }

    private void OnEditButtonClick(object? sender, EventArgs e)
    {
        // Open the body part editor window
        var editorWindow = new BodyPartEditorWindow(_pawn);
        editorWindow.ShowModal(Desktop);
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

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;
        
        TouchDown -= OnTouchDown;
        if (_editorButton != null)
        {
            _editorButton.Click -= OnEditButtonClick;
        }
        _renderer.Dispose();
    }
}
