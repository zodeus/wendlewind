using Grafted.Sim.Combat;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;

namespace Grafted.Sim.Gui.CombatGuis;

public class CombatControlPanel : VerticalStackPanel {
    private readonly TextButton _continueButton;
    private readonly HorizontalStackPanel _speedButtons;

    public CombatControlPanel(CombatEvent combatEvent) {
        //ShowGridLines = true;
        _continueButton = new TextButton(BaseContent.Styles.Button.Normal) {
            Text = "Continue", Visible = false, Margin = new Thickness(0, 10, 0, 0), HorizontalAlignment = HorizontalAlignment.Stretch
        };
        _continueButton.Click += (_, _) => {
            Core.Sim.Gui = new CombatResultsGui(combatEvent);
        };

        AddChild(_continueButton);

        var pauseButton = new TextButton("small") {
            Text = "||"
        };

        pauseButton.Click += (_, _) => {
            Core.Sim.TogglePause();
        };
        _speedButtons = new HorizontalStackPanel {
            Spacing = 3,
            Margin = new Thickness(0, 20, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Center,
            Widgets = {
                pauseButton,
                CreateSpeedButton("1x", .2f),
                CreateSpeedButton("2x", .1f),
                CreateSpeedButton("4x", .01f)
            }
        };

        AddChild(_speedButtons);
    }

    private TextButton CreateSpeedButton(string label, float speed) {
        var button = new TextButton("small") {
            Text = label
        };

        button.Click += (_, _) => {
            Core.PauseCoroutines = false;
            Core.Sim.GameSpeed = speed;
        };
        return button;
    }

    public void ShowContinueButton() {
        _speedButtons.RemoveFromParent();
        _continueButton.Visible = true;
    }
}