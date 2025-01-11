namespace Grafted.Scenes.MainGameScene.Gui.CombatGui;

internal sealed class ZoneStartWindow : Window
{
    private readonly Button _startButton;

    public ZoneStartWindow(BiomeDef biome)
    {
        Title = biome.Label;
        TitleFont = BaseContent.Fonts.Default.Large;

        _startButton = new Button(BaseContent.Styles.Button.Large)
        {
            Content = new Label { Text = $"/c[{TC.Green}]Start" }
        };
        _startButton.Click += (_, _) => { Core.Context.EnterZone(biome); };
        Button close = new(BaseContent.Styles.Button.Large)
        {
            Content = new Label { Text = $"/c[{TC.Red}]Cancel" }
        };
        close.Click += (_, _) => Close();
        Content = new VerticalStackPanel()
        {
            Padding = new Thickness(50),
            Background = new ColoredRegion(new TextureRegion(biome.BackgroundTexture), new Color(20, 20, 20, 20)),
            Spacing = 10,
            Widgets =
            {
                new Image { Width = 800, Height = 450, Background = new TextureRegion(biome.BackgroundTexture) },
                new HorizontalSeparator(),
                new HorizontalStackPanel
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Spacing = 10, Widgets =
                    {
                        _startButton, close
                    }
                }
            }
        };
    }
}