namespace Wendlemire.Scenes.MainGameScene.Gui.Widgets.EntityWidgets;

public sealed class PotionPanel : EntityPanelBase
{
    private readonly Item _item;
    private readonly Label _stackLabel;

    public PotionPanel(BaseGui gui, Item item, EntityPanelProperties? properties = null) : base(gui, item, properties)
    {
        _item = item;
        Padding = new Thickness(16);
        MinWidth = 380;
        Spacing = 12;

        // Header with icon and title info
        var headerPanel = CreateHeaderPanel(item);
        Widgets.Add(headerPanel);

        // Separator
        Widgets.Add(new HorizontalSeparator { Margin = new Thickness(0, 4, 0, 4) });

        // Effect description from handler or def
        var effectDescription = GetEffectDescription(item);
        if (!string.IsNullOrEmpty(effectDescription))
        {
            var effectPanel = new VerticalStackPanel
            {
                Spacing = 4,
                Widgets =
                {
                    new Label(BaseContent.Styles.Label.Small)
                    {
                        Text = "Effect",
                        TextColor = new Color(160, 160, 160)
                    },
                    new Label(BaseContent.Styles.Label.Normal)
                    {
                        Text = effectDescription,
                        Wrap = true,
                        MaxWidth = 340,
                        TextColor = new Color(140, 220, 140)
                    }
                }
            };
            Widgets.Add(effectPanel);
        }

        // Stats panel (duration, etc.)
        var statsPanel = CreateStatsPanel(item);
        if (statsPanel != null)
        {
            Widgets.Add(statsPanel);
        }

        // Stack info
        if (item.IsStackable && item.StackSize > 1)
        {
            _stackLabel = new Label(BaseContent.Styles.Label.Small)
            {
                Text = $"Quantity: /c[{TC.Golden}]{item.StackSize}",
                Margin = new Thickness(0, 4, 0, 0)
            };
            Widgets.Add(_stackLabel);
        }
        else
        {
            _stackLabel = new Label(BaseContent.Styles.Label.Small) { Visible = false };
        }

        // Usage info panel
        var usageInfoPanel = CreateUsageInfoPanel(item);
        if (usageInfoPanel != null)
        {
            Widgets.Add(usageInfoPanel);
        }

        // Action buttons
        var buttonsPanel = CreateButtonsPanel(item, gui);
        if (buttonsPanel != null)
        {
            Widgets.Add(buttonsPanel);
        }
    }

    private HorizontalStackPanel CreateHeaderPanel(Item item)
    {
        // Icon with decorative frame
        var iconPanel = new Panel
        {
            Width = 96,
            Height = 96,
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrame]
        };

        iconPanel.Widgets.Add(new Image
        {
            Background = item.GetIconImage(),
            Width = 80,
            Height = 80,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        });

        // Title and description
        var infoPanel = new VerticalStackPanel
        {
            Spacing = 6,
            VerticalAlignment = VerticalAlignment.Center,
            Widgets =
            {
                new Label(BaseContent.Styles.Label.Large)
                {
                    Text = item.Def.Label,
                    TextColor = GetPotionTitleColor(item)
                },
                new Label(BaseContent.Styles.Label.Small)
                {
                    Text = item.Def.Description,
                    Wrap = true,
                    MaxWidth = 250,
                    TextColor = new Color(180, 180, 180)
                }
            }
        };

