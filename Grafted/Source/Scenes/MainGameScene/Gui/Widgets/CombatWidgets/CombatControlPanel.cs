namespace Grafted.Scenes.MainGameScene.Gui.Widgets.CombatWidgets;

public class CombatControlPanel : VerticalStackPanel
{
    private readonly Encounter _encounter;
    private readonly TextButton _continueButton;
    private readonly HorizontalStackPanel _speedButtons;
    private readonly TextButton _retreatButton;

    public CombatControlPanel(Encounter encounter)
    {
        _encounter = encounter;
        ShowGridLines = false;
        _continueButton = new TextButton(BaseContent.Styles.Button.Normal)
        {
            Text = "Continue", Visible = false, Margin = new Thickness(0, 10, 0, 0), HorizontalAlignment = HorizontalAlignment.Stretch
        };
        _continueButton.Click += (_, _) => { encounter.Zone!.CombatResults(); };

        AddChild(_continueButton);

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

        AddChild(_speedButtons);

        _retreatButton = new TextButton(BaseContent.Styles.Button.Normal)
        {
            Text = $"\\c[{TC.Golden}]Attempt Escape", Margin = new Thickness(0, 10, 0, 0), HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        _retreatButton.Click += (_, _) => { encounter.ShouldAttemptRetreat = true; };
        _retreatButton.Visible = Core.Context.Player.HasTrinket(Defs.Items.CowardsFlag);
        AddChild(_retreatButton);
    }

    private TextButton CreateSpeedButton(string label, float speed)
    {
        var button = new TextButton("small")
        {
            Text = label
        };

        button.Click += (_, _) =>
        {
            Core.PauseCoroutines = false;
            //Core.Context.CombatSettings.Speed = speed;
        };
        return button;
    }

    public void ShowContinueButton()
    {
        _speedButtons.RemoveFromParent();
        _continueButton.Visible = true;
    }

    public void Update()
    {
        _retreatButton.Enabled = !_encounter.ShouldAttemptRetreat;
    }
}