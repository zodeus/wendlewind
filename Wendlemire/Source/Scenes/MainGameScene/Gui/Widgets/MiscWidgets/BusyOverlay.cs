namespace Wendlemire.Scenes.MainGameScene.Gui.Widgets.MiscWidgets;

public sealed class BusyOverlay : Panel
{
    private static readonly Color Veil = new(7, 5, 4, 180);
    private static readonly Color Bone = new(203, 184, 150);

    private readonly EllipsisLabel _label;
    private Window? _modal;

    public BusyOverlay()
    {
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;
        Background = new SolidBrush(Veil);
        Visible = false;
        _label = new EllipsisLabel(BaseContent.Styles.Label.Huge)
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            TextColor = Bone
        };
        Widgets.Add(_label);
        TouchDown += (_, _) => { };
    }

    public void Show(string message)
    {
        _label.BaseText = StripDots(message);
        Visible = true;
    }

    public void ShowModal(Desktop desktop, string message)
    {
        Show(message);
        if (_modal is { IsPlaced: true })
        {
            return;
        }

        RemoveFromParent();
        _modal = new Window
        {
            Title = "",
            Content = this,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(0),
            Background = new SolidBrush(Color.Transparent)
        };
        _modal.TitlePanel.Visible = false;
        _modal.Closed += (_, _) => _modal = null;
        _modal.ShowModal(desktop);
        _modal.Left = 0;
        _modal.Top = 0;
        _modal.Width = Core.ReferenceResolution.X;
        _modal.Height = Core.ReferenceResolution.Y;
    }

    public void Hide()
    {
        Visible = false;
        if (_modal == null)
        {
            return;
        }

        var modal = _modal;
        _modal = null;
        modal.Close();
    }

    public void Update(float deltaTime)
    {
        if (Visible)
        {
            _label.Update(deltaTime);
        }
    }

    private static string StripDots(string message)
    {
        return string.IsNullOrWhiteSpace(message) ? "" : message.Trim().TrimEnd('.');
    }
}
