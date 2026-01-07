using SolidBrush = Myra.Graphics2D.Brushes.SolidBrush;

namespace Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets;

[UsedImplicitly]
public sealed class IncensePanel : EntityPanelBase
{
    private static readonly Color WarmGlow = new(255, 200, 120);
    private static readonly Color AshGray = new(180, 170, 160);
    private static readonly Color DeepEmber = new(180, 80, 30);

    public IncensePanel(BaseGui gui, Item item, EntityPanelProperties? properties = null) : base(gui, item, properties)
    {
        Padding = new Thickness(24);
        MinWidth = 420;
        Spacing = 8;

        // ═══════════════════════════════════════════════════════════════════
        // Header Section: Framed Icon + Description
        // ═══════════════════════════════════════════════════════════════════
        var headerSection = new HorizontalStackPanel
        {
            Spacing = 18,
            Margin = new Thickness(0, 0, 0, 16)
        };

        // Icon with decorative ember-glow frame
        var iconOuter = new Panel
        {
            Background = new SolidBrush(DeepEmber),
            Padding = new Thickness(3),
            Width = 100, Height = 100
        };
        var iconInner = new Panel
        {
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.DeepGold],
            Padding = new Thickness(4)
        };
        iconInner.Widgets.Add(new Image
        {
            Background = new TextureRegion(item.Icon),
            Width = 84, Height = 84,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        });
        iconOuter.Widgets.Add(iconInner);
        headerSection.Widgets.Add(iconOuter);

        // Description with warm styling
        var descArea = new VerticalStackPanel
        {
            Spacing = 8,
            VerticalAlignment = VerticalAlignment.Center
        };

        if (!string.IsNullOrEmpty(item.Def.Description) && item.Def.Description != "undefined")
        {
            descArea.Widgets.Add(new Label(BaseContent.Styles.Label.Normal)
            {
                Text = item.Def.Description,
                Wrap = true,
                MaxWidth = 260,
                TextColor = WarmGlow
            });
        }

        // Stack count indicator
        if (item.StackSize > 1)
        {
            descArea.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
            {
                Text = $"× {item.StackSize}",
                TextColor = AshGray,
                Margin = new Thickness(0, 4, 0, 0)
            });
        }

        headerSection.Widgets.Add(descArea);
        Widgets.Add(headerSection);

        // ═══════════════════════════════════════════════════════════════════
        // Effect Info Section (if has incense properties)
        // ═══════════════════════════════════════════════════════════════════
        var incenseProps = item.ItemDef.IncenseProperties;
        if (incenseProps?.Effect != null)
        {
            Widgets.Add(new Panel
            {
                Height = 2,
                Background = new SolidBrush(new Color(80, 60, 40)),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(0, 4, 0, 12)
            });

            var effectSection = new VerticalStackPanel { Spacing = 4 };
            effectSection.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
            {
                Text = "When Burned:",
                TextColor = AshGray
            });
            effectSection.Widgets.Add(new Label(BaseContent.Styles.Label.Normal)
            {
                Text = incenseProps.Effect.Def.Label,
                TextColor = WarmGlow
            });
            if (!string.IsNullOrEmpty(incenseProps.Effect.Def.Description))
            {
                effectSection.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
                {
                    Text = incenseProps.Effect.Def.Description,
                    TextColor = AshGray,
                    Wrap = true,
                    MaxWidth = 350
                });
            }
            var durationSeconds = incenseProps.Effect.DurationInTicks / 60f;
            effectSection.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
            {
                Text = $"Duration: {durationSeconds:0.#}s",
                TextColor = Color.DarkGray,
                Margin = new Thickness(0, 4, 0, 0)
            });
            Widgets.Add(effectSection);
        }
    }

    public override void Update()
    {
    }
}
