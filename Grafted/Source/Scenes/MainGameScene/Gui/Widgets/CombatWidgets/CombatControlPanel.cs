namespace Grafted.Scenes.MainGameScene.Gui.Widgets.CombatWidgets;

public sealed class CombatControlPanel : VerticalStackPanel
{
    private readonly HorizontalStackPanel _speedButtonsPanel;
    private readonly List<(Button button, Label label)> _speedButtons = [];

    public CombatControlPanel(Encounter encounter)
    {
        var pauseLabel = new Label("small") { Text = "||" };
        var pauseButton = new Button("small")
        {
            Content = pauseLabel
        };

        pauseButton.Click += (_, _) => { Core.Context.TogglePause(); };
        _speedButtonsPanel = new HorizontalStackPanel
        {
            Spacing = 3,
            Margin = new Thickness(0, 20, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Center,
            Widgets =
            {
                pauseButton,
                CreateSpeedButton("1x", 1),
                CreateSpeedButton("2x", 2),
                CreateSpeedButton("4x", 4)
            }
        };

        Widgets.Add(_speedButtonsPanel);
    }

    private Button CreateSpeedButton(string label, int speed)
    {
        var buttonLabel = new Label("small") { Text = label, TextColor = Color.LightGray };
        var button = new Button("small")
        {
            Content = buttonLabel
        };
        if (DebugSettings.CombatSpeed == speed)
        {
            buttonLabel.TextColor = Color.Goldenrod;
        }

        button.Click += (_, _) =>
        {
            _speedButtons.ForEach(b => b.label.TextColor = Color.LightGray);
            buttonLabel.TextColor = Color.Goldenrod;
            DebugSettings.CombatSpeed = speed;
        };
        _speedButtons.Add((button, buttonLabel));
        return button;
    }
}