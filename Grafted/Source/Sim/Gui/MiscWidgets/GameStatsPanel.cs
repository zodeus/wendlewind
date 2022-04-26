using Grafted.Maths;
using Grafted.Sim.Combat;
using Myra.Graphics2D.UI;

namespace Grafted.Sim.Gui.MiscWidgets;

public class GameStatsPanel : HorizontalStackPanel {
    private Label _zoneLabel;
    private Label _zoneKillsLabel;
    private Label _distanceLabel;

    public GameStatsPanel() {
        Spacing = 10;
        _zoneLabel = new Label(BaseContent.Styles.Label.Large);
        _zoneKillsLabel = new Label(BaseContent.Styles.Label.Large);
        _distanceLabel = new Label(BaseContent.Styles.Label.Large);
        AddChild(_zoneLabel);
        //todo this is dirty business
        if (Core.Sim.World.CurrentZone.Def.ZoneType == ZoneType.Adventure) {
            AddChild(new VerticalSeparator());
            AddChild(_distanceLabel);
            AddChild(new VerticalSeparator());
            AddChild(_zoneKillsLabel);
        }

        AddChild(new VerticalSeparator());
        AddChild(new Label(BaseContent.Styles.Label.Large) { Text = "7:15 pm" });
        AddChild(new VerticalSeparator());
        AddChild(new Label(BaseContent.Styles.Label.Large) { Text = "Day 3" });
    }

    public void Update() {
        _zoneLabel.Text = $"{Core.Sim.World.CurrentZone.Def.Label}";
        _distanceLabel.Text = $"Traveled: {Mathf.RoundToNearest(Core.Sim.World.CurrentZone.DistanceTraveled, 0.1f)}km ({Mathf.RoundToInt(Core.Sim.World.CurrentZone.PercentTraveled * 100)}%)";
        _zoneKillsLabel.Text = $"Zone Kills: \\c[{UiTextColor.TextColorGreen}]{Core.Sim.World.CurrentZone.ZoneKills}";
    }
}