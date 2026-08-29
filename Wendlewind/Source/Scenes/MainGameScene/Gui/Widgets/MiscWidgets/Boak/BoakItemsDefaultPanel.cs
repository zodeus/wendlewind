namespace Wendlewind.Scenes.MainGameScene.Gui.Widgets.MiscWidgets.Boak;

internal sealed class BoakItemDefaultCard : Panel
{
    private const int IconSize = 96;

    public BoakItemDefaultCard(ItemDef def)
    {
        Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.DeepGold];
        Padding = new Thickness(12);
        Width = 280;

        var content = new VerticalStackPanel
        {
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        // Icon
        var iconPanel = new Panel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Widgets =
            {
                new Image
                {
                    Width = IconSize,
                    Height = IconSize,
                    Background = new TextureRegion(def.Icon)
                }
            }
        };
        content.Widgets.Add(iconPanel);

        // Label
        content.Widgets.Add(new Label(BaseContent.Styles.Label.Medium)
        {
            Text = def.Label,
            HorizontalAlignment = HorizontalAlignment.Center,
            TextColor = BaseContent.Colors.Text.Golden
        });

        // Stats row
        var statsPanel = new HorizontalStackPanel
        {
            Spacing = 16,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 4, 0, 0)
        };

        var healingValue = def.BaseStats.FirstOrNull(s => s?.Def == Defs.Stats.HealingValue)?.Value;
        if (healingValue != null)
        {
            statsPanel.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
            {
                Text = $"Heal: {healingValue}",
                TextColor = new Color(100, 255, 100)
            });
        }

        var duration = def.MedicinalProperties?.DurationInTicks;
        if (duration != null)
        {
            statsPanel.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
            {
                Text = $"Dur: {duration}",
                TextColor = new Color(200, 200, 100)
            });
        }

        if (statsPanel.Widgets.Count > 0)
        {
            content.Widgets.Add(statsPanel);
        }

        // Description
        if (!string.IsNullOrEmpty(def.Description))
        {
            content.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
            {
                Text = def.Description,
                Wrap = true,
                MaxWidth = 250,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextColor = new Color(200, 200, 200),
                Margin = new Thickness(0, 4, 0, 0)
            });
        }

        Widgets.Add(content);
    }
}

internal sealed class BoakItemsDefaultPanel : ScrollViewer
{
    public BoakItemsDefaultPanel(IReadOnlyList<ItemDef> defs)
    {
        const int cardsPerRow = 6;
        var grid = new Grid
        {
            ColumnSpacing = 16,
            RowSpacing = 16,
            Margin = new Thickness(16)
        };

        for (var i = 0; i < cardsPerRow; i++)
        {
            grid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
        }

        var row = 0;
        var col = 0;
        foreach (var def in defs)
        {
            var card = new BoakItemDefaultCard(def);
            Grid.SetRow(card, row);
            Grid.SetColumn(card, col);
            grid.Widgets.Add(card);

            col++;
            if (col >= cardsPerRow)
            {
                col = 0;
                row++;
            }
        }

        Content = grid;
    }
}
