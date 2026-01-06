namespace Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets.PawnPreparationPanelWidgets;

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
    private Window? _tooltipWindow;
    private Label? _tooltipLabel;
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
        MouseEntered += (_, _) => ShowTooltip();
        MouseLeft += (_, _) => HideTooltip();
        Update();
    }

    private void EnsureTooltipCreated()
    {
        if (_tooltipWindow != null) return;

        _tooltipLabel = new Label(BaseContent.Styles.Label.Small)
        {
            TextColor = Color.White
        };

        _tooltipWindow = new Window
        {
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.IconFrame],
            Margin = new Thickness(0),
            Padding = new Thickness(10,3,10,10),
            Content = _tooltipLabel
        };
        _tooltipWindow.TitlePanel.Visible = false;
    }

    private void ShowTooltip()
    {
        if (Desktop == null) return;

        EnsureTooltipCreated();

        _tooltipLabel!.Text = _item.Label;

        // Position tooltip near the mouse
        var screenPos = Mouse.GetState().Position;
        var uiX = (int)((screenPos.X - Core.UiOffset.X) / Core.UiScale);
        var uiY = (int)((screenPos.Y - Core.UiOffset.Y) / Core.UiScale);

        if (!_tooltipWindow!.IsPlaced)
        {
            _tooltipWindow.Show(Desktop, new Point(uiX + 15, uiY + 15));
        }
        else
        {
            _tooltipWindow.Left = uiX + 15;
            _tooltipWindow.Top = uiY + 15;
        }
    }

    private void HideTooltip()
    {
        _tooltipWindow?.Close();
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

        // Update tooltip position while hovering
        if (_tooltipWindow?.IsPlaced == true)
        {
            var screenPos = Mouse.GetState().Position;
            var uiX = (int)((screenPos.X - Core.UiOffset.X) / Core.UiScale);
            var uiY = (int)((screenPos.Y - Core.UiOffset.Y) / Core.UiScale);

            _tooltipWindow.Left = uiX + 15;
            _tooltipWindow.Top = uiY + 15;
        }
    }
}