        return new HorizontalStackPanel
        {
            Spacing = 14,
            Widgets = { iconPanel, infoPanel }
        };
    }

    private static bool IsHealingPotion(Item item) =>
        item.Def == Defs.Items.HealingPotion
        || item.Def == Defs.Items.HealingFlask
        || item.Def == Defs.Items.HealingSalve;

    private Color GetPotionTitleColor(Item item)
    {
        // Different colors based on potion type
        if (item.Def == Defs.Items.JarOfBlood)
            return new Color(180, 40, 40);
        if (item.Def == Defs.Items.Pitchblood)
            return new Color(90, 20, 20);
        if (item.Def == Defs.Items.AcidFlask)
            return new Color(140, 200, 60);
        if (item.Def == Defs.Items.TallowFlask)
            return new Color(210, 180, 90);
        if (IsHealingPotion(item))
            return new Color(200, 80, 100);

        return BaseContent.Colors.Text.Golden;
    }

    private string GetEffectDescription(Item item)
    {
        // Use handler's effect description if available
        if (item.PotionHandler != null)
        {
            return item.PotionHandler.GetEffectDescription();
        }

        // Fallback descriptions based on def
        if (item.Def == Defs.Items.JarOfBlood)
            return "Instantly restores all lost blood.";
        if (item.Def == Defs.Items.AcidFlask)
            return "Throws acid at opponent, potentially blinding them.";
        if (IsHealingPotion(item))
            return "Applies regeneration to all body parts.";

        return "";
    }

    private VerticalStackPanel? CreateStatsPanel(Item item)
    {
        var potionDuration = (int)item.GetStatValue(Defs.Stats.PotionDuration);
        if (potionDuration <= 0) return null;

        var statsPanel = new VerticalStackPanel
        {
            Spacing = 4,
            Margin = new Thickness(0, 4, 0, 0),
            Widgets =
            {
                new Label(BaseContent.Styles.Label.Small)
                {
                    Text = "Stats",
                    TextColor = new Color(160, 160, 160)
                }
            }
        };

        var durationRow = new HorizontalStackPanel
        {
            Spacing = 8,
            Widgets =
            {
                new Label(BaseContent.Styles.Label.Normal)
                {
                    Text = "Duration:",
                    TextColor = new Color(200, 200, 200)
                },
                new Label(BaseContent.Styles.Label.Normal)
                {
                    Text = $"{potionDuration} ticks",
                    TextColor = new Color(100, 180, 255)
                }
            }
        };
        statsPanel.Widgets.Add(durationRow);

        var potionPower = item.GetStatValue(Defs.Stats.PotionPower);
        if (potionPower > 0 && potionPower != 1)
        {
            statsPanel.Widgets.Add(new HorizontalStackPanel
            {
                Spacing = 8,
                Widgets =
                {
                    new Label(BaseContent.Styles.Label.Normal)
                    {
                        Text = "Power:",
                        TextColor = new Color(200, 200, 200)
                    },
                    new Label(BaseContent.Styles.Label.Normal)
                    {
                        Text = $"{potionPower:0.##}x",
                        TextColor = new Color(140, 220, 140)
                    }
                }
            });
        }

        return statsPanel;
    }

    private HorizontalStackPanel? CreateUsageInfoPanel(Item item)
    {
        var handler = item.PotionHandler;
        if (handler == null) return null;

        var usagePanel = new HorizontalStackPanel
        {
            Spacing = 16,
            Margin = new Thickness(0, 8, 0, 0)
        };

        // Combat usage indicator
        var combatIcon = new Image
        {
            Width = 20,
            Height = 20,
            Background = handler.CanUseInCombat
                ? Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Icon.Checkmark]
                : Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Icon.X],
            Color = handler.CanUseInCombat ? new Color(120, 220, 120) : new Color(160, 100, 100)
        };
        var combatLabel = new Label(BaseContent.Styles.Label.Small)
        {
            Text = "Combat",
            TextColor = handler.CanUseInCombat ? new Color(120, 220, 120) : new Color(160, 100, 100),
            VerticalAlignment = VerticalAlignment.Center
        };
        usagePanel.Widgets.Add(new HorizontalStackPanel
        {
            Spacing = 6,
            Widgets = { combatIcon, combatLabel }
        });

        // Out of combat usage indicator
        var outsideIcon = new Image
        {
            Width = 20,
            Height = 20,
            Background = handler.CanUseOutsideCombat
                ? Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Icon.Checkmark]
                : Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Icon.X],
            Color = handler.CanUseOutsideCombat ? new Color(120, 220, 120) : new Color(160, 100, 100)
        };
        var outsideLabel = new Label(BaseContent.Styles.Label.Small)
        {
            Text = "World",
            TextColor = handler.CanUseOutsideCombat ? new Color(120, 220, 120) : new Color(160, 100, 100),
            VerticalAlignment = VerticalAlignment.Center
        };
        usagePanel.Widgets.Add(new HorizontalStackPanel
        {
            Spacing = 6,
            Widgets = { outsideIcon, outsideLabel }
        });

        var triggerText = item.PotionTrigger?.Describe() ?? "No combat trigger";
        usagePanel.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
        {
            Text = triggerText,
            TextColor = item.PotionTrigger != null ? new Color(120, 220, 120) : new Color(160, 100, 100),
            VerticalAlignment = VerticalAlignment.Center
        });

        return usagePanel;
    }

    private HorizontalStackPanel? CreateButtonsPanel(Item item, BaseGui gui)
    {
        var handler = item.PotionHandler;
        if (handler == null || !handler.CanUseOutsideCombat) return null;

        var buttonsPanel = new HorizontalStackPanel
        {
            Spacing = 12,
            Margin = new Thickness(0, 12, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Center
        };

        // Use button for outside combat
        var useButton = new CursorButton(BaseContent.Styles.Button.Normal)
        {
            Content = new Label(BaseContent.Styles.Label.Normal)
            {
                Text = "Use Now",
                VerticalAlignment = VerticalAlignment.Center
            },
            Padding = new Thickness(16, 8)
        };

        useButton.Click += (_, _) =>
        {
            var result = handler.UseOutsideCombat(Core.Context.PlayerPawn);
            if (result.Success)
            {
                Core.Context.Achievements.OnItemUsed(Core.Context.PlayerPawn, item);
                item.StackSize--;
                if (item.StackSize < 1)
                {
                    item.Destroy();
                    gui.CloseEntityWindow();
                }
                else
                {
                    UpdateStackLabel();
                }
            }
        };

        buttonsPanel.Widgets.Add(useButton);
        return buttonsPanel;
    }

    private void UpdateStackLabel()
    {
        if (_stackLabel.Visible || _item.StackSize > 1)
        {
            _stackLabel.Text = $"Quantity: /c[{TC.Golden}]{_item.StackSize}";
            _stackLabel.Visible = _item.StackSize > 1;
        }
    }

    public override void Update()
    {
        if (_item.IsDestroyed) return;
        UpdateStackLabel();
    }
}
