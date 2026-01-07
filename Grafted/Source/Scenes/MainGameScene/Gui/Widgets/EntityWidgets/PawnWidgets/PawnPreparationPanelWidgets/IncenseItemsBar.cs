namespace Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets.PawnPreparationPanelWidgets;

internal sealed class IncenseItemsBar : HorizontalStackPanel, IUpdatable
{
    private readonly BaseGui _gui;
    private readonly PawnInventory _inventory;
    private readonly Player _player;
    private readonly Dictionary<Item, IncenseItemButton> _itemButtons = new();
    private readonly HorizontalStackPanel _itemsContainer;

    public IncenseItemsBar(BaseGui gui, Player player)
    {
        _gui = gui;
        _player = player;
        _inventory = player.Pawn.Inventory;
        Spacing = 4;
        _itemsContainer = new HorizontalStackPanel { Spacing = 4 };
        Widgets.Add(_itemsContainer);

        Update();
    }

    public void Update()
    {
        // Get all incense items that can be burned from inventory
        var incenseItems = _inventory
            .Where(item => item.ItemDef.ItemType == ItemType.Incense && CanBurnItem(item))
            .OrderBy(item => item.Label)
            .ToList();

        // Remove buttons for items no longer in inventory
        foreach (var (item, button) in _itemButtons.ToList())
        {
            if (item.IsDestroyed || !incenseItems.Contains(item))
            {
                button.RemoveFromParent();
                _itemButtons.Remove(item);
            }
        }

        // Add or update buttons for incense items
        foreach (var item in incenseItems)
        {
            if (!_itemButtons.ContainsKey(item))
            {
                var button = new IncenseItemButton(_gui, _player, item);
                _itemButtons[item] = button;
                _itemsContainer.Widgets.Add(button);
            }
            else
            {
                _itemButtons[item].Update();
            }
        }

        // Hide the bar if no incense items
        Visible = incenseItems.Count > 0;
    }

    private bool CanBurnItem(Item item)
    {
        // Only specific incense items can be burned
        return item.ItemDef == Defs.Items.MullinStick ||
               item.ItemDef == Defs.Items.ShadeWood ||
               item.ItemDef == Defs.Items.DippedMullinStick;
    }
}

internal sealed class IncenseItemButton : CursorButton
{
    private readonly BaseGui _gui;
    private readonly Player _player;
    private readonly Item _item;
    private readonly Label _stackLabel;
    private readonly Image _iconImage;
    private Window? _tooltipWindow;
    private Label? _tooltipLabel;

    public IncenseItemButton(BaseGui gui, Player player, Item item) : base(BaseContent.Styles.Button.Icon)
    {
        _gui = gui;
        _player = player;
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
        if (!CanBurn()) return;

        BurnItem();
    }

    private bool CanBurn()
    {
        // Requires FlameStick trinket
        return _player.HasTrinkets(Defs.Items.FlameStick);
    }

    private void BurnItem()
    {
        if (_item.StackSize > 1)
        {
            _item.StackSize--;
        }
        else
        {
            _item.Destroy();
        }

        Core.Context.Achievements.OnItemUsed(_player.Pawn, _item);

        if (_item.ItemDef == Defs.Items.MullinStick)
        {
            _gui.PushScreenMessage(new ScreenMessageData
            {
                Font = BaseContent.Fonts.Default.Medium,
                Text = Defs.BodyEffects.SmokeyHaze.Description,
                Duration = 6,
                Color = Color.Orange
            });
            _player.Pawn.Body.Effects.TryApplyEffect(new BodyEffect
            {
                Def = Defs.BodyEffects.SmokeyHaze,
                TicksLeft = 4000
            });
        }
        else if (_item.ItemDef == Defs.Items.ShadeWood)
        {
            _gui.PushScreenMessage(new ScreenMessageData
            {
                Font = BaseContent.Fonts.Default.Medium,
                Text = Defs.BodyEffects.SmokeyHaze.Description,
                Duration = 6,
                Color = Color.Orange
            });
            _player.Pawn.Body.Effects.TryApplyEffect(new BodyEffect
            {
                Def = Defs.BodyEffects.Psychedelic,
                TicksLeft = 4000
            });
        }
        else if (_item.ItemDef == Defs.Items.DippedMullinStick)
        {
            _gui.PushScreenMessage(new ScreenMessageData
            {
                Font = BaseContent.Fonts.Default.Medium,
                Text = Defs.BodyEffects.GoldenSmoke.Description,
                Duration = 6,
                Color = Color.Orange
            });
            _player.Pawn.Body.Effects.TryApplyEffect(new BodyEffect
            {
                Def = Defs.BodyEffects.GoldenSmoke,
                TicksLeft = 2000
            });
        }
    }

    public void Update()
    {
        _stackLabel.Text = _item.StackSize > 1 ? _item.StackSize.ToString() : "";
        // Only enable if player has FlameStick
        Enabled = CanBurn();
        // Tint icon when disabled
        ((ColoredRegion)_iconImage.Background).Color = Enabled ? Color.White : Color.Gray;

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
