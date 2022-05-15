using System.Linq;
using Grafted.Sim.Gui.Widgets.MiscWidgets;
using Grafted.Sim.Gui.Widgets.TownWidgets.HouseWidgets.FoodWidgets;
using Grafted.Sim.Zones;
using Grafted.Sim.Zones.Handlers;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.Styles;

namespace Grafted.Sim.Gui.Widgets.TownWidgets.HouseWidgets;

public class HousePanel : HorizontalStackPanel, IUpdatable {
    private readonly WoodPanel _woodPanel;
    private readonly TendFirePanel _tendFirePanel;
    private readonly RestPanel _restPanel;
    private readonly KitchenPanel _kitchenPanel;
    private readonly HouseUpgradesPanel _upgradesPanel;

    public HousePanel(Town town) {
        Spacing = 20;
        VerticalStackPanel statsPanel = new() {
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrame],
            Padding = new Thickness(20), Width = 400, Spacing = 10,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Stretch
        };
        GameStatsPanel(statsPanel);

        _woodPanel = new WoodPanel(town);
        _tendFirePanel = new TendFirePanel(town.GetStructure<TownStructureHouse>()!);
        _kitchenPanel = new KitchenPanel(town.GetStructure<TownStructureHouse>()!) {
            Width = 260, HorizontalAlignment = HorizontalAlignment.Center,
            Padding = new Thickness(20), VerticalAlignment = VerticalAlignment.Center
        };
        _upgradesPanel = new HouseUpgradesPanel(town.GetStructure<TownStructureHouse>()!) {
            VerticalAlignment = VerticalAlignment.Top
        };
        _restPanel = new RestPanel(town.GetStructure<TownStructureHouse>()!);
        AddChild(new VerticalStackPanel {
            Spacing = 20,
            VerticalAlignment = VerticalAlignment.Stretch,
            Widgets = {
                _restPanel,
                _woodPanel,
                _tendFirePanel
            },

        });
        AddChild(new VerticalStackPanel {
            //VerticalAlignment = VerticalAlignment.Stretch,
            Spacing = 10, Widgets = { _kitchenPanel }
        });
        /*AddChild(statsPanel);*/
        AddChild(_upgradesPanel);
        AddChild(new MessagePanel(Core.Sim.Messages) {
            Width = 400, Height = 800, VerticalAlignment = VerticalAlignment.Stretch
        });
    }

    public void Update() {
        _woodPanel.Update();
        _tendFirePanel.Update();
        _restPanel.Update();
        _kitchenPanel.Update();
        _upgradesPanel.Update();
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