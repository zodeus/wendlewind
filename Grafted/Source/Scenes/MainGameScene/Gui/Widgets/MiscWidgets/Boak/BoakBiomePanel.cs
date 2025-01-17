namespace Grafted.Scenes.MainGameScene.Gui.Widgets.MiscWidgets.Boak;

internal sealed class BoakBiomePanel : VerticalStackPanel
{
    public BoakBiomePanel(IReadOnlyList<BiomeDef> defs)
    {
        Spacing = 20;
        foreach (var biomeDef in defs)
        {
            var details = new VerticalStackPanel()
            {
                Margin = new Thickness(0, 0, 40, 0),
                Width = 700,
                Widgets = { new Label(BaseContent.Styles.Label.Large) { Text = biomeDef.Label } }
            };
            biomeDef.Resources.ToList().ForEach(
                r =>
                {
                    details.Widgets.Add(new HorizontalStackPanel
                    {
                        Spacing = 10,
                        Widgets =
                        {
                            new Image
                            {
                                Width = 64, Height = 64, Background = new TextureRegion(r.Item.Icon)
                            },
                            new Label(BaseContent.Styles.Label.Medium)
                            {
                                VerticalAlignment = VerticalAlignment.Center,
                                Text = r.Item.Label
                            }
                        }
                    });
                }
            );

            var encounters = new VerticalStackPanel { };
            DefRepository<EncounterDef>.Defs
                .Where(d => d.Biome == biomeDef).ToList()
                .ForEach(e =>
                {
                    encounters.Widgets.Add(new Label(BaseContent.Styles.Label.Medium)
                    {
                        Text = $"{e.Moniker} - {e.Enemies.FirstOrNull()?.PawnName}"
                    });
                });
            Widgets.Add(new HorizontalStackPanel()
            {
                Spacing = 10,
                Widgets =
                {
                    new Panel
                    {
                        Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.DeepGold],
                        Padding = new Thickness(10),
                        Widgets = { new Image { Width = 800, Height = 450, Background = new TextureRegion(biomeDef.BackgroundTexture) } }
                    },
                    details,
                    encounters
                }
            });
        }
    }
}