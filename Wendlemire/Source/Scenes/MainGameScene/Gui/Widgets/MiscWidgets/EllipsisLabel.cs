namespace Wendlemire.Scenes.MainGameScene.Gui.Widgets.MiscWidgets;

public sealed class EllipsisLabel : Label
{
    private const float Interval = 0.4f;

    private string _baseText = "";
    private float _elapsed;
    private int _dots = 1;

    public EllipsisLabel()
    {
    }

    public EllipsisLabel(string styleName) : base(styleName)
    {
    }

    public string BaseText
    {
        get => _baseText;
        set
        {
            _baseText = value ?? "";
            _dots = 1;
            _elapsed = 0;
            RefreshText();
        }
    }

    public void Update(float deltaTime)
    {
        if (!Visible)
        {
            return;
        }

        _elapsed += deltaTime;
        if (_elapsed < Interval)
        {
            return;
        }

        _elapsed = 0;
        _dots = _dots % 3 + 1;
        RefreshText();
    }

    private void RefreshText()
    {
        Text = _baseText + new string('.', _dots);
    }
}
