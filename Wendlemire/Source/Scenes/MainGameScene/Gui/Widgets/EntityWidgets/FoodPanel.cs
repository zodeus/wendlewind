using System.Globalization;

namespace Wendlemire.Scenes.MainGameScene.Gui.Widgets.EntityWidgets;

[UsedImplicitly]
public sealed class FoodPanel : EntityPanelBase
{
    private readonly Label? _stackSizeLabel;
    private readonly Item _item;

    public FoodPanel(BaseGui gui, Item item, EntityPanelProperties? properties = null) : base(gui, item, properties)
    {
        _item = item;
        EntityCardChrome.ApplyCard(this, 340);

        Widgets.Add(EntityCardChrome.Header(item));

        var foodProps = item.ItemDef.FoodProperties;

        var nutritionValue = item.GetStatValue(Defs.Stats.NutritionalValue);
        Widgets.Add(EntityCardChrome.StatRow(
            "Nutrition",
            nutritionValue.ToString(CultureInfo.InvariantCulture),
            FoodProperties.GetNutritionColor(nutritionValue)));

        if (foodProps?.Effects.Any() == true)
        {
            Widgets.Add(EntityCardChrome.SectionLabel("Effects"));

            foreach (var effect in foodProps.Effects)
            {
                var effectPanel = new HorizontalStackPanel
                {
                    Spacing = 6,
                    Margin = new Thickness(4, 0, 0, 0)
                };

                effectPanel.Widgets.Add(new Image
                {
                    Background = new TextureRegion(effect.Def.GetTexture()),
                    Width = 16,
                    Height = 16
                });

                effectPanel.Widgets.Add(new Label("small")
                {
                    Text = effect.Def.Label,
                    TextColor = FoodProperties.GetEffectColor(effect.Def)
                });

                Widgets.Add(effectPanel);

                AddAffectedStatRows(this, effect.Def.AffectedStats);

                if (!string.IsNullOrEmpty(effect.Def.Notes))
                {
                    Widgets.Add(new Label("small")
                    {
                        Text = $"  {effect.Def.Notes}",
                        TextColor = new Color(130, 130, 130),
                        Wrap = true,
                        MaxWidth = 300
                    });
                }
            }
        }

        if (item.StackSize > 1 || item.ItemDef.StackLimit > 1)
        {
            _stackSizeLabel = new Label("small")
            {
                Text = $"Stack: {item.StackSize}/{item.ItemDef.StackLimit}",
                TextColor = EntityCardChrome.Muted
            };
            Widgets.Add(_stackSizeLabel);
        }
    }

    public override void Update()
    {
        if (_stackSizeLabel != null)
        {
            _stackSizeLabel.Text = $"Stack: {_item.StackSize}/{_item.ItemDef.StackLimit}";
        }
    }

    internal static void AddAffectedStatRows(VerticalStackPanel parent, IEnumerable<AffectedStatRecord>? affectedStats)
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

            parent.Widgets.Add(new HorizontalStackPanel
            {
                Spacing = 6,
                Margin = new Thickness(20, 0, 0, 0),
                Widgets =
                {
                    new Label("small")
                    {
                        Text = affectedStat.Stat.Label,
                        TextColor = EntityCardChrome.Muted,
                        Width = 110
                    },
                    new Label("small")
                    {
                        Text = $"{offset}{factor}"
                    }
                }
            });
        }
    }
}
