using System.Globalization;

namespace Wendlewind.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets.BodyPartPanelWidget;

public sealed class BodyPartPanel : EntityPanelBase
{
    private readonly BodyPartPanelModifiersLabel _modifiersPanel;

    public BodyPartPanel(BaseGui gui, BodyPart bodyPart, EntityPanelProperties? properties = null) : base(gui, bodyPart, properties)
    {
        Padding = new Thickness(20);
        MinWidth = 300;

        _modifiersPanel = new BodyPartPanelModifiersLabel(bodyPart);

        VerticalStackPanel leftPanel = new() { Spacing = 5, MinWidth = 330 };
        leftPanel.Widgets.Add(new BodyPartPanelHealthLabel(bodyPart));
        leftPanel.Widgets.Add(_modifiersPanel);
        leftPanel.Widgets.Add(new BodyPartPanelBleedingLabel(bodyPart));
        leftPanel.Widgets.Add(new BodyPartPanelBrokenBonesLabel(bodyPart));
        leftPanel.Widgets.Add(new BodyPartPanelCrackedLabel(bodyPart));
        leftPanel.Widgets.Add(new BodyPartPanelMobilityLabel(bodyPart));
        leftPanel.Widgets.Add(new BodyPartPanelFunctionalLabel(bodyPart));
        leftPanel.Widgets.Add(new BodyPartPanelArteryLabel(bodyPart));
        leftPanel.Widgets.Add(new BodyPartPanelDestroyedLabel(bodyPart));
        
        VerticalStackPanel centerPanel = new() { Spacing = 5, MinWidth = 330 };
        RegisterAttachedParts(gui, centerPanel, bodyPart);
            
        var rightPanel = new VerticalStackPanel { Spacing = 5, MinWidth = 450};
        rightPanel.Widgets.Add(new Label(BaseContent.Styles.Label.Small) { Text = bodyPart.Def.Description, Wrap = true });
        rightPanel.Widgets.Add(new Label(BaseContent.Styles.Label.Small) { Text = $"Short Label: {bodyPart.LabelShort}" });
        rightPanel.Widgets.Add(new Label(BaseContent.Styles.Label.Small) { Text = $"Type: {bodyPart.Type}" });
        rightPanel.Widgets.Add(new Label(BaseContent.Styles.Label.Small) { Text = $"Size: {bodyPart.BloodAmount}" });
        rightPanel.Widgets.Add(new Label(BaseContent.Styles.Label.Small) { Text = $"Attack Speed Mod: {bodyPart.AttackSpeedModifier}" });
        rightPanel.Widgets.Add(new Label(BaseContent.Styles.Label.Small) { Text = $"Position: {bodyPart.Position}" });
        rightPanel.Widgets.Add(new Label(BaseContent.Styles.Label.Small) { Text = $"Is External: {bodyPart.IsExternal}" });
        rightPanel.Widgets.Add(new Label(BaseContent.Styles.Label.Small) { Text = $"Attached To: {bodyPart.Socket?.Label ?? "n/a"}" });
        rightPanel.Widgets.Add(new Label(BaseContent.Styles.Label.Small) { Text = $"Has Bones: {bodyPart.HasBones}" });
        rightPanel.Widgets.Add(new HorizontalSeparator { Margin = new Thickness(0, 15, 0, 15) });
        
        // Equipment Slots Section
        rightPanel.Widgets.Add(new BodyPartEquipmentSlotsWidget(bodyPart));
        
        // Equipped Items Section
        rightPanel.Widgets.Add(new BodyPartEquippedItemsWidget(bodyPart));
        
        rightPanel.Widgets.Add(new HorizontalSeparator { Margin = new Thickness(0, 15, 0, 15) });
        rightPanel.Widgets.Add(new Label(BaseContent.Styles.Label.Small) { Text = $"Substance: {bodyPart.Substance}" });
        rightPanel.Widgets.Add(new Label(BaseContent.Styles.Label.Small) { Text = $"Is Severed: {bodyPart.IsSevered}" });
        rightPanel.Widgets.Add(new Label(BaseContent.Styles.Label.Small) { Text = $"Is Cracked: {bodyPart.IsCracked}" });
        rightPanel.Widgets.Add(new Label(BaseContent.Styles.Label.Small) { Text = $"Is Vital: {bodyPart.IsVital}" });
        rightPanel.Widgets.Add(new Label(BaseContent.Styles.Label.Small) { Text = $"Ticks Since Last Hit: {bodyPart.TicksSinceLastHit}" });
        rightPanel.Widgets.Add(new HorizontalSeparator { Margin = new Thickness(0, 15, 0, 15) });
        foreach (var baseStat in bodyPart.Def.BaseStats)
        {
            var row = new HorizontalStackPanel { Spacing = 10 };
            row.Widgets.Add(new Label(BaseContent.Styles.Label.Small) { Text = $"{baseStat.Def.Label}:" });
            row.Widgets.Add(new Label(BaseContent.Styles.Label.Small) { Text = bodyPart.GetStatValue(baseStat.Def).ToString(CultureInfo.InvariantCulture) });
            rightPanel.Widgets.Add(row);
        }
        
        Widgets.Add(new HorizontalStackPanel
        {
            Spacing = 30, Widgets =
            {
                leftPanel, centerPanel, rightPanel
            }
        });
    }

