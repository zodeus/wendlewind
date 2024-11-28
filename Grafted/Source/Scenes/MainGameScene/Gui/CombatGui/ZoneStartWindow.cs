namespace Grafted.Scenes.MainGameScene.Gui.CombatGui;

internal class ZoneStartWindow : Window
{
    private readonly TextButton _startButton;
    public ZoneStartWindow(BiomeDef biome)
    {
        Title = biome.Label;
        _startButton = new TextButton(BaseContent.Styles.Button.Large);
        _startButton.Click += (_, _) => { Core.Context.EnterZone(biome); };
        TextButton close = new(BaseContent.Styles.Button.Large) { Text = $"/c[{TC.Red}]Cancel" };
        close.Click += (_, _) => Close();
        Content = new VerticalStackPanel()
        {
            Padding = new Thickness(50),
            Background = new ColoredRegion(new TextureRegion(biome.BackgroundTexture), new Color(20, 20, 20, 20)),
            Spacing = 10,
            Widgets =
            {
                //new Label(BaseContent.Styles.Label.Medium) { Text = $"Boss Kill: {(Core.Context.World.Zones[zone].IsComplete ? "Yes" : "No")}" },
                //new Label(BaseContent.Styles.Label.Medium) { Text = $"Kills: {Core.Context.World.Zones[zone].TotalZoneKills}" },
                new Label(BaseContent.Styles.Label.Medium) { Text = "Enemies" },
                GenerateZoneEnemies(biome),
                new HorizontalSeparator(),
                new Label(BaseContent.Styles.Label.Medium) { Text = "Resources" },
                GenerateZoneResources(biome),
                new HorizontalSeparator(),
                new HorizontalStackPanel
                {
                    Spacing = 10, Widgets =
                    {
                        _startButton, close
                    }
                }
            }
        };
    }

    private Widget GenerateZoneEnemies(BiomeDef zone)
    {
        HorizontalStackPanel panel = new() { Spacing = 5 };
        var enemies = DefRepository<CombatConfigDef>.Defs.Where(def => def.Biome == zone).SelectMany(def => def.Enemies).DistinctBy(record => record.Race);
        foreach (CombatConfigEnemyRecord enemyConfig in enemies)
        {
            panel.AddChild(new Image() { Background = new TextureRegion(enemyConfig.Race.Icon), Width = 256, Height = 256 });
        }

        return panel;
    }

    private Widget GenerateZoneResources(BiomeDef zone)
    {
        HorizontalStackPanel panel = new() { Spacing = 5 };
        foreach (ZoneResourceRecord record in zone.Resources)
        {
            panel.AddChild(new Image() { Background = new TextureRegion(record.Item.Icon), Width = 80, Height = 80 });
        }

        return panel;
    }

    public void Update()
    {
        if (!IsPlaced)
        {
            return;
        }

        _startButton.Text = $"/c[{TC.Green}]Start";
    }
}