using Myra.Graphics2D.UI;

namespace Grafted.Sim.Gui.Widgets.MiscWidgets;

public class ProgramStatsPanel : HorizontalStackPanel {
    private readonly Label _fps;
    private readonly Label _frameTime;

    public ProgramStatsPanel() {
        Spacing = 10;
        _fps = new Label { Width = 200 };
        AddChild(_fps);
        _frameTime = new Label { Width = 200 };
        AddChild(_frameTime);
    }

    public void Update() {
        _fps.Text = $"FPS: {(int) Core.FrameCounter.AverageFramesPerSecond}";
        _frameTime.Text = $"FT: {Core.FrameCounter.AverageFrameTime}";
    }
}