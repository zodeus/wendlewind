namespace Grafted.Scenes.MainGameScene.Gui.Widgets.MiscWidgets;

public class ProgramStatsPanel : VerticalStackPanel {
    private readonly Label _fps;
    private readonly Label _ticks;
    private readonly Label _frameTime;

    public ProgramStatsPanel() {
        Spacing = 10;
        _fps = new Label { Width = 200 };
        AddChild(_fps);
        _ticks = new Label { Width = 200 };
        AddChild(_ticks);
        _frameTime = new Label { Width = 200 };
        AddChild(_frameTime);
    }

    public void Update() {
        _fps.Text = $"FPS: {(int) Core.FrameCounter.AverageFramesPerSecond}";
        _ticks.Text = $"Ticks: {(int) Core.Context.Ticks}";
        _frameTime.Text = $"FT: {Core.FrameCounter.AverageFrameTime}";
    }
}