using System.Globalization;
using Grafted.Sim.Entities;

namespace Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets;

[UsedImplicitly]
public sealed class FoodPanel : EntityPanelBase
{
    private readonly Button _eatButton;
    private readonly Button _cookButton;

    public FoodPanel(BaseGui gui, Item item, EntityPanelProperties? properties = null) : base(gui, item, properties)
    {
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
            var foodTypeLabel = FormatFoodType(foodProps.FoodType);
            Widgets.Add(new Label("small")
            {
                Text = $"Type: {foodTypeLabel}",
                TextColor = new Color(180, 180, 180)
            });
        }

        // Nutritional value
        var nutritionValue = item.GetStatValue(Defs.Stats.NutritionalValue);
        var nutritionColor = GetNutritionColor(nutritionValue);
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

                var effectColor = GetEffectColor(effect.Def);
                effectPanel.Widgets.Add(new Label("small")
                {
                    Text = effect.Def.Label,
                    TextColor = effectColor
                });

                // Duration
                if (effect.DurationInTicks > 0)
                {
                    var durationSeconds = effect.DurationInTicks / 60f;
                    effectPanel.Widgets.Add(new Label("small")
                    {
                        Text = $"({durationSeconds:0.#}s)",
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
            Widgets.Add(new Label("small")
            {
                Text = $"Stack: {item.StackSize}/{item.ItemDef.StackLimit}",
                TextColor = new Color(150, 150, 150),
                Margin = new Thickness(0, 5, 0, 0)
            });
        }

        // Buttons
        _eatButton = new Button(BaseContent.Styles.Button.Normal)
        {
            Content = new Label { Text = "Eat" }, Margin = new Thickness(0, 15, 0, 0)
        };
        _eatButton.Click += (_, _) =>
        {
            if (Core.Context.PlayerPawn.TryEat(item))
            {
                gui.WorldTextHandler.Add(new WorldSpaceText
                {
                    Font = BaseContent.Fonts.Default.Medium,
                    Color = Color.PaleGoldenrod,
                    Text = item.Label,
                    DurationInTicks = 120,
                    Position = Mouse.GetState().Position.ToVector2()
                });
            }
        };

        _cookButton = new Button(BaseContent.Styles.Button.Normal)
        {
            Content = new Label { Text = "Cook" }, Margin = new Thickness(0, 15, 0, 0),
            Visible = ShowCookButton(item)
        };
        _cookButton.Click += (_, _) => { HandleCooking(item); };
        Widgets.Add(new HorizontalStackPanel
        {
            Spacing = 20,
            Widgets = { _eatButton, _cookButton }
        });
    }

    private static string FormatFoodType(FoodType foodType)
    {
        return foodType switch
        {
            FoodType.RawGrain => "Raw Grain",
            FoodType.RawMeat => "Raw Meat",
            FoodType.CookedVegetable => "Cooked Vegetable",
            FoodType.CookedMeat => "Cooked Meat",
            FoodType.DriedMeat => "Dried Meat",
            FoodType.Fungi => "Fungi",
            FoodType.Meat => "Meat",
            FoodType.Fish => "Fish",
            FoodType.Berry => "Berry",
            FoodType.Fruit => "Fruit",
            _ => foodType.ToString()
        };
    }

    private static Color GetNutritionColor(float value)
    {
        return value switch
        {
            >= 50 => new Color(100, 220, 100),  // High nutrition - green
            >= 25 => new Color(220, 220, 100),  // Medium nutrition - yellow
            _ => new Color(200, 150, 100)       // Low nutrition - orange
        };
    }

    private static Color GetEffectColor(BodyEffectDef def)
    {
        // Check if the effect has any negative stat modifiers
        if (def.AffectedStats != null)
        {
            foreach (var stat in def.AffectedStats)
            {
                if (stat.Offset < 0 || stat.Factor < 1f)
                    return new Color(220, 100, 100); // Negative effect - red
            }
        }
        return new Color(100, 180, 220); // Positive/neutral effect - blue
    }

    private static void HandleCooking(Item item)
    {
        if (item.ItemDef != Defs.Items.RawMeat && item.ItemDef != Defs.Items.RawCorn) return;
        if (item.StackSize > 1)
        {
            item.StackSize--;
        }
        else
        {
            item.Destroy();
        }

        if (item.ItemDef == Defs.Items.RawMeat)
        {
            Core.Context.PlayerPawn.Inventory.TryAdd(EntityGenerator.CreateEntity<Item>(Defs.Items.CookedMeat));
        }
        else if (item.ItemDef == Defs.Items.RawCorn)
        {
            Core.Context.PlayerPawn.Inventory.TryAdd(EntityGenerator.CreateEntity<Item>(Defs.Items.CookedCorn));
        }
    }

    private static bool ShowCookButton(Item item)
    {
        if (item.ItemDef == Defs.Items.RawMeat && Core.Context.Player.HasTrinkets(Defs.Items.EncasedFire))
            return true;
        if (item.ItemDef == Defs.Items.RawCorn && Core.Context.Player.HasTrinkets(Defs.Items.EncasedFire, Defs.Items.CookingPot, Defs.Items.WeepingBucket))
            return true;
        return false;
    }

    public override void Update()
    {
        _eatButton.Enabled = Core.Context.World.PlayerPawn.IsHungry;
    }
}