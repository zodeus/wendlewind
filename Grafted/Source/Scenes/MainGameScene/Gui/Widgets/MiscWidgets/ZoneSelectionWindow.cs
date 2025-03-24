using Grafted.Scenes.MainGameScene.Gui.CombatGui;

namespace Grafted.Scenes.MainGameScene.Gui.Widgets.MiscWidgets;

public class ZoneSelectionWindow : Window
{
    private readonly VerticalStackPanel _zoneDisplay;

    public sealed override Widget Content
    {
        get => base.Content;
        set => base.Content = value;
    }

    public ZoneSelectionWindow(World world)
    {
        TitlePanel.Visible = false;

        var peacefulMeadow = world.GetZone(Defs.Biomes.PeacefulMeadow);
        var outskirts = world.GetZone(Defs.Biomes.TheOutskirts);
        var grainMill = world.GetZone(Defs.Biomes.GrainMill);
        var festerpusSwamp = world.GetZone(Defs.Biomes.FesterpusSwamp);
        var forgottenForest = world.GetZone(Defs.Biomes.ForgottenForest);
        var dampCave = world.GetZone(Defs.Biomes.DampCave);
        var cemetery = world.GetZone(Defs.Biomes.Cemetery);
        var mineShafts = world.GetZone(Defs.Biomes.Mineshaft);
        _zoneDisplay = new VerticalStackPanel();
        Content = new HorizontalStackPanel()
        {
            Widgets =
            {
                new VerticalStackPanel
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Spacing = 10,
                    Widgets =
                    {
                        /*new VerticalStackPanel
                        {
                            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.DeepGold],
                            Padding = new Thickness(15),
                            Widgets =
                            {
                                new Label(BaseContent.Styles.Label.Large) { Text = "Journey", HorizontalAlignment = HorizontalAlignment.Center }
                            }
                        },*/
                        CreateButton(peacefulMeadow),
                        CreateButton(outskirts, peacefulMeadow),
                        CreateButton(grainMill, outskirts),
                        CreateButton(festerpusSwamp, grainMill),
                        CreateButton(forgottenForest, festerpusSwamp),
                        CreateButton(dampCave, forgottenForest),
                        CreateButton(cemetery, dampCave),
                        CreateButton(mineShafts, cemetery),

                        // new TextButton(BaseContent.Styles.Button.Normal) { Text = "The Alchemist Hut", HorizontalAlignment = HorizontalAlignment.Stretch, Enabled = false },
                        // new TextButton(BaseContent.Styles.Button.Normal) { Text = "Forgemaster's Quarry", HorizontalAlignment = HorizontalAlignment.Stretch, Enabled = false },
                        // new TextButton(BaseContent.Styles.Button.Normal) { Text = "Fallow Field", HorizontalAlignment = HorizontalAlignment.Stretch, Enabled = false },
                        // new TextButton(BaseContent.Styles.Button.Normal) { Text = "Mage Tower", HorizontalAlignment = HorizontalAlignment.Stretch, Enabled = false },
                        // new TextButton(BaseContent.Styles.Button.Normal) { Text = "Field of Vegetables", HorizontalAlignment = HorizontalAlignment.Stretch, Enabled = false },
                        // new TextButton(BaseContent.Styles.Button.Normal) { Text = "Blood Court", HorizontalAlignment = HorizontalAlignment.Stretch, Enabled = false },
                        // new TextButton(BaseContent.Styles.Button.Normal) { Text = "His Rectory", HorizontalAlignment = HorizontalAlignment.Stretch, Enabled = false },
                        // new TextButton(BaseContent.Styles.Button.Normal) { Text = "Scarlet Chapel", HorizontalAlignment = HorizontalAlignment.Stretch, Enabled = false },
                        // new TextButton(BaseContent.Styles.Button.Normal) { Text = "Steamy Oil Vents", HorizontalAlignment = HorizontalAlignment.Stretch, Enabled = false },
                    }
                },
                _zoneDisplay
            }
        };
    }

    private Button CreateButton(Zone zone, Zone? previousZone = null)
    {
        if (!zone.IsComplete && (previousZone == null || previousZone.IsComplete))
        {
            ShowZone(zone);
        }

        var button = new Button(BaseContent.Styles.Button.Normal)
        {
            Content = new Label { Text = zone.BiomeDef.Label, HorizontalAlignment = HorizontalAlignment.Center },
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Enabled = !zone.IsComplete && (previousZone == null || previousZone.IsComplete)
        };
        button.Click += (_, _) => ShowZone(zone);
        return button;
    }

    private void ShowZone(Zone zone)
    {
        _zoneDisplay.Widgets.Clear();
        var startButton = new Button(BaseContent.Styles.Button.Large)
        {
            Content = new Label(BaseContent.Styles.Label.Large) { Text = $"/c[{TC.Green}]Start" }
        };
        startButton.Click += (_, _) => { Core.Context.EnterZone(zone.BiomeDef); };
        Button close = new(BaseContent.Styles.Button.Large)
        {
            Content = new Label(BaseContent.Styles.Label.Large) { Text = $"/c[{TC.Red}]Cancel" }
        };
        close.Click += (_, _) => Close();
        _zoneDisplay.Widgets.Add(new VerticalStackPanel
        {
            VerticalAlignment = VerticalAlignment.Top,
            Padding = new Thickness(40, 0, 40, 0),
            Background = new ColoredRegion(new TextureRegion(zone.BiomeDef.BackgroundTexture), new Color(20, 20, 20, 20)),
            Spacing = 5,
            Widgets =
            {
                new Label(BaseContent.Styles.Label.Huge)
                {
                    TextColor = Color.DarkGoldenrod,
                    Text = zone.BiomeDef.Label, HorizontalAlignment = HorizontalAlignment.Center
                },
                new Panel
                {
                    Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.DeepGold],
                    Padding = new Thickness(10),
                    Widgets = { new Image { Width = 800, Height = 450, Background = new TextureRegion(zone.BiomeDef.BackgroundTexture) } }
                },
                new HorizontalStackPanel
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(20),
                    Spacing = 10, Widgets =
                    {
                        startButton, close
                    }
                }
            }
        });
    }
}