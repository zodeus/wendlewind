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
        EntityCardChrome.ApplyCard(this, 340);

        Widgets.Add(EntityCardChrome.Header(item, WarmGlow));

        var incenseProps = item.ItemDef.IncenseProperties;
        if (incenseProps?.Effect != null)
        {
            var durationSeconds = incenseProps.GetDurationInTicks() / (float)GameContext.TicksPerSecond;
            Widgets.Add(new Label("small")
            {
                Text = $"Lasts {durationSeconds:0.#}s once lit",
                TextColor = WarmGlow
            });
            Widgets.Add(new Label("small")
            {
                Text = "Slots light at 120, 240, then 360",
                TextColor = AshGray
            });

            Widgets.Add(EntityCardChrome.SectionLabel("When Lit"));

            var effectRow = new HorizontalStackPanel
            {
                Spacing = 6,
                Margin = new Thickness(4, 0, 0, 0)
            };

            effectRow.Widgets.Add(new Image
            {
                Background = new TextureRegion(incenseProps.Effect.Def.GetTexture()),
                Width = 16,
                Height = 16
            });

            effectRow.Widgets.Add(new Label("small")
            {
                Text = incenseProps.Effect.Def.Label,
                TextColor = IncenseProperties.GetEffectColor(incenseProps.Effect.Def)
            });

            effectRow.Widgets.Add(new Label("small")
            {
                Text = $"({durationSeconds:0.#}s)",
                TextColor = Color.DarkGray
            });

            Widgets.Add(effectRow);

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
                        Spacing = 6,
                        Margin = new Thickness(20, 0, 0, 0),
                        Widgets =
                        {
                            new Label("small")
                            {
                                Text = affectedStat.Stat.Label,
                                TextColor = AshGray,
                                Width = 110
                            },
                            new Label("small") { Text = $"{offset}{factor}" }
                        }
                    });
                }
            }
        }

        if (item.StackSize > 1 || item.ItemDef.StackLimit > 1)
        {
            _stackLabel = new Label("small")
            {
                Text = $"Stack: {item.StackSize}",
                TextColor = AshGray
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
