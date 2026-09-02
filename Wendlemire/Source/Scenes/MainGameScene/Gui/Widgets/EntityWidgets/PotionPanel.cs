namespace Wendlemire.Scenes.MainGameScene.Gui.Widgets.EntityWidgets;

public sealed class PotionPanel : EntityPanelBase
{
    private readonly Item _item;
    private readonly Label _stackLabel;

    public PotionPanel(BaseGui gui, Item item, EntityPanelProperties? properties = null) : base(gui, item, properties)
    {
        _item = item;
        EntityCardChrome.ApplyCard(this, 340);

        Widgets.Add(EntityCardChrome.Header(item, GetPotionTitleColor(item)));

        var effectDescription = GetEffectDescription(item);
        if (!string.IsNullOrEmpty(effectDescription))
        {
            Widgets.Add(EntityCardChrome.SectionLabel("Effect"));
            Widgets.Add(EntityCardChrome.BodyLabel(effectDescription, EntityCardChrome.Effect));
        }

        var statsPanel = CreateStatsPanel(item);
        if (statsPanel != null)
        {
            Widgets.Add(statsPanel);
        }

        if (item.IsStackable && item.StackSize > 1)
        {
            _stackLabel = new Label("small")
            {
                Text = $"Quantity: /c[{TC.Golden}]{item.StackSize}"
            };
            Widgets.Add(_stackLabel);
        }
        else
        {
            _stackLabel = new Label("small") { Visible = false };
        }

        var triggerText = item.PotionTrigger?.Describe();
        if (!string.IsNullOrWhiteSpace(triggerText))
        {
            Widgets.Add(EntityCardChrome.BodyLabel(triggerText, EntityCardChrome.Effect));
        }

        var buttonsPanel = CreateButtonsPanel(item, gui);
        if (buttonsPanel != null)
        {
            Widgets.Add(buttonsPanel);
        }
    }

    private static bool IsHealingPotion(Item item) =>
        item.Def == Defs.Items.HealingPotion
        || item.Def == Defs.Items.HealingFlask
        || item.Def == Defs.Items.HealingSalve;

    private Color GetPotionTitleColor(Item item)
    {
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
        if (item.PotionHandler != null)
        {
            return item.PotionHandler.GetEffectDescription();
        }

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

        var statsPanel = new VerticalStackPanel { Spacing = 2 };
        statsPanel.Widgets.Add(EntityCardChrome.SectionLabel("Stats"));
        statsPanel.Widgets.Add(EntityCardChrome.StatRow("Duration", $"{potionDuration} ticks", new Color(100, 180, 255)));

        var potionPower = item.GetStatValue(Defs.Stats.PotionPower);
        if (potionPower > 0 && potionPower != 1)
        {
            statsPanel.Widgets.Add(EntityCardChrome.StatRow("Power", $"{potionPower:0.##}x", EntityCardChrome.Effect));
        }

        return statsPanel;
    }

    private HorizontalStackPanel? CreateButtonsPanel(Item item, BaseGui gui)
    {
        var handler = item.PotionHandler;
        if (handler == null || !handler.CanUseOutsideCombat) return null;

        var buttonsPanel = new HorizontalStackPanel
        {
            Spacing = 8,
            Margin = new Thickness(0, 4, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Center
        };

        var useButton = new CursorButton(BaseContent.Styles.Button.Small)
        {
            Content = new Label("small")
            {
                Text = "Use Now",
                VerticalAlignment = VerticalAlignment.Center
            },
            Padding = new Thickness(10, 4)
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
