using System.Linq;
using Grafted.Maths;
using Grafted.Sim.Gui.MiscWidgets;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.Styles;

namespace Grafted.Sim.Gui.TownWidgets;

public class TownSummaryPanel : HorizontalStackPanel, IUpdatable {
    public TownSummaryPanel() {
        Spacing = 50;
        VerticalStackPanel statsPanel = new() {
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.DeepGold],
            Padding = new Thickness(20),
            Width = 400, Height = 700,
            Spacing = 10
        };
        statsPanel.AddChild(new Label(BaseContent.Styles.Label.Large) { Text = $"Total Kills: \\c[{UiTextColor.TextColorGreen}]{Core.Sim.World.TotalKills}" });
        foreach ((ZoneDef? zoneDef, Zone? zone) in Core.Sim.World.Zones.Where(z => z.Key.ZoneType == ZoneType.Adventure)) {
            string distanceText = $"Furthest Distance: \\c[{UiTextColor.TextColorGreen}]{Mathf.RoundToNearest(zone.FurthestDistanceTraveled, 0.1f)} " +
                                  $"\\c[{UiTextColor.TextColorDefault}]({Mathf.RoundToInt(zone.FurthestDistanceTraveled / zoneDef.TravelSize * 100)}%)";
            statsPanel.AddChild(new Label(BaseContent.Styles.Label.Large) { Text = zone.Label });
            statsPanel.AddChild(new HorizontalStackPanel {
                Margin = new Thickness(20, 0, 0, 0),
                Spacing = 15, Widgets = {
                    new Image { Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Icon.ArrowNeutral], Width = 16, Height = 10, VerticalAlignment = VerticalAlignment.Center },
                    new Label(BaseContent.Styles.Label.Medium) { Text = $"Total Kills: \\c[{UiTextColor.TextColorGreen}]{zone.TotalZoneKills}", VerticalAlignment = VerticalAlignment.Center }
                }
            });
            statsPanel.AddChild(new HorizontalStackPanel {
                Margin = new Thickness(20, 0, 0, 0),
                Spacing = 15, Widgets = {
                    new Image { Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Icon.ArrowNeutral], Width = 16, Height = 10, VerticalAlignment = VerticalAlignment.Center },
                    new Label(BaseContent.Styles.Label.Medium) { Text = distanceText, VerticalAlignment = VerticalAlignment.Center }
                }
            });
        }

        AddChild(statsPanel);
        AddChild(new MessagePanel(Core.Sim.Messages) { Width = 500, Height = 700 });
    }


    public void Update() { }
}