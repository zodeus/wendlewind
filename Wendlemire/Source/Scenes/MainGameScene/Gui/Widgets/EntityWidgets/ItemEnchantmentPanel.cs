using System.Globalization;

namespace Wendlemire.Scenes.MainGameScene.Gui.Widgets.EntityWidgets;

[UsedImplicitly]
public sealed class ItemEnchantmentPanel : EntityPanelBase
{
    public ItemEnchantmentPanel(BaseGui gui, Item item, EntityPanelProperties? properties = null) : base(gui, item, properties)
    {
        var card = EntityCardChrome.BeginInspect(this, item);
        var enchantmentProps = item.ItemDef.EnchantmentProperties;

        if (enchantmentProps?.ValidEquipmentTypes is { Count: > 0 })
        {
            Widgets.Add(new HorizontalStackPanel
            {
                Spacing = 8,
                Widgets =
                {
                    new Label("small") { Text = "Can enchant", TextColor = EntityCardChrome.Muted },
                    new Label("small")
                    {
                        Text = string.Join(", ", enchantmentProps.ValidEquipmentTypes),
                        TextColor = EntityCardChrome.Effect
                    }
                }
            });
        }

        if (enchantmentProps?.BodyPartModifiers is { Count: > 0 })
        {
            Widgets.Add(EntityCardChrome.SectionHeader("Effects"));

            foreach (var modifier in enchantmentProps.BodyPartModifiers)
            {
                var modifierColor = modifier.Def.Type switch
                {
                    BodyPartModifierType.Buff => new Color(100, 200, 100),
                    BodyPartModifierType.Debuff => new Color(200, 100, 100),
                    _ => new Color(200, 200, 200)
                };

                var durationText = modifier.DurationInTicks.Min == 0 && modifier.DurationInTicks.Max == 0
                    ? "Permanent"
                    : $"{modifier.DurationInTicks.Min}–{modifier.DurationInTicks.Max} ticks";
                var chanceValue = modifier.Chance.Min;
                var chanceText = chanceValue >= 1f
                    ? "Always"
                    : $"{chanceValue * 100f:0}%";

                var handlerInstance = (BodyPartModifier)Activator.CreateInstance(
                    modifier.Def.HandlerClass, new SimRng(Rng.Visual))!;
                var substancesText = handlerInstance.AllowedSubstances.Count > 0
                    ? string.Join(", ", handlerInstance.AllowedSubstances)
                    : "All substances";

                Widgets.Add(EntityCardChrome.InsetBlock(
                    card.BodyWidth,
                    new Label("small") { Text = modifier.Def.Label, TextColor = modifierColor },
                    EntityCardChrome.StatRow("Duration", durationText, EntityCardChrome.Muted),
                    EntityCardChrome.StatRow("Chance", chanceText, EntityCardChrome.Muted),
                    EntityCardChrome.BodyLabel("Affects  " + substancesText, EntityCardChrome.Tan, card.BodyWidth - 24)));
            }
        }

        if (item.Def.BaseStats.Count > 0)
        {
            Widgets.Add(EntityCardChrome.SectionHeader("Stats"));
            Widgets.Add(EntityCardChrome.StatStrip(item.Def.BaseStats
                .Select(stat => (
                    stat.Def.Label,
                    item.GetStatValue(stat.Def).ToString(CultureInfo.InvariantCulture),
                    Color.LightGoldenrodYellow))
                .ToArray()));
        }
    }

    public override void Update()
    {
    }
}
