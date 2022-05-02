using System.Linq;
using Grafted.Sim.Gui.MiscWidgets;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.Styles;

namespace Grafted.Sim.Gui.TownWidgets.HouseWidgets;

public class HouseGeneralPanel : HorizontalStackPanel, IUpdatable {
    private readonly FirewoodPanel _firewoodPanel;
    private readonly TendFirePanel _tendFirePanel;
    private readonly RestPanel _restPanel;
    private readonly FoodPanel _foodPanel;

    public HouseGeneralPanel(Town town) {
        Spacing = 20;
        VerticalStackPanel statsPanel = new() {
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrame],
            Padding = new Thickness(20),
            Width = 400, Height = 700,
            Spacing = 10,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        GameStatsPanel(statsPanel);

        _firewoodPanel = new FirewoodPanel(town);
        _tendFirePanel = new TendFirePanel(town.GetStructure<TownStructureHouse>()!);
        _foodPanel = new FoodPanel(town.GetStructure<TownStructureHouse>()!);
        _restPanel = new RestPanel(town.GetStructure<TownStructureHouse>()!);
        AddChild(new VerticalStackPanel {
            Spacing = 20,
            Widgets = {
                _restPanel,
                _firewoodPanel,
                _tendFirePanel,
            },

        });
        AddChild(new VerticalStackPanel {
            Spacing = 20,
            Widgets = {
                _foodPanel
            },

        });
        AddChild(statsPanel);
        AddChild(new MessagePanel(Core.Sim.Messages) { Width = 400, Height = 700 });
    }

    public void Update() {
        _firewoodPanel.Update();
        _tendFirePanel.Update();
        _restPanel.Update();
        _foodPanel.Update();
    }

    private static void GameStatsPanel(VerticalStackPanel statsPanel) {
        statsPanel.AddChild(new Label(BaseContent.Styles.Label.Large) { Text = $"Total Kills: \\c[{UiTextColor.TextColorGreen}]{Core.Sim.World.TotalKills}" });
        foreach ((ZoneDef? zoneDef, Zone? zone) in Core.Sim.World.Zones.Where(z => z.Key.ZoneType == ZoneType.Adventure)) {
            string distanceText = $"Furthest Distance: \\c[{UiTextColor.TextColorGreen}]{zone.FurthestDistanceTraveled.ToString("0.00")} " +
                                  $"\\c[{UiTextColor.TextColorDefault}]({zone.FurthestDistanceTraveled / zoneDef.TravelSize:P})";
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
    }
}