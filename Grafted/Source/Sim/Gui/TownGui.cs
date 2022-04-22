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

    public TownGui(Town town) {
        _tabs = new TabPanel() { HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 30, 0, 0), Width = 1200 };
        _tabs.AddTab("Storage", new TownStoragePanel(town, Core.Sim.World.PlayerPawns[0]));
        _tabs.AddTab("Merchant", new Label(BaseContent.Styles.Label.Large) { Text = "Coming soon!" });
        _tabs.AddTab("Mend", new Label(BaseContent.Styles.Label.Large) { Text = "Coming soon!" });
        _tabs.AddTab("Build & Repair", new Image { Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Icon.Boak], Width = 64, Height = 64 });
        _tabs.AddTab("Travel", TravelPanel());

        Desktop = new Desktop { Root = _tabs, HasExternalTextInput = true };
    }

    public override void Update(float deltaTime) {
        _tabs.Update();
        base.Update(deltaTime);
    }


    private Grid TravelPanel() {
        var button = new TextButton(BaseContent.Styles.Button.Small) { Text = "The Outskirts", HorizontalAlignment = HorizontalAlignment.Stretch };
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
                        new Image { Background = new TextureRegion(BaseContent.Textures.Village), Width = 900, Height = 600 }
                    }
                },

                new VerticalStackPanel {
                    Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrame],
                    GridRow = 1, GridColumn = 1,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    Spacing = 10,
                    Padding = new Thickness(20),
                    Widgets = {
                        new Label(BaseContent.Styles.Label.Medium) { Text = "Zones (In Town)" },
                        new HorizontalSeparator(),
                        new TextButton(BaseContent.Styles.Button.Small) { Text = "The Mill", HorizontalAlignment = HorizontalAlignment.Stretch, Enabled = false },
                        new TextButton(BaseContent.Styles.Button.Small) { Text = "Fallow Field", HorizontalAlignment = HorizontalAlignment.Stretch, Enabled = false },
                        new TextButton(BaseContent.Styles.Button.Small) { Text = "Vegetable Field", HorizontalAlignment = HorizontalAlignment.Stretch, Enabled = false },
                        new TextButton(BaseContent.Styles.Button.Small) { Text = "Blood Court", HorizontalAlignment = HorizontalAlignment.Stretch, Enabled = false },
                        new TextButton(BaseContent.Styles.Button.Small) { Text = "Rectory", HorizontalAlignment = HorizontalAlignment.Stretch, Enabled = false },
                        new TextButton(BaseContent.Styles.Button.Small) { Text = "The Chapel", HorizontalAlignment = HorizontalAlignment.Stretch, Enabled = false },
                        new Label(BaseContent.Styles.Label.Medium) { Text = "Zones (Combat)", Margin = new Thickness(0, 20, 0, 0) },
                        new HorizontalSeparator(),
                        button,
                        new TextButton(BaseContent.Styles.Button.Small) { Text = "Festerpus Swamp", HorizontalAlignment = HorizontalAlignment.Stretch, Enabled = false },
                    }
                }
            }
        };
        return grid;
    }
}