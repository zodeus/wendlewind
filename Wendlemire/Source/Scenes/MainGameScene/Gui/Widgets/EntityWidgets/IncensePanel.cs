using SolidBrush = Myra.Graphics2D.Brushes.SolidBrush;

namespace Wendlemire.Scenes.MainGameScene.Gui.Widgets.EntityWidgets;

[UsedImplicitly]
public sealed class IncensePanel : EntityPanelBase
{
    private static readonly Color WarmGlow = new(255, 200, 120);
    private static readonly Color AshGray = new(180, 170, 160);

    private readonly Item _item;
    private readonly Label? _stackLabel;

    public IncensePanel(BaseGui gui, Item item, EntityPanelProperties? properties = null) : base(gui, item, properties)
    {
        _item = item;
        Padding = new Thickness(24);
        MinWidth = 420;
        Spacing = 8;

        // Header: Icon + Name/Description
        var headerSection = new HorizontalStackPanel
        {
            Spacing = 18,
            Margin = new Thickness(0, 0, 0, 16)
        };

        // Icon with decorative frame
        var iconFrame = new Panel
        {
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.DeepGold],
            Padding = new Thickness(4)
        };
        iconFrame.Widgets.Add(new Image
        {
            Background = item.GetIconImage(),
            Width = 84,
            Height = 84,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        });
        headerSection.Widgets.Add(iconFrame);

        // Name and description
        var infoArea = new VerticalStackPanel
        {
            Spacing = 6,
            VerticalAlignment = VerticalAlignment.Center
        };

        infoArea.Widgets.Add(new Label(BaseContent.Styles.Label.Normal)
        {
            Text = item.Label,
            TextColor = WarmGlow
        });

        if (!string.IsNullOrEmpty(item.Def.Description) && item.Def.Description != "undefined")
        {
            infoArea.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
            {
                Text = item.Def.Description,
                Wrap = true,
                MaxWidth = 260,
                TextColor = AshGray
            });
        }

        headerSection.Widgets.Add(infoArea);
        Widgets.Add(headerSection);

        // Effect section
        var incenseProps = item.ItemDef.IncenseProperties;
        if (incenseProps?.Effect != null)
        {
            // Separator
            Widgets.Add(new Panel
            {
                Height = 2,
                Background = new SolidBrush(new Color(80, 60, 40)),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(0, 4, 0, 12)
            });

            var durationSeconds = incenseProps.GetDurationInTicks() / (float)GameContext.TicksPerSecond;
            Widgets.Add(new Label(BaseContent.Styles.Label.Small)
            {
                Text = $"Lasts {durationSeconds:0.#}s once lit",
                TextColor = WarmGlow
            });
            Widgets.Add(new Label(BaseContent.Styles.Label.Small)
            {
                Text = "Slots light at 120, 240, then 360",
                TextColor = AshGray
            });

            Widgets.Add(new Label(BaseContent.Styles.Label.Small)
            {
                Text = "When Lit:",
                TextColor = AshGray,
                Margin = new Thickness(0, 8, 0, 0)
            });

            // Effect with icon
            var effectRow = new HorizontalStackPanel
            {
                Spacing = 10,
                Margin = new Thickness(10, 4, 0, 0)
            };

            effectRow.Widgets.Add(new Image
            {
                Background = new TextureRegion(incenseProps.Effect.Def.GetTexture()),
                Width = 20,
                Height = 20
            });

            var effectColor = IncenseProperties.GetEffectColor(incenseProps.Effect.Def);
            effectRow.Widgets.Add(new Label(BaseContent.Styles.Label.Normal)
            {
                Text = incenseProps.Effect.Def.Label,
                TextColor = effectColor
            });

            effectRow.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
            {
                Text = $"({durationSeconds:0.#}s)",
                TextColor = Color.DarkGray
            });

            Widgets.Add(effectRow);

            // Buff/stat effects granted by this effect
            var affectedStats = incenseProps.Effect.Def.AffectedStats;
            if (affectedStats != null)
            {
                foreach (var affectedStat in affectedStats)
                {
                    var offset = affectedStat.Offset != null
                        ? $"/c[{(affectedStat.Offset > 0 ? TC.Green : TC.Red)}]+{affectedStat.Offset} "
                        : "";
                    var factor = affectedStat.Factor != null
                        ? $"/c[{(affectedStat.Factor > 0 ? TC.Green : TC.Red)}]*{affectedStat.Factor} "
                        : "";

                    Widgets.Add(new HorizontalStackPanel
                    {
                        Spacing = 10,
                        Margin = new Thickness(40, 2, 0, 0),
                        Widgets =
                        {
                            new Label(BaseContent.Styles.Label.Small)
                            {
                                Text = affectedStat.Stat.Label,
                                TextColor = AshGray,
                                Width = 120
                            },
                            new Label(BaseContent.Styles.Label.Small)
                            {
                                Text = $"{offset}{factor}"
                            }
                        }
                    });
                }
            }
        }

        // Stack info
        if (item.StackSize > 1 || item.ItemDef.StackLimit > 1)
        {
            _stackLabel = new Label(BaseContent.Styles.Label.Small)
            {
                Text = $"Stack: {item.StackSize}",
                TextColor = AshGray,
                Margin = new Thickness(0, 8, 0, 0)
            };
            Widgets.Add(_stackLabel);
        }
    }

    public override void Update()
    {
        if (_stackLabel != null)
        {
            _stackLabel.Text = $"Stack: {_item.StackSize}";
        }
    }
}
