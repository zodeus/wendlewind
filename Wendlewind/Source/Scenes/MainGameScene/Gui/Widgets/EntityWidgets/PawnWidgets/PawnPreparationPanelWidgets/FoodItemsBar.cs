using System.Globalization;

namespace Wendlewind.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets.PawnPreparationPanelWidgets;

internal sealed class FoodItemsBar : HorizontalStackPanel, IUpdatable
{
    private readonly BaseGui _gui;
    private readonly PawnInventory _inventory;
    private readonly Pawn _pawn;
    private readonly Dictionary<Item, FoodItemButton> _itemButtons = new();
    private readonly HorizontalStackPanel _itemsContainer;

    public FoodItemsBar(BaseGui gui, Pawn pawn)
    {
        _gui = gui;
        _pawn = pawn;
        _inventory = pawn.Inventory;
        Spacing = 4;
        _itemsContainer = new HorizontalStackPanel { Spacing = 4 };
        Widgets.Add(_itemsContainer);

        Update();
    }

    public void Update()
    {
        // Get all food items from inventory
        var foodItems = _inventory
            .Where(item => item.ItemDef.FoodProperties != null)
            .OrderBy(item => item.Label)
            .ToList();

        // Remove buttons for items no longer in inventory
        foreach (var (item, button) in _itemButtons.ToList())
        {
            if (item.IsDestroyed || !foodItems.Contains(item))
            {
                button.RemoveFromParent();
                _itemButtons.Remove(item);
            }
        }

        // Add or update buttons for food items
        foreach (var item in foodItems)
        {
            if (!_itemButtons.ContainsKey(item))
            {
                var button = new FoodItemButton(_gui, _pawn, item);
                _itemButtons[item] = button;
                _itemsContainer.Widgets.Add(button);
            }
            else
            {
                _itemButtons[item].Update();
            }
        }

        // Hide the bar if no food items
        Visible = foodItems.Count > 0;
    }
}

internal sealed class FoodItemButton : CursorButton
{
    private readonly BaseGui _gui;
    private readonly Pawn _pawn;
    private readonly Item _item;
    private readonly Label _stackLabel;
    private readonly Image _iconImage;
    private Color _enabledColor = Color.White;
    private Color _disabledColor = new Color(180, 40, 40);

    public FoodItemButton(BaseGui gui, Pawn pawn, Item item) : base(BaseContent.Styles.Button.Icon)
    {
        _gui = gui;
        _pawn = pawn;
        _item = item;

        var container = new Panel();

        // Item icon
        _iconImage = new Image
        {
            Background = new ColoredRegion(new TextureRegion(item.Icon), Color.White),
            Width = BaseContent.IconSizes.Large,
            Height = BaseContent.IconSizes.Large
        };
        container.Widgets.Add(_iconImage);

        // Stack size label (bottom-right corner)
        _stackLabel = new Label(BaseContent.Styles.Label.Small)
        {
            Text = item.StackSize > 1 ? item.StackSize.ToString() : "",
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom,
            TextColor = Color.White
        };
        container.Widgets.Add(_stackLabel);

        Content = container;
        Width = BaseContent.IconSizes.Large + 8;
        Height = BaseContent.IconSizes.Large + 8;

        Click += OnClick;
        this.WithTooltip(CreateTooltipContent);
        Update();
    }

    private Widget CreateTooltipContent()
    {
        var container = new VerticalStackPanel { Spacing = 4, Padding = new Thickness(4) };

        // Item name
        container.Widgets.Add(new Label(BaseContent.Styles.Label.Normal)
        {
            Text = _item.Label,
            TextColor = Color.Gold
        });

        // Nutritional value
        var nutritionValue = _item.GetStatValue(Defs.Stats.NutritionalValue);
        var nutritionColor = FoodProperties.GetNutritionColor(nutritionValue);
        container.Widgets.Add(new HorizontalStackPanel
        {
            Spacing = 6,
            Widgets =
            {
                new Label(BaseContent.Styles.Label.Small) { Text = "Nutrition:", TextColor = new Color(180, 180, 180) },
                new Label(BaseContent.Styles.Label.Small) { Text = $"{nutritionValue:0.##}", TextColor = nutritionColor }
            }
        });

        // Effects section
        var foodProps = _item.ItemDef.FoodProperties;
        if (foodProps?.Effects.Any() == true)
        {
            container.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
            {
                Text = "Effects:",
                TextColor = new Color(220, 180, 100),
                Margin = new Thickness(0, 4, 0, 2)
            });

            foreach (var effect in foodProps.Effects)
            {
                var effectPanel = new HorizontalStackPanel
                {
                    Spacing = 6,
                    Margin = new Thickness(8, 0, 0, 0)
                };

                var effectColor = FoodProperties.GetEffectColor(effect.Def);
                effectPanel.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
                {
                    Text = effect.Def.Label,
                    TextColor = effectColor
                });

                if (effect.DurationInTicks > 0)
                {
                    effectPanel.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
                    {
                        Text = $"({effect.DurationInTicks:N0} ticks)",
                        TextColor = new Color(150, 150, 150)
                    });
                }

                container.Widgets.Add(effectPanel);
            }
        }

        return container;
    }

    private void OnClick(object? sender, EventArgs e)
    {
        if (!_pawn.IsHungry) return;

        if (_pawn.TryEat(_item))
        {
            _gui.WorldTextHandler.Add(new WorldSpaceText
            {
                Font = BaseContent.Fonts.Default.Medium,
                Color = Color.PaleGoldenrod,
                Text = _item.Label,
                DurationInTicks = 120,
                Position = Mouse.GetState().Position.ToVector2()
            });
        }
    }

    public void Update()
    {
        _stackLabel.Text = _item.StackSize > 1 ? _item.StackSize.ToString() : "";
        // Only enable if pawn is hungry
        Enabled = _pawn.IsHungry;
        // Tint icon when disabled
        ((ColoredRegion)_iconImage.Background).Color = Enabled ? _enabledColor : _disabledColor;
        TooltipHelper.UpdatePosition();
    }
}
