using System.Globalization;

namespace Wendlewind.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets.PawnPreparationPanelWidgets;

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
        // Only incense items with defined effects can be burned
        return item.ItemDef.IncenseProperties?.Effect != null;
    }
}

internal sealed class IncenseItemButton : CursorButton
{
    private readonly BaseGui _gui;
    private readonly Player _player;
    private readonly Item _item;
    private readonly Label _stackLabel;
    private readonly Image _iconImage;

    public IncenseItemButton(BaseGui gui, Player player, Item item) : base(BaseContent.Styles.Button.Icon)
    {
        _gui = gui;
        _player = player;
        _item = item;

        var container = new Panel();

        // Item icon
        _iconImage = new Image
        {
            Background = new ColoredRegion(new TextureRegion(item.GetIcon()), Color.White),
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

        // Effect section
        var incenseProps = _item.ItemDef.IncenseProperties;
        if (incenseProps?.Effect != null)
        {
            container.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
            {
                Text = "When Burned:",
                TextColor = new Color(180, 170, 160),
                Margin = new Thickness(0, 4, 0, 2)
            });

            var effectPanel = new HorizontalStackPanel
            {
                Spacing = 6,
                Margin = new Thickness(8, 0, 0, 0)
            };

            var effectColor = IncenseProperties.GetEffectColor(incenseProps.Effect.Def);
            effectPanel.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
            {
                Text = incenseProps.Effect.Def.Label,
                TextColor = effectColor
            });

            if (incenseProps.Effect.DurationInTicks > 0)
            {
                var durationSeconds = incenseProps.Effect.DurationInTicks / 60f;
                effectPanel.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
                {
                    Text = $"({durationSeconds:0.#}s)",
                    TextColor = new Color(150, 150, 150)
                });
            }

            container.Widgets.Add(effectPanel);
        }

        return container;
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
        var incenseProps = _item.ItemDef.IncenseProperties;
        if (incenseProps?.Effect == null) return;

        if (_item.StackSize > 1)
        {
            _item.StackSize--;
        }
        else
        {
            _item.Destroy();
        }

        Core.Context.Achievements.OnItemUsed(_player.Pawn, _item);

        _gui.PushScreenMessage(new ScreenMessageData
        {
            Text = incenseProps.Effect.Def.Description,
            Duration = 6,
            Color = Color.Orange
        });

        _player.Pawn.Body.Effects.TryApplyEffect(new BodyEffect
        {
            Def = incenseProps.Effect.Def,
            TicksLeft = incenseProps.Effect.DurationInTicks
        });
    }

    public void Update()
    {
        _stackLabel.Text = _item.StackSize > 1 ? _item.StackSize.ToString() : "";
        // Only enable if player has FlameStick
        Enabled = CanBurn();
        // Tint icon when disabled
        ((ColoredRegion)_iconImage.Background).Color = Enabled ? Color.White : Color.Gray;
        TooltipHelper.UpdatePosition();
    }
}
