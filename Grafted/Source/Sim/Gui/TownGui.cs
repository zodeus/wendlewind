using System.Collections;
using Grafted.Definitions;
using Grafted.Sim.Gui.EntityWidgets.PawnWidgets;
using Grafted.Sim.Gui.MiscWidgets;
using Grafted.Sim.Gui.TownWidgets;
using Grafted.Sim.Gui.TownWidgets.HouseWidgets;
using Grafted.Utils;
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
    private readonly GameHud _gameHud;
    private readonly ZoneBeginWindow _zoneBeginWindow;
    private readonly PawnBodyEffectsWindow _pawnBodyEffectsWindow;

    public TownGui(Town town) {
        _gameHud = new GameHud { HorizontalAlignment = HorizontalAlignment.Stretch, Margin = new Thickness(0, 5, 0, 0) };
        _zoneBeginWindow = new ZoneBeginWindow(Defs.Zones.PeacefulMeadow);
        _tabs = new TabPanel {
            ButtonStyle = BaseContent.Styles.Button.Large,
            HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 30, 0, 0), Width = 1800
        };
        _tabs.AddTab("House", new HousePanel(town));
        _tabs.AddTab("Character", new PawnDetailPanel(Core.Sim.World.PlayerPawn, "Storage", town.GetStructure<TownStructureHouse>()!.Storage));
        _tabs.AddTab("Merchant", new MerchantPanel(Core.Sim.World.PlayerPawn, town));
        _tabs.AddTab("Adventure", AdventurePanel());

        Desktop = new Desktop {
            Root = new VerticalStackPanel {
                Widgets = {
                    _gameHud,
                    _tabs
                }
            },
            HasExternalTextInput = true
        };

        _pawnBodyEffectsWindow = new PawnBodyEffectsWindow(Core.Sim.World.PlayerPawn);
        _pawnBodyEffectsWindow.Show(Desktop, new Point(50, 20));
    }

    public override void Update(float deltaTime) {
        _tabs.Update();
        _gameHud.Update();
        _zoneBeginWindow.Update();
        _pawnBodyEffectsWindow.Update();
        base.Update(deltaTime);
    }

    private Grid AdventurePanel() {
        var button1 = new TextButton(BaseContent.Styles.Button.Normal) {
            Text = Defs.Zones.TheOutskirts.Label, HorizontalAlignment = HorizontalAlignment.Stretch
        };
        button1.Click += (_, _) => {
            Core.Sim.World.MoveToZone(Defs.Zones.TheOutskirts);
            Core.Sim.World.DoZoneTravel();
            Core.Sim.ActivateCombatEvent(Core.Sim.World.NextCombat());
        };

        var button2 = new TextButton(BaseContent.Styles.Button.Normal) {
            Text = Defs.Zones.PeacefulMeadow.Label, HorizontalAlignment = HorizontalAlignment.Stretch
        };
        button2.Click += (_, _) => {
            _zoneBeginWindow.ShowModal(Desktop, (Screen.Center - new Vector2(200, 300)).ToPoint());
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
                new VerticalStackPanel {
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
                        button2,
                        button1,
                        new TextButton(BaseContent.Styles.Button.Normal) { Text = "The Grain Mill", HorizontalAlignment = HorizontalAlignment.Stretch, Enabled = false },
                        new TextButton(BaseContent.Styles.Button.Normal) { Text = "Festerpus Swamp", HorizontalAlignment = HorizontalAlignment.Stretch, Enabled = false },
                        new TextButton(BaseContent.Styles.Button.Normal) { Text = "The Alchemist Hut", HorizontalAlignment = HorizontalAlignment.Stretch, Enabled = false },
                        new TextButton(BaseContent.Styles.Button.Normal) { Text = "Forgotten Forest", HorizontalAlignment = HorizontalAlignment.Stretch, Enabled = false },
                        new TextButton(BaseContent.Styles.Button.Normal) { Text = "Forgemaster Quarry", HorizontalAlignment = HorizontalAlignment.Stretch, Enabled = false },
                        new TextButton(BaseContent.Styles.Button.Normal) { Text = "Temple of the Skinworkers", HorizontalAlignment = HorizontalAlignment.Stretch, Enabled = false },
                        new TextButton(BaseContent.Styles.Button.Normal) { Text = "Fallow Field", HorizontalAlignment = HorizontalAlignment.Stretch, Enabled = false },
                        new TextButton(BaseContent.Styles.Button.Normal) { Text = "Field of Vegetables", HorizontalAlignment = HorizontalAlignment.Stretch, Enabled = false },
                        new TextButton(BaseContent.Styles.Button.Normal) { Text = "Blood Court", HorizontalAlignment = HorizontalAlignment.Stretch, Enabled = false },
                        new TextButton(BaseContent.Styles.Button.Normal) { Text = "His Rectory", HorizontalAlignment = HorizontalAlignment.Stretch, Enabled = false },
                        new TextButton(BaseContent.Styles.Button.Normal) { Text = "Scarlet Chapel", HorizontalAlignment = HorizontalAlignment.Stretch, Enabled = false },
                        new TextButton(BaseContent.Styles.Button.Normal) { Text = "Steamy Oil Vents", HorizontalAlignment = HorizontalAlignment.Stretch, Enabled = false },
                    }
                }
            }
        };
        return grid;
    }
}