namespace Wendlemire.Scenes.MainGameScene.Gui.Widgets.EntityWidgets;

[UsedImplicitly]
public sealed class ArmorPanel : EntityPanelBase
{
    private readonly Item _item;
    private readonly Label _durabilityLabel;
    private readonly HorizontalProgressBar _durabilityBar;
    private readonly ItemEnchantmentSocketsPanel _socketsPanel;

    public ArmorPanel(BaseGui gui, Item item, EntityPanelProperties? properties = null) : base(gui, item, properties)
    {
        _item = item;
        EntityCardChrome.ApplyCard(this, 340);
        Widgets.Add(EntityCardChrome.Header(item));

        // ═══════════════════════════════════════════════════════════════════
        // Durability Section
        // ═══════════════════════════════════════════════════════════════════
        var durabilitySection = new VerticalStackPanel { Spacing = 2, Margin = new Thickness(0, 2, 0, 4) };

        _durabilityBar = new HorizontalProgressBar(BaseContent.Styles.Bar.Durability)
        {
            Width = 160, Height = 18,
            Minimum = 0,
            Maximum = 100,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        durabilitySection.Widgets.Add(_durabilityBar);

        _durabilityLabel = new Label("small")
        {
            TextColor = Color.LightGray
        };
        RefreshDurability();
        durabilitySection.Widgets.Add(_durabilityLabel);
        Widgets.Add(durabilitySection);

        // ═══════════════════════════════════════════════════════════════════
        // Armor Properties Section
        // ═══════════════════════════════════════════════════════════════════
        var propsSection = new VerticalStackPanel { Spacing = 1, Margin = new Thickness(0, 0, 0, 4) };
        propsSection.Widgets.Add(CreatePropertyRow("Slot", item.ItemDef.EquipmentProperties?.SlotUsedToEquip?.ToString() ?? "n/a", TC.Blue));

        var maxEnchantments = item.ItemDef.EquipmentProperties?.MaxEnchantments ?? 0;
        if (maxEnchantments > 0)
        {
            propsSection.Widgets.Add(CreatePropertyRow("Enchant Slots", $"{maxEnchantments}", TC.Purple));
        }

        var armorSet = item.ItemDef.EquipmentProperties?.ArmorSet;
        if (!string.IsNullOrEmpty(armorSet))
        {
            propsSection.Widgets.Add(CreatePropertyRow("Set", SetBonuses.DisplayName(armorSet), TC.Golden));
            if (SetBonuses.Table.TryGetValue(armorSet, out var tiers))
            {
                foreach (var tier in tiers)
                {
                    propsSection.Widgets.Add(CreatePropertyRow(
                        $"{tier.Pieces}",
                        SetBonuses.DescribeTier(tier),
                        TC.Golden));
                }
            }
        }

        Widgets.Add(propsSection);

        // ═══════════════════════════════════════════════════════════════════
        // Stats Section
        // ═══════════════════════════════════════════════════════════════════
        var displayableStats = item.Def.BaseStats
            .Where(s => s.Def != Defs.Stats.MaxDurability)
            .ToList();

        if (displayableStats.Count > 0)
        {
            var statsSection = new VerticalStackPanel { Spacing = 2, Margin = new Thickness(0, 0, 0, 10) };
            statsSection.Widgets.Add(new Label("small")
            {
                Text = "Stats",
                TextColor = BaseContent.Colors.Text.Golden,
                Margin = new Thickness(0, 0, 0, 4)
            });

            var statsGrid = new Grid { ColumnSpacing = 12, RowSpacing = 2, Margin = new Thickness(8, 0, 0, 0) };
            statsGrid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
            statsGrid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));

            var row = 0;
            foreach (var baseStat in displayableStats)
            {
                var keyLabel = new Label("small") { Text = $"{baseStat.Def.Label}:" };
                Grid.SetRow(keyLabel, row);
                Grid.SetColumn(keyLabel, 0);
                statsGrid.Widgets.Add(keyLabel);

                var statValue = item.GetStatValue(baseStat.Def);
                var valueText = statValue % 1 == 0 ? $"{statValue:0}" : $"{statValue:0.##}";

                // Color positive stats green, negative red
                var valueColor = statValue >= 0 ? Color.LightGoldenrodYellow : Color.Salmon;

                var valueLabel = new Label("small")
                {
                    Text = valueText,
                    TextColor = valueColor
                };

                Grid.SetRow(valueLabel, row);
                Grid.SetColumn(valueLabel, 1);
                statsGrid.Widgets.Add(valueLabel);

                row++;
            }
            statsSection.Widgets.Add(statsGrid);
            Widgets.Add(statsSection);
        }

        // ═══════════════════════════════════════════════════════════════════
        // Enchantments Section
        // ═══════════════════════════════════════════════════════════════════
        _socketsPanel = new ItemEnchantmentSocketsPanel(gui, item)
        {
            Margin = new Thickness(0, 5, 0, 0)
        };
        Widgets.Add(_socketsPanel);
    }

    private static HorizontalStackPanel CreatePropertyRow(string key, string value, string valueColorHex)
    {
        var hex = valueColorHex.StartsWith('#') ? valueColorHex[1..] : valueColorHex;
        var color = ColorExt.HexToColor(hex);
        return new HorizontalStackPanel
        {
            Spacing = 8,
            Widgets =
            {
                new Label("small") { Text = $"{key}:", TextColor = Color.Gray },
                new Label("small") { Text = value, TextColor = color }
            }
        };
    }

    public override void Update()
    {
        RefreshDurability();
        _socketsPanel.Update();
    }

    private void RefreshDurability()
    {
        var max = Math.Max(1f, _item.MaxDurability);
        _durabilityBar.Value = _item.Durability / max * 100f;
        _durabilityLabel.Text = $"Durability: {_item.Durability:0}/{_item.MaxDurability:0}";
    }
}