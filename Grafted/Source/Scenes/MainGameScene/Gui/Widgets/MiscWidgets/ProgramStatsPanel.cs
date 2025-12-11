namespace Grafted.Scenes.MainGameScene.Gui.Widgets.MiscWidgets;

public sealed class ProgramStatsPanel : VerticalStackPanel {
    private readonly Label _fps;
    private readonly Label _ticks;
    private readonly Label _frameTime;

    public ProgramStatsPanel()
    {
        Width = 120;
        Spacing = 3;
        _fps = new Label(BaseContent.Styles.Label.Small);
        Widgets.Add(_fps);
        _ticks = new Label(BaseContent.Styles.Label.Small);
        Widgets.Add(_ticks);
        _frameTime = new Label(BaseContent.Styles.Label.Small);
        Widgets.Add(_frameTime);
    }

    public void Update() {
        _fps.Text = $"FPS: {(int) Core.FrameCounter.AverageFramesPerSecond}";
        _ticks.Text = $"Ticks: {Core.Context.Ticks}";
        _frameTime.Text = $"FT: {Core.FrameCounter.AverageFrameTime}";
    }
}