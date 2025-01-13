namespace Grafted.Scenes.MainGameScene.Gui.CombatGui;

internal sealed class ZoneStartWindow : Window
{
    private readonly Button _startButton;

    public ZoneStartWindow(BiomeDef biome)
    {
        TitleFont = BaseContent.Fonts.Default.Large;

        _startButton = new Button(BaseContent.Styles.Button.Large)
        {
            Content = new Label(BaseContent.Styles.Label.Large) { Text = $"/c[{TC.Green}]Start" }
        };
        _startButton.Click += (_, _) => { Core.Context.EnterZone(biome); };
        Button close = new(BaseContent.Styles.Button.Large)
        {
            Content = new Label(BaseContent.Styles.Label.Large) { Text = $"/c[{TC.Red}]Cancel" }
        };
        close.Click += (_, _) => Close();
        Content = new VerticalStackPanel()
        {
            VerticalAlignment = VerticalAlignment.Top,
            Padding = new Thickness(40,0,40,0),
            Background = new ColoredRegion(new TextureRegion(biome.BackgroundTexture), new Color(20, 20, 20, 20)),
            Spacing = 5,
            Widgets =
            {
                new Label(BaseContent.Styles.Label.Huge)
                {
                    TextColor = Color.DarkGoldenrod,
                    Text = biome.Label, HorizontalAlignment = HorizontalAlignment.Center
                },
                new Panel
                {
                    Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.DeepGold],
                    Padding = new Thickness(10),
                    Widgets = { new Image { Width = 800, Height = 450, Background = new TextureRegion(biome.BackgroundTexture) } }
                },
                new HorizontalStackPanel
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(20),
                    Spacing = 10, Widgets =
                    {
                        _startButton, close
                    }
                }
            }
        };
    }
}