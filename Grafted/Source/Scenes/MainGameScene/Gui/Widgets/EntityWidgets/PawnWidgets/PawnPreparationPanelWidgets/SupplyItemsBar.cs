namespace Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets.PawnPreparationPanelWidgets;

internal sealed class SupplyItemsBar : HorizontalStackPanel, IUpdatable
{
    private readonly BaseGui _gui;
    private readonly PawnInventory _inventory;
    private readonly Dictionary<Item, SupplyItemButton> _itemButtons = new();
    private readonly HorizontalStackPanel _itemsContainer;

    public SupplyItemsBar(BaseGui gui, PawnInventory inventory)
    {
        _gui = gui;
        _inventory = inventory;
        Spacing = 4;
        _itemsContainer = new HorizontalStackPanel { Spacing = 4 };
        Widgets.Add(_itemsContainer);

        Update();
    }

    public void Update()
    {
        // Get all supply items from inventory
        var supplyItems = _inventory
            .Where(item => item.ItemDef.ItemType == ItemType.Supplies && item.ItemDef.AmmoProperties == null)
            .OrderBy(item => item.Label)
            .ToList();

        // Remove buttons for items no longer in inventory
        foreach (var (item, button) in _itemButtons.ToList())
        {
            if (item.IsDestroyed || !supplyItems.Contains(item))
            {
                button.RemoveFromParent();
                _itemButtons.Remove(item);
            }
        }

        // Add or update buttons for supply items
        foreach (var item in supplyItems)
        {
            if (!_itemButtons.ContainsKey(item))
            {
                var button = new SupplyItemButton(_gui, item);
                _itemButtons[item] = button;
                _itemsContainer.Widgets.Add(button);
            }
            else
            {
                _itemButtons[item].Update();
            }
        }

        // Hide the bar if no supply items
        Visible = supplyItems.Count > 0;
    }
}

internal sealed class SupplyItemButton : CursorButton
{
    private readonly BaseGui _gui;
    private readonly Item _item;
    private readonly Label _stackLabel;
    private Window? _tooltipWindow;
    private Label? _tooltipLabel;

    public SupplyItemButton(BaseGui gui, Item item) : base(BaseContent.Styles.Button.Icon)
    {
        _gui = gui;
        _item = item;

        var container = new Panel();

        // Item icon
        container.Widgets.Add(new Image
        {
            Background = new TextureRegion(item.Icon),
            Width = BaseContent.IconSizes.Large,
            Height = BaseContent.IconSizes.Large
        });

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
            Padding = new Thickness(10, 3, 10, 10),
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
        // Attach the item to the mouse for use
        _gui.MouseAttachment = new MouseAttachment(
            _gui,
            _item.Icon,
            leftClickAction: null,
            updateAction: (attachment) =>
            {
                if (Mouse.GetState().RightButton == ButtonState.Pressed) attachment.Detach();
            }
        )
        {
            Data = _item,
            IconSize = new Size(BaseContent.IconSizes.ExtraLarge, BaseContent.IconSizes.ExtraLarge)
        };
    }

    public void Update()
    {
        _stackLabel.Text = _item.StackSize > 1 ? _item.StackSize.ToString() : "";

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
