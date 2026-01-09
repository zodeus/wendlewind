using System.Globalization;
using Grafted.Sim.Entities.Items.Weapons;

namespace Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets;

[UsedImplicitly]
public sealed class WeaponPanel : EntityPanelBase
{
    private readonly Item _item;
    private readonly Label _durabilityLabel;
    private readonly HorizontalProgressBar _durabilityBar;
    private readonly ItemEnchantmentSocketsPanel _socketsPanel;
    private readonly Widget? _customInfoWidget;
    private readonly WeaponHandler? _weaponHandler;

    public WeaponPanel(BaseGui gui, Item item, EntityPanelProperties? properties = null) : base(gui, item, properties)
    {
        _item = item;
        _weaponHandler = item.WeaponHandler;
        Padding = new Thickness(20);
        MinWidth = 580;
        Spacing = 4;

        // ═══════════════════════════════════════════════════════════════════
        // Main Two-Column Layout
        // ═══════════════════════════════════════════════════════════════════
        var mainLayout = new HorizontalStackPanel { Spacing = 20 };
        
        // ─────────────────────────────────────────────────────────────────────
        // LEFT COLUMN: Icon + Sockets, Description, Info Widget
        // ─────────────────────────────────────────────────────────────────────
        var leftColumn = new VerticalStackPanel { Spacing = 10, MinWidth = 300 };
        
        // Icon row: Icon + Enchantment Sockets
        var iconRow = new HorizontalStackPanel { Spacing = 12, VerticalAlignment = VerticalAlignment.Center };
        
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
        iconRow.Widgets.Add(iconFrame);
        
        // Enchantment sockets next to icon
        _socketsPanel = new ItemEnchantmentSocketsPanel(gui, item)
        {
            VerticalAlignment = VerticalAlignment.Center
        };
        iconRow.Widgets.Add(_socketsPanel);
        
        leftColumn.Widgets.Add(iconRow);

        // Description (always shown for flavor text)
        if (item.Def.Description != "undefined")
        {
            leftColumn.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
            {
                Text = item.Def.Description, 
                Wrap = true, 
                MaxWidth = 280,
                TextColor = Color.LightGray
            });
        }
        
        // Custom info widget from handler (mechanics, stats, settings)
        _customInfoWidget = _weaponHandler?.CreateInfoWidget(gui);
        if (_customInfoWidget != null)
        {
            leftColumn.Widgets.Add(new HorizontalSeparator { Margin = new Thickness(0, 4, 0, 4) });
            leftColumn.Widgets.Add(_customInfoWidget);
        }
        
        mainLayout.Widgets.Add(leftColumn);
        mainLayout.Widgets.Add(new VerticalSeparator());
        
        // ─────────────────────────────────────────────────────────────────────
        // RIGHT COLUMN: Stats, Properties, Modifiers
        // ─────────────────────────────────────────────────────────────────────
        var rightColumn = new VerticalStackPanel { Spacing = 8 };
        
        // Durability Section
        var durabilitySection = new VerticalStackPanel { Spacing = 2 };

        _durabilityBar = new HorizontalProgressBar(BaseContent.Styles.Bar.Durability)
        {
            Width = 140, Height = 14,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        durabilitySection.Widgets.Add(_durabilityBar);

        _durabilityLabel = new Label("small")
        {
            Text = $"Durability: {item.Durability}/{item.MaxDurability}",
            TextColor = Color.LightGray
        };
        durabilitySection.Widgets.Add(_durabilityLabel);
        rightColumn.Widgets.Add(durabilitySection);

        // Weapon Properties Section
        var propsSection = new VerticalStackPanel { Spacing = 2 };
        propsSection.Widgets.Add(CreatePropertyRow("Type", $"{item.ItemDef.WeaponProperties?.WeaponType}", TC.Golden));
        propsSection.Widgets.Add(CreatePropertyRow("Damage", $"{item.ItemDef.WeaponProperties?.DamageType}", TC.Red));
        propsSection.Widgets.Add(CreatePropertyRow("Slot", item.ItemDef.EquipmentProperties?.SlotUsedToEquip?.ToString() ?? "n/a", TC.Blue));
        rightColumn.Widgets.Add(propsSection);

        // Substance Modifiers Section
        var substanceModifiers = item.ItemDef.WeaponProperties?.SubstanceModifiers;
        if (substanceModifiers is { Count: > 0 })
        {
            var rawWeaponPower = item.GetStatValue(Defs.Stats.WeaponPower);
            var weaponType = item.ItemDef.WeaponProperties?.WeaponType;
            var skillLevel = weaponType != null ? Core.Context?.PlayerPawn?.GetSkill(weaponType.Value)?.Level ?? 0 : 0;
            var skillPower = 1 + (skillLevel * 0.1f);
            var baseWeaponPower = rawWeaponPower * skillPower;

            var modsSection = new VerticalStackPanel { Spacing = 2 };
            modsSection.Widgets.Add(new Label("small")
            {
                Text = "Substance Modifiers",
                TextColor = BaseContent.Colors.Text.Golden,
                Margin = new Thickness(0, 0, 0, 2)
            });

            var modsGrid = new Grid { ColumnSpacing = 8, RowSpacing = 1, Margin = new Thickness(4, 0, 0, 0) };
            modsGrid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
            modsGrid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
            modsGrid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));

