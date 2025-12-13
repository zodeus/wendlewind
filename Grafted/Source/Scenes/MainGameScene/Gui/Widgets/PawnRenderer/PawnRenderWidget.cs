using Grafted.Scenes.MainGameScene.Gui.Widgets.PawnRenderer.Weather;

namespace Grafted.Scenes.MainGameScene.Gui.Widgets.PawnRenderer;

/// <summary>
/// A Myra widget that displays a rendered pawn body.
/// Uses PawnBodyRenderer to composite all body part textures.
/// Falls back to the pawn's icon if no body layout is available.
/// Includes an Edit button that opens a separate editor window.
/// </summary>
public class PawnRenderWidget : Panel, IDisposable
{
    private readonly PawnRenderer _renderer;
    private readonly PawnRenderArea _renderArea;
    private readonly Pawn _pawn;
    private readonly Button _editorButton;
    private readonly VerticalStackPanel? _editorControlPanel;
    private bool _isDisposed;

    public Pawn Pawn => _pawn;
    
    /// <summary>
    /// Gets or sets whether the Edit button is visible. Defaults to false.
    /// </summary>
    public bool ShowEditButton
    {
        get => _editorControlPanel?.Visible ?? false;
        set
        {
            if (_editorControlPanel != null)
                _editorControlPanel.Visible = value;
        }
    }

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
    public PawnRenderer Renderer => _renderer;

    /// <summary>
    /// Provides access to the damage text renderer for adding floating damage numbers.
    /// </summary>
    public BodyPartDamageTextRenderer DamageTextRenderer => _renderArea.DamageTextRenderer;

    public PawnRenderWidget(Pawn pawn, int renderSize = 512)
    {
        _pawn = pawn;
        Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.DeepGold];
        Padding = new Thickness(5);
        _renderer = new PawnRenderer(pawn, renderSize);

        // Get fallback icon if no valid layout
        Texture2D? fallbackIcon = null;
        if (!_renderer.HasValidLayout)
        {
            fallbackIcon = pawn.Icon;
        }

        // Create the render area
        _renderArea = new PawnRenderArea(_renderer, fallbackIcon)
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };
        Widgets.Add(_renderArea);


        // Create edit button in top-right corner (only if we have a valid layout)
        // Hidden by default - set ShowEditButton = true to display
        if (_renderer.HasValidLayout)
        {
            _editorControlPanel = new VerticalStackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Spacing = 2,
                Margin = new Thickness(2),
                Visible = false
            };

            _editorButton = new Button(BaseContent.Styles.Button.Small)
            {
                Content = new Label(BaseContent.Styles.Label.Small) { Text = "Edit" },
                Width = 50,
                Height = 20
            };
            _editorButton.Click += OnEditButtonClick;
            _editorControlPanel.Widgets.Add(_editorButton);

            Widgets.Add(_editorControlPanel);
        }
        else
        {
            _editorButton = null!;
            _editorControlPanel = null;
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
    
    /// <summary>
    /// Sets the weather effect for this widget, disabling automatic weather cycling.
    /// </summary>
    public void SetWeather(WeatherType weatherType)
    {
        _renderer.SetWeather(weatherType);
    }

    /// <summary>
    /// Pre-renders the body to the render target. Must be called BEFORE Myra's Desktop.Render()
    /// to avoid render target switching during UI rendering which causes flickering.
    /// </summary>
    public void PreRender()
    {
        _renderer.Render();
    }

    /// <summary>
    /// Updates the damage text animations. Should be called each frame.
    /// </summary>
    public void Update(float deltaTime)
    {
        _renderArea.DamageTextRenderer.Update(deltaTime);
    }

    /// <summary>
    /// Adds a floating damage text near a body part.
    /// </summary>
    public void AddDamageText(BodyPart? bodyPart, string text, DynamicSpriteFont font, Color color, float duration = 2f)
    {
        _renderArea.DamageTextRenderer.AddDamageText(bodyPart, text, font, color, duration);
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
