namespace Wendlemire.Scenes.MainGameScene.Gui.Widgets;

/// <summary>
/// A Button that changes the mouse cursor to a hand pointer on hover.
/// </summary>
public class CursorButton : Button
{
    private bool _cursorSet;

    public CursorButton() : base()
    {
        SetupCursorHandling();
    }

    public CursorButton(string? styleName) : base(styleName)
    {
        SetupCursorHandling();
    }

    private void SetupCursorHandling()
    {
        MouseEntered += OnMouseEntered;
        MouseLeft += OnMouseLeft;
    }

    private void OnMouseEntered(object? sender, EventArgs e)
    {
        if (!Enabled)
        {
            return;
        }

        Mouse.SetCursor(Microsoft.Xna.Framework.Input.MouseCursor.Hand);
        _cursorSet = true;
    }

    private void OnMouseLeft(object? sender, EventArgs e)
    {
        ResetCursor();
    }


    private void ResetCursor()
    {
        if (_cursorSet)
        {
            Mouse.SetCursor(Microsoft.Xna.Framework.Input.MouseCursor.Arrow);
            _cursorSet = false;
        }
    }

    public override void OnVisibleChanged()
    {
        base.OnVisibleChanged();
        if (!Visible)
        {
            ResetCursor();
        }
    }

    public override void InternalRender(RenderContext context)
    {
        // if widget is not placed, reset cursor
        if (!IsPlaced || !Enabled)
        {
            ResetCursor();
        }
        base.InternalRender(context);
    }
}
