namespace Grafted.Scenes.MainGameScene.Gui.Widgets.CombatWidgets;

public sealed class CombatControlPanel : VerticalStackPanel
{
    private readonly Encounter _encounter;
    private readonly Button _continueButton;
    private readonly HorizontalStackPanel _speedButtons;

    public CombatControlPanel(Encounter encounter)
    {
        _encounter = encounter;
        ShowGridLines = false;
        _continueButton = new Button(BaseContent.Styles.Button.LargeGold)
        {
            Content = new Label { Text = "Continue" },
            Visible = false, Margin = new Thickness(0, 10, 0, 0), HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        _continueButton.Click += (_, _) => { encounter.Zone!.CombatResults(); };

        Widgets.Add(_continueButton);

        TextButton pauseButton = new("small")
        {
            Text = "||"
        };

        pauseButton.Click += (_, _) => { Core.Context.TogglePause(); };
        _speedButtons = new HorizontalStackPanel
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

        Widgets.Add(_speedButtons);
    }

    private TextButton CreateSpeedButton(string label, int speed)
    {
        var button = new TextButton("small")
        {
            Text = label
        };

        button.Click += (_, _) => { DebugSettings.CombatSpeed = speed; };
        return button;
    }

    public void ShowContinueButton()
    {
        _speedButtons.RemoveFromParent();
        _continueButton.Visible = true;
    }
}