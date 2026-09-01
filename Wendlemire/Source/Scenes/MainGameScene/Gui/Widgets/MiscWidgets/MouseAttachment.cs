namespace Wendlemire.Scenes.MainGameScene.Gui.Widgets.MiscWidgets;

public class MouseAttachment {
    public Size IconSize { get; set; } = new(18, 18);

    private readonly BaseGui _gui;
    private readonly Texture2D _texture;

    private readonly Action<MouseAttachment> _leftClickAction;

    private readonly Action<MouseAttachment>? _updateAction;

    private readonly Action<MouseAttachment, SpriteBatch>? _renderAction;
    public Color DrawColor = Color.White;

    public string CurrentState = "Default";
    public object? Data { get; set; }
    public string? Text { get; set; } = null;

    private ButtonState _previousLeftButtonState = ButtonState.Released;

    public MouseAttachment(BaseGui gui, Texture2D texture, Action<MouseAttachment>? leftClickAction = null, Action<MouseAttachment>? updateAction = null, Action<MouseAttachment, SpriteBatch>? renderAction = null) {
        _gui = gui;
        _texture = texture;
        _leftClickAction = leftClickAction ?? (attachment => { });
        _updateAction = updateAction;
        _renderAction = renderAction;
    }

    public void Update() {
        _updateAction?.Invoke(this);
        
        var currentLeftButtonState = Mouse.GetState().LeftButton;
        if (currentLeftButtonState == ButtonState.Pressed && _previousLeftButtonState == ButtonState.Released) {
            Log.Debug("Attachment Clicked");
            _leftClickAction?.Invoke(this);
        }
        _previousLeftButtonState = currentLeftButtonState;

        if (Keyboard.GetState().IsKeyDown(Keys.Escape)) {
            DetachInternal();
        }
    }

    private void DetachInternal() {
        Log.Debug("Attachment Cancelled");
        Core.Instance.IsMouseVisible = true;
        _gui.MouseAttachment = null;
    }

    public void Detach() {
        DetachInternal();
    }

    public void Draw(SpriteBatch spriteBatch) {
        Vector2 position = Mouse.GetState().Position.ToVector2();

        spriteBatch.Draw(
            _texture,
            new Rectangle((int) position.X + 8, (int) position.Y, IconSize.Width, IconSize.Height),
            null,
            DrawColor,
            0,
            Vector2.Zero,
            SpriteEffects.None,
            0f
        );

        if (Text != null) {
            spriteBatch.DrawString(
                BaseContent.Fonts.Default.Large,
                Text,
                position + new Vector2(IconSize.Width, 10),
                DrawColor
            );
        }

        _renderAction?.Invoke(this, spriteBatch);
    }
}