    private void RegisterAttachedParts(BaseGui gui, VerticalStackPanel panel, BodyPart bodyPart)
    {
        if (bodyPart.Socket?.ParentPart != null)
        {
            var partPanel = new BodyPartPanelPartLabel(gui, bodyPart.Socket.ParentPart);
            panel.Widgets.Add(new Label { Text = "Parent", Margin = new Thickness(0, 20, 0, 0) });
            panel.Widgets.Add(new HorizontalSeparator { Margin = new Thickness(0, 5, 0, 5) });
            panel.Widgets.Add(partPanel);
        }

        if (bodyPart.ExternalParts.Any())
        {
            panel.Widgets.Add(new Label { Text = "External Parts", Margin = new Thickness(0, 20, 0, 0) });
            panel.Widgets.Add(new HorizontalSeparator { Margin = new Thickness(0, 5, 0, 5) });

            foreach (var externalPart in bodyPart.ExternalParts)
            {
                var partPanel = new BodyPartPanelPartLabel(gui, externalPart)
                {
                    Margin = new Thickness(0, 0, 0, 0)
                };
                panel.Widgets.Add(partPanel);
            }
        }

        if (bodyPart.InternalParts.Any())
        {
            panel.Widgets.Add(new Label { Text = "Internal Parts", Margin = new Thickness(0, 20, 0, 0) });
            panel.Widgets.Add(new HorizontalSeparator { Margin = new Thickness(0, 5, 0, 5) });
            foreach (var internalPart in bodyPart.InternalParts)
            {
                var partPanel = new BodyPartPanelPartLabel(gui, internalPart)
                {
                    Margin = new Thickness(0, 0, 0, 0)
                };
                panel.Widgets.Add(partPanel);
            }
        }
    }

    public override void Update()
    {
        _modifiersPanel.Update();
    }
}

public sealed class BodyPartPanelModifiersLabel : VerticalStackPanel
{
    private Dictionary<BodyPartModifier, Widget> _widgets = new();

    public BodyPartPanelModifiersLabel(BodyPart bodyPart)
    {
        Spacing = 5;
        foreach (var modifier in bodyPart.Modifiers)
        {
            var widget = CreateModifierWidget(modifier);
            Widgets.Add(widget);
            _widgets.Add(modifier, widget);
        }

        bodyPart.ModifiersChanged += OnModifiersChanged;
    }

    private static Widget CreateModifierWidget(BodyPartModifier modifier)
    {
        // Header panel with label
        var headerPanel = new Panel
        {
            Padding = new Thickness(12),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Background = new ColoredRegion(Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.SimpleWhite], modifier.Def.Color)
        };

        var headerLabel = new Label
        {
            Text = GetLabelText(modifier),
            TextColor = modifier.Def.Color,
            Tag = "headerLabel"
        };

        headerPanel.Widgets.Add(headerLabel);

        // Add tooltip with info panel if available
        headerPanel.WithTooltip(() => modifier.GetInfoPanel() ?? new Label(BaseContent.Styles.Label.Small)
        {
            Text = modifier.Def.Description,
            TextColor = Color.GhostWhite
        });

