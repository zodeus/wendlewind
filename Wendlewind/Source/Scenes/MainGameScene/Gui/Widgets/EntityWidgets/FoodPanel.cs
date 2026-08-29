using System.Globalization;

namespace Wendlewind.Scenes.MainGameScene.Gui.Widgets.EntityWidgets;

[UsedImplicitly]
public sealed class FoodPanel : EntityPanelBase
{
    private readonly Label _stackSizeLabel = null!;
    private readonly Item _item;

    public FoodPanel(BaseGui gui, Item item, EntityPanelProperties? properties = null) : base(gui, item, properties)
    {
        _item = item;
        Padding = new Thickness(20);
        MinWidth = 300;
        Spacing = 8;

        // Icon and description header
        Widgets.Add(new HorizontalStackPanel
        {
            Spacing = 15,
            Widgets =
            {
                new Image { Background = new TextureRegion(item.Icon), Width = 96, Height = 96 },
                new VerticalStackPanel
                {
                    Spacing = 5,
                    Widgets =
                    {
                        new Label(BaseContent.Styles.Label.Normal) { Text = item.Label },
                        new Label("small")
                        {
                            Text = item.Def.Description,
                            Wrap = true,
                            MaxWidth = 350,
                            TextColor = new Color(200, 200, 200)
                        }
                    }
                }
            }
        });

        // Food type
        var foodProps = item.ItemDef.FoodProperties;
        if (foodProps != null)
        {
            Widgets.Add(new Label("small")
            {
                Text = $"Type: {item.ItemDef.Label}",
                TextColor = new Color(180, 180, 180)
            });
        }

        // Nutritional value
        var nutritionValue = item.GetStatValue(Defs.Stats.NutritionalValue);
        var nutritionColor = FoodProperties.GetNutritionColor(nutritionValue);
        Widgets.Add(new HorizontalStackPanel
        {
            Spacing = 8,
            Margin = new Thickness(0, 5, 0, 0),
            Widgets =
            {
                new Label("small") { Text = "Nutritional Value:", TextColor = new Color(180, 180, 180) },
                new Label("small") { Text = nutritionValue.ToString(CultureInfo.InvariantCulture), TextColor = nutritionColor }
            }
        });

        // Effects section
        if (foodProps?.Effects.Any() == true)
        {
            Widgets.Add(new Label("small")
            {
                Text = "Effects:",
                TextColor = new Color(220, 180, 100),
                Margin = new Thickness(0, 10, 0, 5)
            });

            foreach (var effect in foodProps.Effects)
            {
                var effectPanel = new HorizontalStackPanel
                {
                    Spacing = 10,
                    Margin = new Thickness(10, 0, 0, 2)
                };

                effectPanel.Widgets.Add(new Image
                {
                    Background = new TextureRegion(effect.Def.Texture),
                    Width = 20,
                    Height = 20
                });

                var effectColor = FoodProperties.GetEffectColor(effect.Def);
                effectPanel.Widgets.Add(new Label("small")
                {
                    Text = effect.Def.Label,
                    TextColor = effectColor
                });

                // Duration
                if (effect.DurationInTicks > 0)
                {
                    effectPanel.Widgets.Add(new Label("small")
                    {
                        Text = $"({effect.DurationInTicks:N0} ticks)",
                        TextColor = new Color(150, 150, 150)
                    });
                }

                Widgets.Add(effectPanel);

                // Effect notes/description if available
                if (!string.IsNullOrEmpty(effect.Def.Notes))
                {
                    Widgets.Add(new Label("small")
                    {
                        Text = $"   {effect.Def.Notes}",
                        TextColor = new Color(130, 130, 130),
                        Wrap = true,
                        MaxWidth = 350
                    });
                }
            }
        }

        // Stack info
        if (item.StackSize > 1 || item.ItemDef.StackLimit > 1)
        {
            _stackSizeLabel = new Label("small")
            {
                Text = $"Stack: {item.StackSize}/{item.ItemDef.StackLimit}",
                TextColor = new Color(150, 150, 150),
                Margin = new Thickness(0, 5, 0, 0)
            };
            Widgets.Add(_stackSizeLabel);
        }        
    }

    public override void Update()
    {
        _stackSizeLabel.Text = $"Stack: {_item.StackSize}/{_item.ItemDef.StackLimit}";
    }
}