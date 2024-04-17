using Grafted.Definitions;
using Grafted.Sim.Combat;
using Grafted.Sim.Zones.Handlers;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;

namespace Grafted.Sim.Gui.Widgets.CombatWidgets;

public class CombatControlPanel : VerticalStackPanel {
    private readonly CombatEvent _combatEvent;
    private readonly TextButton _continueButton;
    private readonly HorizontalStackPanel _speedButtons;
    private readonly TextButton _retreatButton;

    public CombatControlPanel(CombatEvent combatEvent) {
        _combatEvent = combatEvent;
        ShowGridLines = false;
        _continueButton = new TextButton(BaseContent.Styles.Button.Normal) {
            Text = "Continue", Visible = false, Margin = new Thickness(0, 10, 0, 0), HorizontalAlignment = HorizontalAlignment.Stretch
        };
        _continueButton.Click += (_, _) => {
            combatEvent.Zone!.Adventure!.State = AdventureState.CombatResults;
        };

        AddChild(_continueButton);

        TextButton pauseButton = new("small") {
            Text = "||"
        };

        pauseButton.Click += (_, _) => {
            Core.Sim.CombatSettings.TogglePause();
        };
        _speedButtons = new HorizontalStackPanel {
            Spacing = 3,
            Margin = new Thickness(0, 20, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Center,
            Widgets = {
                pauseButton,
                CreateSpeedButton("1x", CombatSpeed.Slow),
                CreateSpeedButton("2x", CombatSpeed.Normal),
                CreateSpeedButton("4x", CombatSpeed.Fast)
            }
        };

        AddChild(_speedButtons);

        _retreatButton = new TextButton(BaseContent.Styles.Button.Normal) {
            Text = $"\\c[{UiTextColor.TextColorGolden}]Attempt Escape", Margin = new Thickness(0, 10, 0, 0), HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        _retreatButton.Click += (_, _) => {
            combatEvent.ShouldAttemptRetreat = true;
        };
        _retreatButton.Visible = Core.Sim.Player.HasTrinket(Defs.Items.CowardsFlag);
        AddChild(_retreatButton);
    }

    private TextButton CreateSpeedButton(string label, float speed) {
        var button = new TextButton("small") {
            Text = label
        };

        button.Click += (_, _) => {
            Core.PauseCoroutines = false;
            Core.Sim.CombatSettings.Speed = speed;
        };
        return button;
    }

    public void ShowContinueButton() {
        _speedButtons.RemoveFromParent();
        _continueButton.Visible = true;
    }

    public void Update() {
        _retreatButton.Enabled = !_combatEvent.ShouldAttemptRetreat;
    }
}