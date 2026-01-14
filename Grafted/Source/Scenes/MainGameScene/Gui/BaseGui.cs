using Grafted.Scenes.MainGameScene.Gui.Widgets.DevConsole;
using Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets;
using Grafted.Scenes.MainGameScene.Gui.Widgets.MiscWidgets.Boak;

namespace Grafted.Scenes.MainGameScene.Gui;

public abstract class BaseGui : IDisposable
{
    private BookOfAllKnowledgeWindow? Boak;
    public Desktop Desktop { get; set; } = null!;
    public abstract WorldTextHandler WorldTextHandler { get; }

    public MouseAttachment? MouseAttachment;

    // Developer console
    protected DevConsole? Console;

    // Text input handler for Myra
    private EventHandler<TextInputEventArgs>? _textInputHandler;

    private ScreenMessageData? _screenMessage;
    private float _screenMessageTimeLeft;
    private readonly Window _entityViewerWindow = new();
    private Entity? _viewedEntity;
    private KeyValuePair<Entity, Point?>? _queuedEntityToView;

    public virtual void Update(float deltaTime)
    {
        HandleInput();
        Console?.UpdateInput();
        MouseAttachment?.Update();
        ShowEntityIfQueued();
        if (_viewedEntity?.IsDestroyed == true)
        {
            _viewedEntity = null;
            _entityViewerWindow.Close();
        }

        ((EntityPanelBase?)_entityViewerWindow.Content)?.Update();
        if (_screenMessageTimeLeft > 0)
        {
            _screenMessageTimeLeft -= deltaTime;
        }
    }

    public virtual void ViewEntity(Entity entity, Point? position = null)
    {
        _queuedEntityToView = new KeyValuePair<Entity, Point?>(entity, position);
    }

    public virtual void HandleInput()
    {
        // Don't process other input when console is open
        if (IsConsoleOpen) return;

        if (Keyboard.GetState().IsKeyDown(Keys.B))
        {
            OpenBoak();
        }
    }

    /// <summary>
    /// Returns true if the console is currently open and capturing input.
    /// </summary>
    public bool IsConsoleOpen => Console?.IsOpen == true;

    /// <summary>
    /// Toggles the developer console visibility.
    /// </summary>
    public void ToggleConsole()
    {
        Console?.Toggle();
    }

    /// <summary>
    /// Initializes the developer console and adds it to the Desktop.
    /// Call this after Desktop.Root is set.
    /// </summary>
    protected void InitializeConsole()
    {
        // Hook up text input for Myra (required when HasExternalTextInput = true)
        _textInputHandler = (_, e) => Desktop.OnChar(e.Character);
        Core.Instance.Window.TextInput += _textInputHandler;

        Console = new DevConsole(Desktop);
        Console.OnClose += () => Desktop.FocusedKeyboardWidget = null;

        // Add console to desktop - it will be on top of other widgets
        if (Desktop.Root is not Panel panel)
        {
            Log.Error("Desktop.Root is not a Panel");
            return;
        }
        panel.Widgets.Add(Console);
    }

    /// <summary>
    /// Brings the console to the front of the widget stack.
    /// Call this after adding other widgets to the Desktop.Root panel.
    /// </summary>
    protected void BringConsoleToFront()
    {
        if (Console == null || Desktop.Root is not Panel panel) return;
        
        // Remove and re-add to move to end of widget list (renders on top)
        if (panel.Widgets.Contains(Console))
        {
            panel.Widgets.Remove(Console);
            panel.Widgets.Add(Console);
        }
    }

    /// <summary>
    /// Cleans up text input handler. Call this in Dispose().
    /// </summary>
    protected void CleanupTextInput()
    {
        if (_textInputHandler != null)
        {
            Core.Instance.Window.TextInput -= _textInputHandler;
            _textInputHandler = null;
        }
    }

    public void OpenBoak()
    {
        if (Boak != null)
        {
            Boak.Close();
        }

        Boak = new BookOfAllKnowledgeWindow();
        Boak.Show(Desktop, new Point(0, 0));
    }

