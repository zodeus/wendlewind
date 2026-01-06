using SolidBrush = Myra.Graphics2D.Brushes.SolidBrush;

namespace Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets;

[UsedImplicitly]
public sealed class FlammablePanel : EntityPanelBase
{
    private static readonly Color EmberOrange = new(255, 140, 50);
    private static readonly Color AshGray = new(180, 170, 160);
    private static readonly Color WarmGlow = new(255, 200, 120);
    private static readonly Color DeepEmber = new(180, 80, 30);

    public FlammablePanel(BaseGui gui, Item item, EntityPanelProperties? properties = null) : base(gui, item, properties)
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
        // Divider
        // ═══════════════════════════════════════════════════════════════════
        Widgets.Add(new Panel
        {
            Height = 2,
            Background = new SolidBrush(new Color(80, 60, 40)),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Margin = new Thickness(0, 4, 0, 12)
        });

        // ═══════════════════════════════════════════════════════════════════
        // Actions Section
        // ═══════════════════════════════════════════════════════════════════
        var actionsPanel = new HorizontalStackPanel { Spacing = 16 };

        // Burn Wood Button - fire themed
        if (ShowBurnWood(Core.Context.Player, item))
        {
            var burnButton = CreateFireThemedButton("Burn Wood", () => BurnWood(gui, Core.Context.Player, item));
            actionsPanel.Widgets.Add(burnButton);
        }

        if (actionsPanel.Widgets.Count > 0)
        {
            Widgets.Add(actionsPanel);
        }
    }

    private Panel CreateFireThemedButton(string text, Action onClick)
    {
        // Outer glow container
        var buttonContainer = new Panel
        {
            Background = new SolidBrush(DeepEmber),
            Padding = new Thickness(2)
        };

        var button = new CursorButton(BaseContent.Styles.Button.Normal)
        {
            Content = new HorizontalStackPanel
            {
                Spacing = 8,
                HorizontalAlignment = HorizontalAlignment.Center,
                Widgets =
                {
                    new Label(BaseContent.Styles.Label.Normal)
                    {
                        Text = "🔥",
                        VerticalAlignment = VerticalAlignment.Center
                    },
                    new Label(BaseContent.Styles.Label.Normal)
                    {
                        Text = text,
                        TextColor = EmberOrange,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                }
            },
            Padding = new Thickness(16, 8)
        };
        button.Click += (_, _) => onClick();

        buttonContainer.Widgets.Add(button);
        return buttonContainer;
    }

    private void BurnWood(BaseGui gui, Player player, Item item)
    {
        if (item.StackSize > 1)
        {
            item.StackSize--;
        }
        else
        {
            item.Destroy();
        }
        Core.Context.Achievements.OnItemUsed(player.Pawn, item);

        if (item.ItemDef == Defs.Items.GlitteringLog)
        {
            gui.PushScreenMessage(new ScreenMessageData
            {
                Font = BaseContent.Fonts.Default.Medium,
                Text = Defs.BodyEffects.SmokeyHaze.Description,
                Duration = 6,
                Color = Color.Orange
            });
            player.Pawn.Body.Effects.TryApplyEffect(new BodyEffect
            {
                Def = Defs.BodyEffects.SmokeyHaze,
                TicksLeft = 4000
            });
        }
        else if (item.ItemDef == Defs.Items.ShimmeringBark)
        {
            gui.PushScreenMessage(new ScreenMessageData
            {
                Font = BaseContent.Fonts.Default.Medium,
                Text = Defs.BodyEffects.SmokeyHaze.Description,
                Duration = 6,
                Color = Color.Orange
            });
            player.Pawn.Body.Effects.TryApplyEffect(new BodyEffect
            {
                Def = Defs.BodyEffects.Psychedelic,
                TicksLeft = 4000
            });
        }
        else if (item.ItemDef == Defs.Items.GoldenWood)
        {
            gui.PushScreenMessage(new ScreenMessageData
            {
                Font = BaseContent.Fonts.Default.Medium,
                Text = Defs.BodyEffects.GoldenSmoke.Description,
                Duration = 6,
                Color = Color.Orange
            });
            player.Pawn.Body.Effects.TryApplyEffect(new BodyEffect
            {
                Def = Defs.BodyEffects.GoldenSmoke,
                TicksLeft = 2000
            });
        }
    }

    private bool ShowBurnWood(Player player, Item item)
    {
        if (item.ItemDef == Defs.Items.GlitteringLog ||
            item.ItemDef == Defs.Items.ShimmeringBark ||
            item.ItemDef == Defs.Items.GoldenWood)
        {
            return player.HasTrinkets(Defs.Items.FlameStick);
        }

        return false;
    }

    public override void Update()
    {
    }
}