        return headerPanel;
    }

    private void OnModifiersChanged(BodyPartModifier mod, BodyPartModifierEventType type)
    {
        switch (type)
        {
            case BodyPartModifierEventType.Added:
                var widget = CreateModifierWidget(mod);
                Widgets.Add(widget);
                _widgets.Add(mod, widget);
                break;
            case BodyPartModifierEventType.Removed:
                _widgets[mod].RemoveFromParent();
                _widgets.Remove(mod);
                break;
        }
    }

    private static string GetLabelText(BodyPartModifier modifier)
    {
        var timeRemaining = modifier.DurationInTicks == 0 ? "\u221e" : modifier.TicksRemaining + "t";
        return $"{modifier.Label} {timeRemaining}";
    }

    public void Update()
    {
        foreach (var (modifier, widget) in _widgets)
        {
            // Find the header label within the panel and update its text
            if (widget is Panel panel)
            {
                var headerLabel = panel.Widgets.FirstOrDefault(w => w.Tag as string == "headerLabel") as Label;
                if (headerLabel != null)
                {
                    headerLabel.Text = GetLabelText(modifier);
                }
            }
        }
    }
}

/// <summary>
/// Displays available equipment slots for a body part with styled badges.
/// </summary>
public sealed class BodyPartEquipmentSlotsWidget : VerticalStackPanel
{
    private static readonly Color SlotBadgeColor = new(60, 75, 90);
    private static readonly Color SlotTextColor = new(180, 200, 220);
    
    public BodyPartEquipmentSlotsWidget(BodyPart bodyPart)
    {
        Spacing = 8;
        
        var headerLabel = new Label(BaseContent.Styles.Label.Small)
        {
            Text = "Equipment Slots",
            TextColor = BaseContent.Colors.Text.Golden
        };
        Widgets.Add(headerLabel);
        
        var slots = bodyPart.EquipmentSlots?.ToList() ?? [];
        if (slots.Count == 0)
        {
            Widgets.Add(new Label(BaseContent.Styles.Label.Small)
            {
                Text = "None",
                TextColor = new Color(120, 120, 120)
            });
            return;
        }
        
        var slotsContainer = new HorizontalStackPanel { Spacing = 6 };
        foreach (var slot in slots)
        {
            var badge = CreateSlotBadge(slot);
            slotsContainer.Widgets.Add(badge);
        }
        Widgets.Add(slotsContainer);
    }
    
    private static Panel CreateSlotBadge(EquipmentSlotType slot)
    {
        var slotName = GetFriendlySlotName(slot);
        var slotColor = GetSlotColor(slot);
        var iconKey = GetSlotIconAtlasKey(slot);
        
        var badge = new Panel
        {
            Background = new ColoredRegion(Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.SimpleWhite], slotColor),
            Padding = new Thickness(8, 4),
        };
        
        var content = new HorizontalStackPanel { Spacing = 6, VerticalAlignment = VerticalAlignment.Center };
        
        // Add icon if available for this slot type
        if (iconKey != null)
        {
            var icon = new Image
            {
                Background = Stylesheet.Current.Atlas[iconKey],
                Width = 16,
                Height = 16,
                VerticalAlignment = VerticalAlignment.Center
            };
            content.Widgets.Add(icon);
        }
        
        var label = new Label(BaseContent.Styles.Label.Small)
        {
            Text = slotName,
            TextColor = SlotTextColor,
            VerticalAlignment = VerticalAlignment.Center
        };
        content.Widgets.Add(label);
        
        badge.Widgets.Add(content);
        
        return badge;
    }
    
    private static string GetFriendlySlotName(EquipmentSlotType slot) => slot switch
    {
        EquipmentSlotType.HandWeapon => "Weapon",
        EquipmentSlotType.HandArmor => "Glove",
        EquipmentSlotType.FootWeapon => "Foot Weapon",
        EquipmentSlotType.FootArmor => "Boot",
        EquipmentSlotType.LegArmor => "Leg Armor",
        EquipmentSlotType.ArmArmor => "Arm Armor",
        EquipmentSlotType.TorsoArmor => "Torso Armor",
        EquipmentSlotType.NeckArmor => "Neck Armor",
        EquipmentSlotType.HeadArmor => "Helmet",
        EquipmentSlotType.PotionSlot1 => "Potion 1",
        EquipmentSlotType.PotionSlot2 => "Potion 2",
        EquipmentSlotType.Bag => "Bag",
        EquipmentSlotType.Cloak => "Cloak",
        EquipmentSlotType.Necklace => "Necklace",
        EquipmentSlotType.BuiltIn => "Built-In",
        _ => slot.ToString()
    };
    
    private static string? GetSlotIconAtlasKey(EquipmentSlotType slot) => slot switch
    {
        EquipmentSlotType.PotionSlot1 or EquipmentSlotType.PotionSlot2 => BaseContent.Styles.Atlas.Icon.PotionSlot,
        EquipmentSlotType.Bag => BaseContent.Styles.Atlas.Icon.BagSlot,
        _ => null
    };
    
    private static Color GetSlotColor(EquipmentSlotType slot) => slot switch
    {
        EquipmentSlotType.HandWeapon or EquipmentSlotType.FootWeapon => new Color(90, 50, 50),
        EquipmentSlotType.PotionSlot1 or EquipmentSlotType.PotionSlot2 => new Color(50, 80, 50),
        EquipmentSlotType.BuiltIn => new Color(70, 70, 50),
        _ => SlotBadgeColor
    };
}