    public virtual void Draw(SpriteBatch spriteBatch, float deltaTime)
    {
        // Pre-render all body renderers before Myra's render pass
        // to avoid render target switching during UI rendering which causes flickering.
        // Note: ZoneGui calls this earlier (before drawing its background) to prevent
        // backbuffer discard when targets switch. This call is a no-op in that case
        // since renderers are already clean, but serves as safety for other GUI subclasses.
        PawnRenderer.PreRenderAll(deltaTime);
        WeatherPreviewWidget.PreRenderAll(deltaTime);

        Desktop.Render();
        spriteBatch.Begin(
            SpriteSortMode.Deferred,
            BlendState.NonPremultiplied,
            SamplerState.PointClamp,
            DepthStencilState.None,
            RasterizerState.CullNone
        );
        MouseAttachment?.Draw(spriteBatch);
        spriteBatch.End();

        if (_screenMessageTimeLeft > 0 && _screenMessage != null)
        {
            spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.NonPremultiplied,
                SamplerState.PointClamp,
                DepthStencilState.None,
                RasterizerState.CullNone
            );
            int offset = (int)_screenMessage.Font.MeasureString(_screenMessage.Text).X / 2;
            Color colorA = _screenMessage.Color.Multiply(new Color(1f, 1f, 1f, Mathf.Lerp(0f, 1f, _screenMessageTimeLeft / _screenMessage.Duration)));
            Color colorB = _screenMessage.Color.Multiply(new Color(1f, 1f, 1f, Mathf.Lerp(0f, .6f, _screenMessageTimeLeft / _screenMessage.Duration)));
            int xOffsetA = Core.Random.Next(-2, 2);
            int yOffsetA = Core.Random.Next(-2, 2);
            int xOffsetB = Core.Random.Next(-8, 8);
            int yOffsetB = Core.Random.Next(-8, 8);

            spriteBatch.DrawString(_screenMessage.Font, _screenMessage.Text, new Vector2((Screen.Width / 2) - offset + xOffsetB, 400 + yOffsetB), colorB);
            //spriteBatch.DrawString(BaseContent.Fonts.Default.VeryLarge, _screenMessage, new Vector2((Screen.Width / 2) - offset + Core.Random.Next(-5, 5), 300 + Core.Random.Next(-5, 5)), colorB);
            spriteBatch.DrawString(_screenMessage.Font, _screenMessage.Text, new Vector2((Screen.Width / 2) - offset + xOffsetA, 400 + yOffsetA), colorA);
            spriteBatch.End();
        }
    }

    public void PushScreenMessage(ScreenMessageData message)
    {
        _screenMessage = message;
        _screenMessageTimeLeft = message.Duration;
    }

    public void ClearScreenMessage()
    {
        _screenMessage = null;
        _screenMessageTimeLeft = 0;
    }

    private void ShowEntityIfQueued()
    {
        if (_queuedEntityToView == null)
        {
            return;
        }

        if (_entityViewerWindow.IsPlaced)
        {
            _entityViewerWindow.Close();
        }

        _entityViewerWindow.Content = _queuedEntityToView.Value.Key.UiPanel(this, new EntityPanelProperties
        {
            ShowTitle = false,
            ShowCloseButton = false,
            Background = null
        });
        _entityViewerWindow.Title = _queuedEntityToView.Value.Key.Label;
        _entityViewerWindow.Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrame];

        if (_queuedEntityToView.Value.Value.HasValue)
        {
            _entityViewerWindow.Show(Desktop, _queuedEntityToView.Value.Value.Value);
        }
        else
        {
            // Show first, then reposition to center based on actual size
            _entityViewerWindow.Show(Desktop);
            _entityViewerWindow.Arrange(new Rectangle(0, 0, Core.ReferenceResolution.X, Core.ReferenceResolution.Y));
            var windowWidth = _entityViewerWindow.ActualBounds.Width;
            var centerX = (Core.ReferenceResolution.X - windowWidth) / 2;
            _entityViewerWindow.Left = centerX;
            _entityViewerWindow.Top = 100;
        }

        _viewedEntity = _queuedEntityToView.Value.Key;

        _queuedEntityToView = null;
    }

    public void CloseEntityWindow()
    {
        _entityViewerWindow.Close();
    }

    public void TransferScreenMessage(BaseGui gui)
    {
        gui._screenMessage = _screenMessage;
        gui._screenMessageTimeLeft = _screenMessageTimeLeft;
    }

    public virtual void Dispose()
    {
        CleanupTextInput();
    }
}

public class ScreenMessageData
{
    public string Text = "";
    public Color Color;
    public int Duration = 10000;
    public DynamicSpriteFont Font = BaseContent.Fonts.Default.VeryLarge;
}