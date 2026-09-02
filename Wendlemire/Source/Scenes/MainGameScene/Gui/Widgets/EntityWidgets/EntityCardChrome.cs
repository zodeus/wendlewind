namespace Wendlemire.Scenes.MainGameScene.Gui.Widgets.EntityWidgets;

/// <summary>
/// Shared compact inspect-card chrome so every entity panel has the same shape:
/// framed icon, one name, wrapped flavor, tight padding.
/// </summary>
public static class EntityCardChrome
{
    public const int Icon = 48;
    public const int Frame = 52;
    public const int FlavorWidth = 260;
    public const int CardMinWidth = 320;
    public const int CardSpacing = 4;
    public static readonly Thickness CardPadding = new(10);

    public static readonly Color Flavor = new(170, 170, 170);
    public static readonly Color Section = BaseContent.Colors.Text.Golden;
    public static readonly Color Effect = new(140, 220, 140);
    public static readonly Color Muted = new(150, 150, 150);

    public static void ApplyCard(VerticalStackPanel panel, int? minWidth = null)
    {
        panel.Padding = CardPadding;
        panel.Spacing = CardSpacing;
        var width = minWidth ?? CardMinWidth;
        if (panel.MinWidth < width)
        {
            panel.MinWidth = width;
        }
    }

    public static HorizontalStackPanel Header(Entity entity, Color? titleColor = null, Widget? extra = null)
    {
        var iconFrame = new Panel
        {
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.DeepGold],
            Padding = new Thickness(2),
            Width = Frame,
            Height = Frame
        };
        iconFrame.Widgets.Add(new Image
        {
            Background = entity.GetIconImage(),
            Width = Icon,
            Height = Icon,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        });

        var info = new VerticalStackPanel
        {
            Spacing = 2,
            VerticalAlignment = VerticalAlignment.Center
        };
        info.Widgets.Add(new Label("small")
        {
            Text = entity.Label,
            TextColor = titleColor ?? Section
        });

        var desc = entity.Def.Description;
        if (!string.IsNullOrWhiteSpace(desc) && desc != "undefined")
        {
            info.Widgets.Add(new Label("small")
            {
                Text = desc,
                Wrap = true,
                MaxWidth = FlavorWidth,
                TextColor = Flavor
            });
        }

        var row = new HorizontalStackPanel { Spacing = 8, Widgets = { iconFrame, info } };
        if (extra != null)
        {
            extra.VerticalAlignment = VerticalAlignment.Center;
            row.Widgets.Add(extra);
        }

        return row;
    }

    public static Label SectionLabel(string text) =>
        new("small")
        {
            Text = text,
            TextColor = Section,
            Margin = new Thickness(0, 2, 0, 1)
        };

    public static Label BodyLabel(string text, Color? color = null, int maxWidth = 300) =>
        new("small")
        {
            Text = text,
            Wrap = true,
            MaxWidth = maxWidth,
            TextColor = color ?? Flavor
        };

    public static HorizontalStackPanel StatRow(string key, string value, Color? valueColor = null) =>
        new()
        {
            Spacing = 6,
            Widgets =
            {
                new Label("small") { Text = $"{key}:", TextColor = Muted },
                new Label("small") { Text = value, TextColor = valueColor ?? Color.LightGray }
            }
        };
}
