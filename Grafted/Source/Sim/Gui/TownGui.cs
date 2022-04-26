using Grafted.Definitions;
using Grafted.Sim.Gui.MiscWidgets;
using Grafted.Sim.Gui.TownWidgets;
using Microsoft.Xna.Framework;
using Myra.Graphics2D;
using Myra.Graphics2D.TextureAtlases;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.Styles;
using HorizontalAlignment = Myra.Graphics2D.UI.HorizontalAlignment;
using Label = Myra.Graphics2D.UI.Label;

namespace Grafted.Sim.Gui;

public class TownGui : BaseGui {
    private readonly TabPanel _tabs;
    private readonly GameStatsPanel _gameStatsPanel;

    public TownGui(Town town) {
        _gameStatsPanel = new GameStatsPanel { HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 5, 0, 0) };
        _tabs = new TabPanel() {
            ButtonStyle = BaseContent.Styles.Button.Large,
            HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 30, 0, 0), Width = 1600
        };
        _tabs.AddTab("Home", new HomePanel(Core.Sim.World.PlayerPawns[0], town));
        _tabs.AddTab("Merchant", new MerchantPanel(Core.Sim.World.PlayerPawns[0], town));
        //_tabs.AddTab("Alchemist", new Label { Text = "Coming some day..." });
        //_tabs.AddTab("Forgemaster", new Label { Text = "Coming some day..." });
        //_tabs.AddTab("Skinworker", new Label { Text = "Coming some day..." });
        _tabs.AddTab("Adventure", AdventurePanel());

        Desktop = new Desktop {
            Root = new VerticalStackPanel {
                Widgets = {
                    _gameStatsPanel,
                    _tabs
                }
            },
            HasExternalTextInput = true
        };
    }

    public override void Update(float deltaTime) {
        _tabs.Update();
        _gameStatsPanel.Update();
        base.Update(deltaTime);
    }

    private Grid AdventurePanel() {
        var button = new TextButton(BaseContent.Styles.Button.Normal) {
            Text = "The Outskirts", HorizontalAlignment = HorizontalAlignment.Stretch
        };
        button.Click += (_, _) => {
            Core.Sim.World.MoveToZone(Defs.Zones.TheOutskirts);
            Core.Sim.Gui = new CombatGui(Core.Sim.World.NextCombat());
        };
        Grid grid = new() {
            ShowGridLines = false, HorizontalAlignment = HorizontalAlignment.Center,
            GridLinesColor = Color.Red,
            RowSpacing = 20, ColumnSpacing = 10,
            DefaultRowProportion = Proportion.Auto, DefaultColumnProportion = Proportion.Auto,
            Widgets = {
                new VerticalStackPanel {
                    Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.DeepGold],
                    Padding = new Thickness(15),
                    GridColumnSpan = 2,
                    Widgets = {
                        new Label(BaseContent.Styles.Label.Large) { Text = "Village of the Damned", HorizontalAlignment = HorizontalAlignment.Center }
                    }
                },
                new VerticalStackPanel() {
                    Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrame],
                    Padding = new Thickness(8),
                    GridRow = 1, GridColumn = 0,
                    Widgets = {
                        new Image { Background = new TextureRegion(BaseContent.Textures.Village), Width = 1200, Height = 800 }
                    }
                },

                new VerticalStackPanel {
                    Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrame],
                    GridRow = 1, GridColumn = 1,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    Spacing = 10,
                    Padding = new Thickness(20),
                    Widgets = {
                        new Label(BaseContent.Styles.Label.Large) { Text = "Zones (In Town)" },
                        new HorizontalSeparator(),
                        new TextButton(BaseContent.Styles.Button.Normal) { Text = "The Mill", HorizontalAlignment = HorizontalAlignment.Stretch, Enabled = false },
                        new TextButton(BaseContent.Styles.Button.Normal) { Text = "Fallow Field", HorizontalAlignment = HorizontalAlignment.Stretch, Enabled = false },
                        new TextButton(BaseContent.Styles.Button.Normal) { Text = "Vegetable Field", HorizontalAlignment = HorizontalAlignment.Stretch, Enabled = false },
                        new TextButton(BaseContent.Styles.Button.Normal) { Text = "Blood Court", HorizontalAlignment = HorizontalAlignment.Stretch, Enabled = false },
                        new TextButton(BaseContent.Styles.Button.Normal) { Text = "Rectory", HorizontalAlignment = HorizontalAlignment.Stretch, Enabled = false },
                        new TextButton(BaseContent.Styles.Button.Normal) { Text = "The Chapel", HorizontalAlignment = HorizontalAlignment.Stretch, Enabled = false },
                        new Label(BaseContent.Styles.Label.Large) { Text = "Zones (Combat)", Margin = new Thickness(0, 20, 0, 0) },
                        new HorizontalSeparator(),
                        button,
                        new TextButton(BaseContent.Styles.Button.Normal) { Text = "Festerpus Swamp", HorizontalAlignment = HorizontalAlignment.Stretch, Enabled = false },
                    }
                }
            }
        };
        return grid;
    }
}