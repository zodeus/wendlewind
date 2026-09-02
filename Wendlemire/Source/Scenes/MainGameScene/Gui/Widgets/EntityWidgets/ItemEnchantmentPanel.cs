using System.Globalization;

namespace Wendlemire.Scenes.MainGameScene.Gui.Widgets.EntityWidgets;

[UsedImplicitly]
public sealed class ItemEnchantmentPanel : EntityPanelBase
{
    public ItemEnchantmentPanel(BaseGui gui, Item item, EntityPanelProperties? properties = null) : base(gui, item, properties)
    {
        EntityCardChrome.ApplyCard(this, 340);

        var enchantmentProps = item.ItemDef.EnchantmentProperties;

        Widgets.Add(EntityCardChrome.Header(item));

        // Valid equipment types
        if (enchantmentProps?.ValidEquipmentTypes is { Count: > 0 })
        {
            var equipmentTypesText = string.Join(", ", enchantmentProps.ValidEquipmentTypes);
            Widgets.Add(new HorizontalStackPanel
            {
                Spacing = 8,
                Widgets =
                {
                    new Label("small") { Text = "Can enchant:", TextColor = EntityCardChrome.Muted },
                    new Label("small") { Text = equipmentTypesText, TextColor = new Color(120, 200, 120) }
                }
            });
        }

        // Body part modifiers
        if (enchantmentProps?.BodyPartModifiers is { Count: > 0 })
        {
            Widgets.Add(EntityCardChrome.SectionLabel("Effects"));

            foreach (var modifier in enchantmentProps.BodyPartModifiers)
            {
                var modifierPanel = new VerticalStackPanel
                {
                    Spacing = 2,
                    Margin = new Thickness(10, 0, 0, 5)
                };

                // Modifier name with type-based coloring
                var modifierColor = modifier.Def.Type switch
                {
                    BodyPartModifierType.Buff => new Color(100, 200, 100),
                    BodyPartModifierType.Debuff => new Color(200, 100, 100),
                    _ => new Color(200, 200, 200)
                };

                modifierPanel.Widgets.Add(new Label("small")
                {
                    Text = $"{modifier.Def.Label}",
                    TextColor = modifierColor
                });

                // Duration info
                var durationText = modifier.DurationInTicks.Min == 0 && modifier.DurationInTicks.Max == 0
                    ? "Permanent"
                    : $"Duration: {modifier.DurationInTicks.Min}~{modifier.DurationInTicks.Max} ticks";

                modifierPanel.Widgets.Add(new Label("small")
                {
                    Text = $"   {durationText}",
                    TextColor = new Color(150, 150, 150)
                });

                // Chance info
                var chanceValue = modifier.Chance.Min;
                var chanceText = chanceValue >= 1f
                    ? "Always applies"
                    : $"Chance: {chanceValue * 100f:0}%";

                modifierPanel.Widgets.Add(new Label("small")
                {
                    Text = $"   {chanceText}",
                    TextColor = new Color(150, 150, 150)
                });

                // Allowed substances info
                var handlerInstance = (BodyPartModifier)Activator.CreateInstance(
                    modifier.Def.HandlerClass, new SimRng(Rng.Visual))!;
                if (handlerInstance.AllowedSubstances.Count > 0)
                {
                    var substancesText = string.Join(", ", handlerInstance.AllowedSubstances);
                    modifierPanel.Widgets.Add(new Label("small")
                    {
                        Text = $"   Affects: {substancesText}",
                        TextColor = new Color(180, 140, 100)
                    });
                } else {
                    modifierPanel.Widgets.Add(new Label("small")
                    {
                        Text = $"   Affects: All substances",
                        TextColor = new Color(180, 140, 100)
                    });
                }

                Widgets.Add(modifierPanel);
            }
        }

        // Base stats
        if (item.Def.BaseStats.Count > 0)
        {
            Widgets.Add(EntityCardChrome.SectionLabel("Stats"));

            foreach (var baseStat in item.Def.BaseStats)
            {
                Widgets.Add(EntityCardChrome.StatRow(
                    baseStat.Def.Label,
                    item.GetStatValue(baseStat.Def).ToString(CultureInfo.InvariantCulture)));
            }
        }
    }

    public override void Update()
    {
        // Enchantments don't have dynamic state to update
    }
}


