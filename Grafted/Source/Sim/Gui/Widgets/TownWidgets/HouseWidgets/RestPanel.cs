using Myra.Graphics2D;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.Styles;

namespace Grafted.Sim.Gui.Widgets.TownWidgets.HouseWidgets;

public class RestPanel : VerticalStackPanel {
    private readonly TextButton _restButton;
    private readonly Label _restLabel;

    public RestPanel(TownStructureHouse house) {
        Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrame];
        Padding = new Thickness(20);
        Spacing = 15;

        _restLabel = new Label(BaseContent.Styles.Label.Large) { Height = 50 };
        _restButton = new TextButton(BaseContent.Styles.Button.Normal) { Text = "Rest" };
        _restButton.Click += (_, _) => {
            house.Rest();
        };
        AddChild(_restLabel);
        AddChild(_restButton);
    }

    public void Update() {
        bool isExhausted = Core.Sim.World.PlayerPawns[0].Body.Energy < .1;
        _restButton.Enabled = Core.Sim.World.Time.CurrentTime is > 2000 or < 0400 || isExhausted;
        if (isExhausted) {
            _restLabel.Text = $"\\c[{UiTextColor.TextColorRed}]You are exhausted, \nrest now";
        }
        else if (Core.Sim.World.Time.CurrentTime is > 2300 or < 0400) {
            _restLabel.Text = $"\\c[{UiTextColor.TextColorRed}]It's late, consider\nresting soon";
        }
        else if (Core.Sim.World.Time.CurrentTime is > 2000 or < 0400) {
            _restLabel.Text = "You may rest now";
        }
        else {
            _restLabel.Text = "It's daytime now";
        }
    }
}