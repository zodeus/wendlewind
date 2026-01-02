using System.Globalization;

namespace Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets;

[UsedImplicitly]
public sealed class WeaponPanel : EntityPanelBase
{
    private readonly Item _item;
    private readonly Label _durabilityLabel;
    private readonly HorizontalProgressBar _durabilityBar;
    private readonly ItemEnchantmentSocketsPanel _socketsPanel;

    public WeaponPanel(BaseGui gui, Item item, EntityPanelProperties? properties = null) : base(gui, item, properties)
    {
        _item = item;
        Padding = new Thickness(20);
        MinWidth = 380;
        Spacing = 4;

        // ═══════════════════════════════════════════════════════════════════
        // Header Section: Icon + Description
        // ═══════════════════════════════════════════════════════════════════
        var headerSection = new HorizontalStackPanel { Spacing = 15, Margin = new Thickness(0, 0, 0, 12) };

        // Icon with frame
        var iconFrame = new Panel
        {
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.DeepGold],
            Padding = new Thickness(4),
            Width = 80, Height = 80
        };
        iconFrame.Widgets.Add(new Image
        {
            Background = new TextureRegion(item.Icon),
            Width = 72, Height = 72,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        });
        headerSection.Widgets.Add(iconFrame);

        // Description area
        var descArea = new VerticalStackPanel { Spacing = 6, VerticalAlignment = VerticalAlignment.Center };
        if (item.Def.Description != "undefined")
        {
            descArea.Widgets.Add(new Label(BaseContent.Styles.Label.Normal)
            {
                Text = item.Def.Description, Wrap = true, MaxWidth = 280
            });
        }
        headerSection.Widgets.Add(descArea);
        Widgets.Add(headerSection);

        // ═══════════════════════════════════════════════════════════════════
        // Durability Section
        // ═══════════════════════════════════════════════════════════════════
        var durabilitySection = new VerticalStackPanel { Spacing = 4, Margin = new Thickness(0, 0, 0, 12) };

        _durabilityBar = new HorizontalProgressBar(BaseContent.Styles.Bar.Durability)
        {
            Width = 160, Height = 18,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        durabilitySection.Widgets.Add(_durabilityBar);

        _durabilityLabel = new Label("small")
        {
            Text = $"Durability: {item.Durability}/{item.MaxDurability}",
            TextColor = Color.LightGray
        };
        durabilitySection.Widgets.Add(_durabilityLabel);
        Widgets.Add(durabilitySection);

        // ═══════════════════════════════════════════════════════════════════
        // Weapon Properties Section
        // ═══════════════════════════════════════════════════════════════════
        var propsSection = new VerticalStackPanel { Spacing = 3, Margin = new Thickness(0, 0, 0, 10) };
        propsSection.Widgets.Add(CreatePropertyRow("Weapon Type", $"{item.ItemDef.WeaponProperties?.WeaponType}", TC.Golden));
        propsSection.Widgets.Add(CreatePropertyRow("Damage Type", $"{item.ItemDef.WeaponProperties?.DamageType}", TC.Red));
        propsSection.Widgets.Add(CreatePropertyRow("Slot", item.ItemDef.EquipmentProperties?.SlotUsedToEquip?.ToString() ?? "n/a", TC.Blue));
        Widgets.Add(propsSection);

        // ═══════════════════════════════════════════════════════════════════
        // Substance Modifiers Section
        // ═══════════════════════════════════════════════════════════════════
        var substanceModifiers = item.ItemDef.WeaponProperties?.SubstanceModifiers;
        if (substanceModifiers is { Count: > 0 })
        {
            var rawMeleePower = item.GetStatValue(Defs.Stats.MeleePower);
            var weaponType = item.ItemDef.WeaponProperties?.WeaponType;
            var skillLevel = weaponType != null ? Core.Context?.PlayerPawn?.GetSkill(weaponType.Value)?.Level ?? 0 : 0;
            var skillPower = 1 + (skillLevel * 0.1f);
            var baseMeleePower = rawMeleePower * skillPower;

            var modsSection = new VerticalStackPanel { Spacing = 2, Margin = new Thickness(0, 0, 0, 10) };
            modsSection.Widgets.Add(new Label("small")
            {
                Text = "Substance Modifiers",
                TextColor = BaseContent.Colors.Text.Golden,
                Margin = new Thickness(0, 0, 0, 4)
            });

            var modsGrid = new Grid { ColumnSpacing = 12, RowSpacing = 2, Margin = new Thickness(8, 0, 0, 0) };
            modsGrid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
            modsGrid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
            modsGrid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));

            var row = 0;
            foreach (var mod in substanceModifiers)
            {
                var isPositive = mod.Modifier >= 1f;
                var modText = isPositive
                    ? $"+{((mod.Modifier - 1f) * 100f):0}%"
                    : $"-{((1f - mod.Modifier) * 100f):0}%";
                var modColor = isPositive ? Color.LimeGreen : Color.Salmon;

                var substanceLabel = new Label("small") { Text = $"{mod.Substance}:" };
                Grid.SetRow(substanceLabel, row);
                Grid.SetColumn(substanceLabel, 0);
                modsGrid.Widgets.Add(substanceLabel);

                var valueLabel = new Label("small") { Text = modText, TextColor = modColor };
                Grid.SetRow(valueLabel, row);
                Grid.SetColumn(valueLabel, 1);
                modsGrid.Widgets.Add(valueLabel);

                var actualDamage = baseMeleePower * mod.Modifier;
                var damageLabel = new Label("small")
                {
                    Text = $"({actualDamage:0})",
                    TextColor = Color.DarkGoldenrod
                };
                Grid.SetRow(damageLabel, row);
                Grid.SetColumn(damageLabel, 2);
                modsGrid.Widgets.Add(damageLabel);

                row++;
            }
            modsSection.Widgets.Add(modsGrid);
            Widgets.Add(modsSection);
        }

        // ═══════════════════════════════════════════════════════════════════
        // Stats Section
        // ═══════════════════════════════════════════════════════════════════
        if (item.Def.BaseStats.Count > 0)
        {
            // Calculate skill power for Melee Power display
            var statsWeaponType = item.ItemDef.WeaponProperties?.WeaponType;
            var statsSkillLevel = statsWeaponType != null ? Core.Context?.PlayerPawn?.GetSkill(statsWeaponType.Value)?.Level ?? 0 : 0;
            var statsSkillPower = 1 + (statsSkillLevel * 0.1f);

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
            foreach (var baseStat in item.Def.BaseStats)
            {
                var keyLabel = new Label("small") { Text = $"{baseStat.Def.Label}:" };
                Grid.SetRow(keyLabel, row);
                Grid.SetColumn(keyLabel, 0);
                statsGrid.Widgets.Add(keyLabel);

                // Apply skill power multiplier to Melee Power
                var statValue = item.GetStatValue(baseStat.Def);
                if (baseStat.Def == Defs.Stats.MeleePower)
                {
                    statValue *= statsSkillPower;
                }

                var valueLabel = new Label("small")
                {
                    Text = $"{statValue:0}",
                    TextColor = Color.LightGoldenrodYellow
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
        // Strip the leading # if present
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
        _durabilityBar.Value = _item.Durability / _item.MaxDurability * 100;
        _durabilityLabel.Text = $"Durability: {_item.Durability}/{_item.MaxDurability}";
        _socketsPanel.Update();
    }
}