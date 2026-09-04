namespace Wendlemire.Scenes.MainGameScene.Gui.Widgets.EntityWidgets;

/// <summary>
/// Shared compact inspect-card chrome so every entity panel has the same shape:
/// framed icon, one name, wrapped flavor, tight padding.
/// </summary>
public static class EntityCardChrome
{
    public const int Icon = 40;
    public const int Frame = 60;
    public const int InspectWidth = 440;
    public const int FlavorWidth = 342;
    public const int CardMinWidth = InspectWidth;
    public const int CardSpacing = 6;
    public static readonly Thickness CardPadding = new(12, 12, 14, 12);

    public static readonly Color Flavor = new(176, 172, 164);
    public static readonly Color Section = BaseContent.Colors.Text.Golden;
    public static readonly Color Effect = new(140, 220, 140);
    public static readonly Color Muted = new(150, 150, 150);
    public static readonly Color Rule = new(78, 62, 40);
    public static readonly Color Inset = new(14, 12, 10, 210);
    public static readonly Color InsetBorder = new(96, 74, 44);
    public static readonly Color Mechanic = new(206, 200, 188);
    public static readonly Color Gold = ColorExt.HexToColor(TC.Golden.TrimStart('#'));
    public static readonly Color Info = ColorExt.HexToColor(TC.Blue.TrimStart('#'));
    public static readonly Color Tan = new(180, 140, 100);

    public readonly record struct CardMetrics(int CardWidth, int ContentWidth, int FlavorWidth, int BodyWidth);

    public static CardMetrics Metrics(int cardWidth)
    {
        var content = Math.Max(200, cardWidth - CardPadding.Left - CardPadding.Right);
        var flavor = Math.Max(160, content - Frame - 12);
        return new CardMetrics(cardWidth, content, flavor, content);
    }

    public static CardMetrics ApplyCard(VerticalStackPanel panel, int? minWidth = null)
    {
        panel.Padding = CardPadding;
        panel.Spacing = CardSpacing;
        var width = minWidth ?? CardMinWidth;
        if (panel.MinWidth < width)
        {
            panel.MinWidth = width;
        }

        return Metrics(width);
    }

    public static CardMetrics BeginInspect(
        VerticalStackPanel panel,
        Entity entity,
        Color? titleColor = null,
        Widget? extra = null,
        int? minWidth = null)
    {
        var card = ApplyCard(panel, minWidth);
        panel.Widgets.Add(Header(entity, titleColor, extra, card.FlavorWidth));
        panel.Widgets.Add(Hairline());
        return card;
    }

    public static HorizontalStackPanel Header(
        Entity entity,
        Color? titleColor = null,
        Widget? extra = null,
        int? flavorWidth = null)
    {
        var iconFrame = new Panel
        {
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.DeepGold],
            Padding = new Thickness(8),
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
            Spacing = 3,
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
                MaxWidth = flavorWidth ?? FlavorWidth,
                TextColor = Flavor
            });
        }

        var row = new HorizontalStackPanel { Spacing = 12, Widgets = { iconFrame, info } };
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
            Margin = new Thickness(0, 4, 0, 1)
        };

    public static Widget SectionHeader(string text)
    {
        return new VerticalStackPanel
        {
            Spacing = 3,
            Margin = new Thickness(0, 4, 0, 2),
            Widgets =
            {
                new Label("small") { Text = text, TextColor = Section },
                new Panel
                {
                    Height = 1,
                    Width = 56,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Background = new SolidBrush(new Color(168, 118, 48, 200))
                }
            }
        };
    }

    public static Widget Hairline() =>
        new Panel
        {
            Height = 1,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Margin = new Thickness(0, 2),
            Background = new SolidBrush(Rule)
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

    public static VerticalStackPanel StatChip(string key, string value, Color color, out Label valueLabel)
    {
        valueLabel = new Label("small") { Text = value, TextColor = color };
        return new VerticalStackPanel
        {
            Spacing = 1,
            Widgets =
            {
                new Label("small") { Text = key.ToUpperInvariant(), TextColor = Muted },
                valueLabel
            }
        };
    }

    public static Widget StatStrip(params (string Key, string Value, Color Color)[] stats) =>
        StatStrip(stats.Select(stat => (Widget)StatChip(stat.Key, stat.Value, stat.Color, out _)));

    public static Widget StatStrip(IEnumerable<Widget> chips)
    {
        var row = new HorizontalStackPanel { Spacing = 22 };
        foreach (var chip in chips)
        {
            row.Widgets.Add(chip);
        }

        return new Panel
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Background = new SolidBrush(Inset),
            Border = new SolidBrush(InsetBorder),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(10, 7),
            Widgets = { row }
        };
    }

    public static Widget InsetBlock(int bodyWidth, params Widget[] children)
    {
        var stack = new VerticalStackPanel
        {
            Spacing = 5,
            Padding = new Thickness(10, 8),
            Width = bodyWidth,
            Background = new SolidBrush(Inset),
            Border = new SolidBrush(InsetBorder),
            BorderThickness = new Thickness(1)
        };
        foreach (var child in children)
        {
            stack.Widgets.Add(child);
        }

        return stack;
    }

    public static HorizontalStackPanel IconLabel(
        IBrush? icon,
        string label,
        Color color,
        string? meta = null)
    {
        var row = new HorizontalStackPanel { Spacing = 8 };
        if (icon != null)
        {
            row.Widgets.Add(new Image
            {
                Background = icon,
                Width = 16,
                Height = 16,
                VerticalAlignment = VerticalAlignment.Center
            });
        }

        row.Widgets.Add(new Label("small")
        {
            Text = label,
            TextColor = color,
            VerticalAlignment = VerticalAlignment.Center
        });
        if (!string.IsNullOrWhiteSpace(meta))
        {
            row.Widgets.Add(new Label("small")
            {
                Text = meta,
                TextColor = Muted,
                VerticalAlignment = VerticalAlignment.Center
            });
        }

        return row;
    }

    public static Widget MechanicsBlock(IReadOnlyList<string> lines, int bodyWidth)
    {
        var block = new VerticalStackPanel
        {
            Spacing = 0,
            Padding = new Thickness(10, 8),
            Width = bodyWidth,
            Background = new SolidBrush(Inset),
            Border = new SolidBrush(InsetBorder),
            BorderThickness = new Thickness(1)
        };

        var textWidth = Math.Max(140, bodyWidth - 28);
        for (var i = 0; i < lines.Count; i++)
        {
            if (i > 0)
            {
                block.Widgets.Add(new Panel
                {
                    Height = 1,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    Margin = new Thickness(0, 6),
                    Background = new SolidBrush(Rule)
                });
            }

            block.Widgets.Add(new HorizontalStackPanel
            {
                Spacing = 8,
                Widgets =
                {
                    new Panel
                    {
                        Width = 3,
                        Height = 11,
                        Margin = new Thickness(0, 3, 0, 0),
                        Background = new SolidBrush(Section)
                    },
                    new Label("small")
                    {
                        Text = lines[i],
                        Wrap = true,
                        MaxWidth = textWidth,
                        TextColor = Mechanic
                    }
                }
            });
        }

        return block;
    }
}
