using Image = Myra.Graphics2D.UI.Image;

namespace Wendlemire.Scenes.MainGameScene.Gui.Widgets.CombatWidgets;

public sealed class BodyStanceBar : HorizontalStackPanel
{
    public BodyStanceBar(Pawn pawn, bool readOnly = false)
    {
        var buttons = new List<CursorButton>();
        var defaultColor = new Color(80, 80, 80, 100);
        foreach (var stance in DefRepository<BodyStanceDef>.Defs)
        {
            var button = new CursorButton(BaseContent.Styles.Button.Icon)
            {
                Content = new Image
                {
                    Background = new ColoredRegion(new TextureRegion(stance.GetTexture()), defaultColor),
                    Width = BaseContent.IconSizes.Medium, Height = BaseContent.IconSizes.Medium
                }
            };

            if (!readOnly)
            {
                button.TouchDown += (_, _) =>
                {
                    buttons.ForEach(b => ((ColoredRegion)b.Content.Background).Color = defaultColor);
                    ((ColoredRegion)button.Content.Background).Color = Color.Goldenrod;
                    pawn.Body.Stance = stance;
                };
            }

            buttons.Add(button);

            if (pawn.Body.Stance == stance)
            {
                ((ColoredRegion)button.Content.Background).Color = Color.Goldenrod;
            }

            button.WithTooltip(() => CreateStanceTooltip(stance));

            Widgets.Add(button);
        }
    }

    private static Widget CreateStanceTooltip(BodyStanceDef stance)
    {
        var container = new VerticalStackPanel { Spacing = 6, Padding = new Thickness(4) };

        container.Widgets.Add(new Label(BaseContent.Styles.Label.Normal)
        {
            Text = stance.Moniker,
            TextColor = Color.Gold
        });

        container.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
        {
            Text = stance.Label,
            TextColor = new Color(180, 180, 180),
            Wrap = true,
            MaxWidth = 250
        });

        if (stance.AffectedStats is { Count: > 0 })
        {
            container.Widgets.Add(new HorizontalSeparator { Color = new Color(60, 50, 40) });

            foreach (var stat in stance.AffectedStats)
            {
                var statRow = new HorizontalStackPanel { Spacing = 8 };

                var statName = new Label(BaseContent.Styles.Label.Small)
                {
                    Text = stat.Stat.Label,
                    TextColor = new Color(200, 200, 200)
                };
                statRow.Widgets.Add(statName);

                string valueText;
                Color valueColor;

                if (stat.Offset.HasValue)
                {
                    var offset = stat.Offset.Value;
                    var sign = offset >= 0 ? "+" : "";
                    valueText = $"{sign}{offset * 100:0}%";
                    valueColor = offset >= 0 ? new Color(100, 200, 100) : new Color(200, 100, 100);
                }
                else if (stat.Factor.HasValue)
                {
                    var factor = stat.Factor.Value;
                    valueText = $"x{factor:0.##}";
                    valueColor = factor >= 1 ? new Color(100, 200, 100) : new Color(200, 100, 100);
                }
                else
                {
                    continue;
                }

                var statValue = new Label(BaseContent.Styles.Label.Small)
                {
                    Text = valueText,
                    TextColor = valueColor
                };
                statRow.Widgets.Add(statValue);

                container.Widgets.Add(statRow);
            }
        }

        return container;
    }
}