/// <summary>
/// Displays equipped items for a body part with icons and durability bars.
/// </summary>
public sealed class BodyPartEquippedItemsWidget : VerticalStackPanel
{
    public BodyPartEquippedItemsWidget(BodyPart bodyPart)
    {
        Spacing = 8;
        Margin = new Thickness(0, 10, 0, 0);
        
        var headerLabel = new Label(BaseContent.Styles.Label.Small)
        {
            Text = "Equipped Items",
            TextColor = BaseContent.Colors.Text.Golden
        };
        Widgets.Add(headerLabel);
        
        var equippedItems = bodyPart.Equipment.Values.Where(i => i != null).ToList();
        if (equippedItems.Count == 0)
        {
            Widgets.Add(new Label(BaseContent.Styles.Label.Small)
            {
                Text = "Nothing equipped",
                TextColor = new Color(120, 120, 120)
            });
            return;
        }
        
        foreach (var item in equippedItems)
        {
            if (item == null) continue;
            var itemRow = CreateItemRow(item);
            Widgets.Add(itemRow);
        }
    }
    
    private static HorizontalStackPanel CreateItemRow(Item item)
    {
        var row = new HorizontalStackPanel
        {
            Spacing = 10,
            VerticalAlignment = VerticalAlignment.Center
        };
        
        // Item icon with frame
        var iconFrame = new Panel
        {
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.IconFrame],
            Width = 36,
            Height = 36
        };
        var icon = new Image
        {
            Background = item.GetIconImage(),
            Width = 32,
            Height = 32,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        iconFrame.Widgets.Add(icon);
        row.Widgets.Add(iconFrame);
        
        // Item info column
        var infoColumn = new VerticalStackPanel { Spacing = 2 };
        
        // Item name with rarity color
        var nameLabel = new Label(BaseContent.Styles.Label.Small)
        {
            Text = item.Label,
            TextColor = GetItemRarityColor(item)
        };
        infoColumn.Widgets.Add(nameLabel);
        
        // Durability bar (if item has durability)
        if (item.MaxDurability > 0)
        {
            var durabilityContainer = new HorizontalStackPanel { Spacing = 5 };
            
            var durabilityBar = new HorizontalProgressBar(BaseContent.Styles.Bar.Durability)
            {
                Width = 80,
                Height = 12,
                Value = item.Durability / item.MaxDurability * 100f
            };
            durabilityContainer.Widgets.Add(durabilityBar);
            
            var durabilityText = new Label(BaseContent.Styles.Label.Small)
            {
                Text = $"{item.Durability:0}/{item.MaxDurability:0}",
                TextColor = GetDurabilityColor(item),
                VerticalAlignment = VerticalAlignment.Center
            };
            durabilityContainer.Widgets.Add(durabilityText);
            
            infoColumn.Widgets.Add(durabilityContainer);
        }
        
        row.Widgets.Add(infoColumn);
        
        return row;
    }
    
    private static Color GetItemRarityColor(Item item)
    {
        // Could be extended to check item rarity/quality
        if (item.Enchantments?.Any() == true)
        {
            return new Color(160, 120, 255); // Purple for enchanted
        }
        return Color.White;
    }
    
    private static Color GetDurabilityColor(Item item)
    {
        var percent = item.Durability / item.MaxDurability;
        return percent switch
        {
            < 0.25f => new Color(255, 80, 80),   // Red - critical
            < 0.50f => new Color(255, 180, 80),  // Orange - low
            < 0.75f => new Color(255, 255, 120), // Yellow - moderate
            _ => new Color(120, 255, 120)        // Green - good
        };
    }
}