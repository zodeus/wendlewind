namespace Wendlewind.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets.PawnBodyPanelWidgets;

internal sealed class MedicalItemsBar : HorizontalStackPanel, IUpdatable
{
    private readonly BaseGui _gui;
    private readonly PawnInventory _inventory;
    private readonly Dictionary<Item, MedicalItemButton> _itemButtons = new();
    private readonly HorizontalStackPanel _itemsContainer;

    public MedicalItemsBar(BaseGui gui, PawnInventory inventory)
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
        // Get all medical items from inventory
        var medicalItems = _inventory
            .Where(item => item.ItemDef.ItemType == ItemType.Medical)
            //order by cauterize, medkit, mist, arterial threads
            .OrderBy(item => item.Def == Defs.Items.Cauterize ? 0 : item.Def == Defs.Items.MedKit ? 1 : item.Def == Defs.Items.MendersMist ? 2 : item.Def == Defs.Items.ArterialThreads ? 3 : 4)
            .ToList();

        // Remove buttons for items no longer in inventory
        foreach (var (item, button) in _itemButtons.ToList())
        {
            if (item.IsDestroyed || !medicalItems.Contains(item))
            {
                button.RemoveFromParent();
                _itemButtons.Remove(item);
            }
        }

        // Add or update buttons for medical items
        foreach (var item in medicalItems)
        {
            if (!_itemButtons.ContainsKey(item))
            {
                var button = new MedicalItemButton(_gui, item);
                _itemButtons[item] = button;
                _itemsContainer.Widgets.Add(button);
            }
            else
            {
                _itemButtons[item].Update();
            }
        }

        // Hide the bar if no medical items
        Visible = medicalItems.Count > 0;
    }
}

internal sealed class MedicalItemButton : CursorButton
{
    private readonly BaseGui _gui;
    private readonly Item _item;
    private readonly Label _stackLabel;

    public MedicalItemButton(BaseGui gui, Item item) : base(BaseContent.Styles.Button.Icon)
    {
        _gui = gui;
        _item = item;

        var container = new Panel();

        // Item icon
        container.Widgets.Add(new Image
        {
            Background = item.GetIconImage(),
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
    }

    private void OnClick(object? sender, EventArgs e)
    {
        // Attach the item to the mouse for use
        _gui.MouseAttachment = new MouseAttachment(
            _gui,
            _item.GetIcon(),
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
    }
}
