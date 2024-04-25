using Grafted.Scenes.MainGameScene.Gui.CombatGui;
using Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets;

namespace Grafted.Scenes.MainGameScene.Gui;

public class CampGui : BaseGui
{
    private readonly World _world;
    private readonly TabPanel _tabs;
    private readonly GameHud _gameHud;
    private readonly PawnBodyEffectsWindow _pawnBodyEffectsWindow;
    private ZoneStartWindow? _zoneBeginWindow;

    public CampGui(World world)
    {
        _world = world;
        _gameHud = new GameHud(world.Player) { HorizontalAlignment = HorizontalAlignment.Stretch, Margin = new Thickness(0, 5, 0, 0) };
        _tabs = new TabPanel
        {
            ButtonStyle = BaseContent.Styles.Button.Large,
            HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 30, 0, 0), Width = 3600
        };
        _tabs.AddTab("Camp", new CampOverviewPanel(this, world.PlayerPawn));
        _tabs.AddTab("Zones", ZonePanel());

        Desktop = new Desktop
        {
            Root = new VerticalStackPanel
            {
                Widgets =
                {
                    _gameHud,
                    _tabs
                }
            },
            HasExternalTextInput = true
        };

        _pawnBodyEffectsWindow = new PawnBodyEffectsWindow(world.PlayerPawn);
        _pawnBodyEffectsWindow.Show(Desktop, new Point(50, 20));
    }

    public override void Update(float deltaTime)
    {
        _tabs.Update();
        _gameHud.Update();
        _zoneBeginWindow?.Update();
        _pawnBodyEffectsWindow.Update();
        base.Update(deltaTime);
    }

    private Grid ZonePanel()
    {
        var peacefulMeadow = new TextButton(BaseContent.Styles.Button.Normal)
        {
            Text = Defs.Zones.PeacefulMeadow.Label, HorizontalAlignment = HorizontalAlignment.Stretch,
            Enabled = !_world.GetZone(Defs.Zones.PeacefulMeadow).IsComplete
        };
        peacefulMeadow.Click += (_, _) =>
        {
            _zoneBeginWindow = new ZoneStartWindow(Defs.Zones.PeacefulMeadow);
            _zoneBeginWindow.ShowModal(Desktop, (Screen.Center - new Vector2(200, 300)).ToPoint());
        };

        var outskirts = new TextButton(BaseContent.Styles.Button.Normal)
        {
            Text = Defs.Zones.TheOutskirts.Label, HorizontalAlignment = HorizontalAlignment.Stretch,
            Enabled = _world.GetZone(Defs.Zones.PeacefulMeadow).IsComplete && !_world.GetZone(Defs.Zones.TheOutskirts).IsComplete
        };
        outskirts.Click += (_, _) =>
        {
            _zoneBeginWindow = new ZoneStartWindow(Defs.Zones.TheOutskirts);
            _zoneBeginWindow.ShowModal(Desktop, (Screen.Center - new Vector2(200, 300)).ToPoint());
        };

        var grainMill = new TextButton(BaseContent.Styles.Button.Normal)
        {
            Text = Defs.Zones.GrainMill.Label, HorizontalAlignment = HorizontalAlignment.Stretch,
            Enabled = _world.GetZone(Defs.Zones.TheOutskirts).IsComplete && !_world.GetZone(Defs.Zones.GrainMill).IsComplete
        };
        grainMill.Click += (_, _) =>
        {
            _zoneBeginWindow = new ZoneStartWindow(Defs.Zones.GrainMill);
            _zoneBeginWindow.ShowModal(Desktop, (Screen.Center - new Vector2(200, 300)).ToPoint());
        };

        var festerpusSwamp = new TextButton(BaseContent.Styles.Button.Normal)
        {
            Text = Defs.Zones.FesterpusSwamp.Label, HorizontalAlignment = HorizontalAlignment.Stretch,
            Enabled = _world.GetZone(Defs.Zones.GrainMill).IsComplete && !_world.GetZone(Defs.Zones.FesterpusSwamp).IsComplete
        };
        festerpusSwamp.Click += (_, _) =>
        {
            _zoneBeginWindow = new ZoneStartWindow(Defs.Zones.FesterpusSwamp);
            _zoneBeginWindow.ShowModal(Desktop, (Screen.Center - new Vector2(200, 300)).ToPoint());
        };

        Grid grid = new()
        {
            ShowGridLines = false, HorizontalAlignment = HorizontalAlignment.Center,
            GridLinesColor = Color.Red,
            RowSpacing = 20, ColumnSpacing = 10,
            DefaultRowProportion = Proportion.Auto, DefaultColumnProportion = Proportion.Auto,
            Widgets =
            {
                new VerticalStackPanel
                {
                    Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.DeepGold],
                    Padding = new Thickness(15),
                    GridColumnSpan = 2,
                    Widgets =
                    {
                        new Label(BaseContent.Styles.Label.Large) { Text = "Village of the Damned", HorizontalAlignment = HorizontalAlignment.Center }
                    }
                },
                new VerticalStackPanel
                {
                    Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrame],
                    Padding = new Thickness(8),
                    GridRow = 1, GridColumn = 0,
                    Widgets =
                    {
                        new Grid { Background = new TextureRegion(BaseContent.Textures.Village), Width = 1200, Height = 800 }
                    }
                },

                new VerticalStackPanel
                {
                    Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrame],
                    GridRow = 1, GridColumn = 1,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    Spacing = 10,
                    Padding = new Thickness(20),
                    Widgets =
                    {
                        peacefulMeadow,
                        outskirts,
                        grainMill,
                        festerpusSwamp,
                        new TextButton(BaseContent.Styles.Button.Normal) { Text = "The Alchemist Hut", HorizontalAlignment = HorizontalAlignment.Stretch, Enabled = false },
                        new TextButton(BaseContent.Styles.Button.Normal) { Text = "Forgotten Forest", HorizontalAlignment = HorizontalAlignment.Stretch, Enabled = false },
                        new TextButton(BaseContent.Styles.Button.Normal) { Text = "Forgemaster Quarry", HorizontalAlignment = HorizontalAlignment.Stretch, Enabled = false },
                        new TextButton(BaseContent.Styles.Button.Normal) { Text = "Fallow Field", HorizontalAlignment = HorizontalAlignment.Stretch, Enabled = false },
                        new TextButton(BaseContent.Styles.Button.Normal) { Text = "Mage Tower", HorizontalAlignment = HorizontalAlignment.Stretch, Enabled = false },
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

    public override void Dispose()
    {
    }
}