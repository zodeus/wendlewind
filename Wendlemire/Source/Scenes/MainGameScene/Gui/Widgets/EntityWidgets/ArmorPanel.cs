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
        EntityCardChrome.BeginInspect(this, item);

        var durabilitySection = new VerticalStackPanel { Spacing = 2 };

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

        var chips = new List<(string Key, string Value, Color Color)>
        {
            ("Slot", item.ItemDef.EquipmentProperties?.SlotUsedToEquip?.ToString() ?? "n/a", EntityCardChrome.Info)
        };

        var maxEnchantments = item.ItemDef.EquipmentProperties?.MaxEnchantments ?? 0;
        if (maxEnchantments > 0)
        {
            chips.Add(("Enchants", $"{maxEnchantments}", ColorExt.HexToColor(TC.Purple.TrimStart('#'))));
        }

        var armorSet = item.ItemDef.EquipmentProperties?.ArmorSet;
        if (!string.IsNullOrEmpty(armorSet))
        {
            chips.Add(("Set", SetBonuses.DisplayName(armorSet), EntityCardChrome.Gold));
        }

        Widgets.Add(EntityCardChrome.StatStrip(chips.ToArray()));

        if (!string.IsNullOrEmpty(armorSet) && SetBonuses.Table.TryGetValue(armorSet, out var tiers))
        {
            Widgets.Add(EntityCardChrome.SectionHeader("Set bonuses"));
            Widgets.Add(EntityCardChrome.MechanicsBlock(
                tiers.Select(tier => $"{tier.Pieces} pieces · {SetBonuses.DescribeTier(tier)}").ToList(),
                EntityCardChrome.Metrics(EntityCardChrome.InspectWidth).BodyWidth));
        }

        // ═══════════════════════════════════════════════════════════════════
        // Stats Section
        // ═══════════════════════════════════════════════════════════════════
        var displayableStats = item.Def.BaseStats
            .Where(s => s.Def != Defs.Stats.MaxDurability)
            .ToList();

        if (displayableStats.Count > 0)
        {
            Widgets.Add(EntityCardChrome.SectionHeader("Stats"));
            Widgets.Add(EntityCardChrome.StatStrip(displayableStats
                .Select(stat =>
                {
                    var value = item.GetStatValue(stat.Def);
                    var text = value % 1 == 0 ? $"{value:0}" : $"{value:0.##}";
                    return (stat.Def.Label, text, value >= 0 ? Color.LightGoldenrodYellow : Color.Salmon);
                })
                .ToArray()));
        }

        // ═══════════════════════════════════════════════════════════════════
        // Enchantments Section
        // ═══════════════════════════════════════════════════════════════════
        _socketsPanel = new ItemEnchantmentSocketsPanel(gui, item)
        {
            Margin = new Thickness(0, 4, 0, 0)
        };
        Widgets.Add(_socketsPanel);
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