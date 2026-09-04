using System.Globalization;

namespace Wendlemire.Scenes.MainGameScene.Gui.Widgets.EntityWidgets;

[UsedImplicitly]
public sealed class FoodPanel : EntityPanelBase
{
    private readonly Label? _stackValue;
    private readonly Item _item;

    public FoodPanel(BaseGui gui, Item item, EntityPanelProperties? properties = null) : base(gui, item, properties)
    {
        _item = item;
        var card = EntityCardChrome.BeginInspect(this, item);

        var chips = new List<Widget>();
        var nutritionValue = item.GetStatValue(Defs.Stats.NutritionalValue);
        chips.Add(EntityCardChrome.StatChip(
            "Nutrition",
            nutritionValue.ToString(CultureInfo.InvariantCulture),
            FoodProperties.GetNutritionColor(nutritionValue),
            out _));

        if (item.StackSize > 1 || item.ItemDef.StackLimit > 1)
        {
            chips.Add(EntityCardChrome.StatChip(
                "Stack",
                $"{item.StackSize}/{item.ItemDef.StackLimit}",
                EntityCardChrome.Gold,
                out _stackValue));
        }

        Widgets.Add(EntityCardChrome.StatStrip(chips));

        var foodProps = item.ItemDef.FoodProperties;
        if (foodProps?.Effects.Any() != true)
        {
            return;
        }

        Widgets.Add(EntityCardChrome.SectionHeader("Effects"));
        foreach (var effect in foodProps.Effects)
        {
            var rows = new List<Widget>
            {
                EntityCardChrome.IconLabel(
                    new TextureRegion(effect.Def.GetTexture()),
                    effect.Def.Label,
                    FoodProperties.GetEffectColor(effect.Def))
            };

            AddAffectedStatRows(rows, effect.Def.AffectedStats);

            if (!string.IsNullOrEmpty(effect.Def.Notes))
            {
                rows.Add(EntityCardChrome.BodyLabel(effect.Def.Notes, EntityCardChrome.Muted, card.BodyWidth - 24));
            }

            Widgets.Add(EntityCardChrome.InsetBlock(card.BodyWidth, rows.ToArray()));
        }
    }

    public override void Update()
    {
        if (_stackValue != null)
        {
            _stackValue.Text = $"{_item.StackSize}/{_item.ItemDef.StackLimit}";
        }
    }

    internal static void AddAffectedStatRows(VerticalStackPanel parent, IEnumerable<AffectedStatRecord>? affectedStats)
    {
        var rows = new List<Widget>();
        AddAffectedStatRows(rows, affectedStats);
        foreach (var row in rows)
        {
            parent.Widgets.Add(row);
        }
    }

    internal static void AddAffectedStatRows(ICollection<Widget> parent, IEnumerable<AffectedStatRecord>? affectedStats)
    {
        if (affectedStats == null)
        {
            return;
        }

        foreach (var affectedStat in affectedStats)
        {
            var offset = affectedStat.Offset != null
                ? $"/c[{(affectedStat.Offset > 0 ? TC.Green : TC.Red)}]+{affectedStat.Offset} "
                : "";
            var factor = affectedStat.Factor != null
                ? $"/c[{(affectedStat.Factor > 0 ? TC.Green : TC.Red)}]*{affectedStat.Factor} "
                : "";

            if (offset.Length == 0 && factor.Length == 0)
            {
                continue;
            }

            parent.Add(new HorizontalStackPanel
            {
                Spacing = 6,
                Widgets =
                {
                    new Label("small")
                    {
                        Text = affectedStat.Stat.Label,
                        TextColor = EntityCardChrome.Muted,
                        Width = 110
                    },
                    new Label("small") { Text = $"{offset}{factor}" }
                }
            });
        }
    }
}
