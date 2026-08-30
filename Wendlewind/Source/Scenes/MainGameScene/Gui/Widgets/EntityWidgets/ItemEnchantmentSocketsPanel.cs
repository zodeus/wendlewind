namespace Wendlewind.Scenes.MainGameScene.Gui.Widgets.EntityWidgets;

internal sealed class ItemEnchantmentSocketsPanel : HorizontalStackPanel
{
    private readonly BaseGui _gui;
    private readonly Item _item;
    private int _currentSocketCount;
    private readonly SelectionPopup<Item> _selectionPopup;

    public ItemEnchantmentSocketsPanel(BaseGui gui, Item item)
    {
        _gui = gui;
        _item = item;
        _selectionPopup = new SelectionPopup<Item>(gui.Desktop);
        var maxEnchantments = item.Enchantments?.MaxEnchantments;
        if (!(maxEnchantments > 0)) return;
        _currentSocketCount = maxEnchantments.Value;
        for (var i = 0; i < maxEnchantments; i++)
        {
            Widgets.Add(GenerateSocket(i));
        }
    }

    private CursorButton GenerateSocket(int index)
    {
        var socket = new CursorButton
        {
            //Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.DeepGold],
            Padding = new Thickness(8),
            Width = 64, Height = 64
        };
        if (_item.Enchantments?.TryGetAtSocket(index) is { } enchantment)
        {
            socket.Content = new Image { Width = 64, Height = 64, Background = enchantment.GetIconImage() };
        }
        else
        {
            socket.Content = new Image { Width = 64, Height = 64, Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.RoundElite64] };
        }

        var position = index;
        socket.Click += (_, _) =>
        {
            var existing = _item.Enchantments?.TryGetAtSocket(position);
            if (existing != null)
            {
                _gui.ViewEntity(existing);
                return;
            }

            if (_gui.MouseAttachment?.Data is Item { ItemDef.ItemType: ItemType.Enchantment } e)
            {
                if (e.ItemDef.EnchantmentProperties?.ValidEquipmentTypes.Contains(_item.ItemDef.EquipmentProperties!.EquipmentType) == false) return;

                e.EjectFromContainer();
                _gui.MouseAttachment.Detach();
                _item.Enchantments!.TryAdd(e, position);
                socket.Content = new Image { Width = 58, Height = 58, Background = e.GetIconImage() };
                return;
            }

            // Show enchantment selection popup for empty sockets
            ShowEnchantmentSelectionPopup(position, socket);
        };
        return socket;
    }

    private void ShowEnchantmentSelectionPopup(int socketIndex, CursorButton socketButton)
    {
        if (_selectionPopup.IsOpen) return;

        var pawn = Core.Context?.PlayerPawn;
        if (pawn == null) return;

        // Find available enchantments in inventory that can be socketed into this item
        var equipmentType = _item.ItemDef.EquipmentProperties?.EquipmentType;
        if (equipmentType == null) return;

        var availableEnchantments = pawn.Inventory
            .Where(i => i.ItemDef.ItemType == ItemType.Enchantment &&
                       i.ItemDef.EnchantmentProperties?.ValidEquipmentTypes.Contains(equipmentType.Value) == true);

        _selectionPopup.Show(
            availableEnchantments,
            e => e.GetIcon(),
            e => SocketEnchantmentFromInventory(socketIndex, e, socketButton));
    }

    private void SocketEnchantmentFromInventory(int socketIndex, Item enchantment, CursorButton socketButton)
    {
        enchantment.EjectFromContainer();
        _item.Enchantments!.TryAdd(enchantment, socketIndex);
        socketButton.Content = new Image { Width = 58, Height = 58, Background = enchantment.GetIconImage() };
    }

    public void Update()
    {
        _selectionPopup.Update();

        if (_item.Enchantments?.MaxEnchantments > _currentSocketCount)
        {
            _currentSocketCount++;
            Widgets.Add(GenerateSocket(_currentSocketCount - 1));
        }
    }
}