            var row = 0;
            foreach (var mod in substanceModifiers.OrderBy(m => m.Substance.ToString()))
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

                var actualDamage = baseWeaponPower * mod.Modifier;
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
            rightColumn.Widgets.Add(modsSection);
        }

        // Stats Section
        if (item.Def.BaseStats.Count > 0)
        {
            var statsWeaponType = item.ItemDef.WeaponProperties?.WeaponType;
            var statsSkillLevel = statsWeaponType != null ? Core.Context?.PlayerPawn?.GetSkill(statsWeaponType.Value)?.Level ?? 0 : 0;
            var statsSkillPower = 1 + (statsSkillLevel * 0.1f);

            var statsSection = new VerticalStackPanel { Spacing = 2 };
            statsSection.Widgets.Add(new Label("small")
            {
                Text = "Stats",
                TextColor = BaseContent.Colors.Text.Golden,
                Margin = new Thickness(0, 0, 0, 2)
            });

            var statsGrid = new Grid { ColumnSpacing = 8, RowSpacing = 1, Margin = new Thickness(4, 0, 0, 0) };
            statsGrid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
            statsGrid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));

            var row = 0;
            foreach (var baseStat in item.Def.BaseStats)
            {
                var keyLabel = new Label("small") { Text = $"{baseStat.Def.Label}:" };
                Grid.SetRow(keyLabel, row);
                Grid.SetColumn(keyLabel, 0);
                statsGrid.Widgets.Add(keyLabel);

                var statValue = item.GetStatValue(baseStat.Def);
                if (baseStat.Def == Defs.Stats.WeaponPower)
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
            rightColumn.Widgets.Add(statsSection);
        }

        // Modifiers Section (body part modifiers from weapon + enchantments)
        var bodyPartModifiers = (item.ItemDef.WeaponProperties?.BodyPartModifiers ?? [])
            .Concat(item.Enchantments?
                .Where(e => e.ItemDef.EnchantmentProperties != null)
                .SelectMany(e => e.ItemDef.EnchantmentProperties!.BodyPartModifiers) ?? [])
            .ToList();
        if (bodyPartModifiers.Count > 0)
        {
            var modsSection = new VerticalStackPanel { Spacing = 2 };
            modsSection.Widgets.Add(new Label("small")
            {
                Text = "Effects",
                TextColor = BaseContent.Colors.Text.Golden,
                Margin = new Thickness(0, 0, 0, 2)
            });

            var modsGrid = new Grid { ColumnSpacing = 8, RowSpacing = 1, Margin = new Thickness(4, 0, 0, 0) };
            modsGrid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
            modsGrid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
            modsGrid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));

            var row = 0;
            foreach (var mod in bodyPartModifiers)
            {
                var nameLabel = new Label("small") { Text = mod.Def.Label, TextColor = mod.Def.Color };
                Grid.SetRow(nameLabel, row);
                Grid.SetColumn(nameLabel, 0);
                modsGrid.Widgets.Add(nameLabel);

                var chanceText = mod.Chance.Min == mod.Chance.Max
                    ? $"{mod.Chance.Min * 100:0}%"
                    : $"{mod.Chance.Min * 100:0}-{mod.Chance.Max * 100:0}%";
                var chanceLabel = new Label("small") { Text = chanceText, TextColor = Color.LightGray };
                Grid.SetRow(chanceLabel, row);
                Grid.SetColumn(chanceLabel, 1);
                modsGrid.Widgets.Add(chanceLabel);

                if (mod.DurationInTicks.Min > 0 || mod.DurationInTicks.Max > 0)
                {
                    var durationSeconds = mod.DurationInTicks.Min == mod.DurationInTicks.Max
                        ? $"{mod.DurationInTicks.Min / 60f:0.#}s"
                        : $"{mod.DurationInTicks.Min / 60f:0.#}-{mod.DurationInTicks.Max / 60f:0.#}s";
                    var durationLabel = new Label("small") { Text = durationSeconds, TextColor = Color.DarkGray };
                    Grid.SetRow(durationLabel, row);
                    Grid.SetColumn(durationLabel, 2);
                    modsGrid.Widgets.Add(durationLabel);
                }

                row++;
            }
            modsSection.Widgets.Add(modsGrid);
            rightColumn.Widgets.Add(modsSection);
        }
        
        mainLayout.Widgets.Add(rightColumn);
        Widgets.Add(mainLayout);
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
        
        // Update custom info widget if present
        if (_customInfoWidget != null)
        {
            _weaponHandler?.UpdateInfoWidget(_customInfoWidget);
        }
    }
}