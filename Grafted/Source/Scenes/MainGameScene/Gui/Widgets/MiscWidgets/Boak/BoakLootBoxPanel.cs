using Grafted.Sim.LootBoxes;

namespace Grafted.Scenes.MainGameScene.Gui.Widgets.MiscWidgets.Boak;

internal sealed class BoakLootBoxPanel : Grid
{
    public BoakLootBoxPanel(IReadOnlyList<LootBoxDef> defs)
    {
        RowSpacing = 30;
        ColumnSpacing = 30;

        int gridRow = 0;
        int gridColum = 0;
        foreach (var def in defs)
        {
            var details = new VerticalStackPanel()
            {
                Spacing = 5,
                Margin = new Thickness(0, 0, 40, 0),
                Width = 600,
                Widgets =
                {
                    new Label(BaseContent.Styles.Label.Large) { Text = def.Label, Margin = new Thickness(0, 0, 0, 20) },
                    new Label(BaseContent.Styles.Label.Medium) { Text = $"Category: {def.Category}" },
                    new Label(BaseContent.Styles.Label.Medium) { Text = $"Rarity: {def.Rarity}" },
                    new Label(BaseContent.Styles.Label.Medium) { Text = $"Collection Limit: {def.CollectionLimit}" },
                    new Label(BaseContent.Styles.Label.Medium) { Text = $"Has Traps: {(def.TrapProperties != null ? "Yes" : "No")}" },
                }
            };

            def.Items.ToList().ForEach(
                i =>
                {
                    details.Widgets.Add(new HorizontalStackPanel
                    {
                        Spacing = 10,
                        Widgets =
                        {
                            new Image
                            {
                                Width = 48, Height = 48, Background = new TextureRegion(i.ItemDef.Icon)
                            },
                            new Label(BaseContent.Styles.Label.Medium)
                            {
                                VerticalAlignment = VerticalAlignment.Center,
                                Text = i.ItemDef.Label
                            }
                        }
                    });
                }
            );

            var panel = new HorizontalStackPanel()
            {
                Spacing = 10,
                Widgets =
                {
                    new Panel
                    {
                        VerticalAlignment = VerticalAlignment.Top,
                        Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.DeepGold],
                        Padding = new Thickness(10),
                        Widgets = { new Image { Width = 256, Height = 256, Background = new TextureRegion(def.Icon) } }
                    },
                    details
                }
            };
            SetRow(panel, gridRow);
            SetColumn(panel, gridColum);
            Widgets.Add(panel);

            gridColum++;
            if (gridColum > 2)
            {
                gridColum = 0;
                gridRow++;
            }
        }
    }
}