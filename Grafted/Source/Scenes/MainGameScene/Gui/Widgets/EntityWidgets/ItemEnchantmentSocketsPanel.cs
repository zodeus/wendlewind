namespace Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets;

internal sealed class ItemEnchantmentSocketsPanel : HorizontalStackPanel
{
    private readonly BaseGui _gui;
    private readonly Item _item;
    private int _currentSocketCount;

    public ItemEnchantmentSocketsPanel(BaseGui gui, Item item)
    {
        _gui = gui;
        _item = item;
        var maxEnchantments = item.Enchantments?.MaxEnchantments;
        if (!(maxEnchantments > 0)) return;
        _currentSocketCount = maxEnchantments.Value;
        for (var i = 0; i < maxEnchantments; i++)
        {
            Widgets.Add(GenerateSocket(i));
        }
    }

    private Button GenerateSocket(int index)
    {
        var socket = new Button
        {
            //Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.DeepGold],
            Padding = new Thickness(8),
            Width = 64, Height = 64
        };
        if (_item.Enchantments?.TryGetAtSocket(index) is { } enchantment)
        {
            socket.Content = new Image { Width = 64, Height = 64, Background = new TextureRegion(enchantment.Icon) };
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

            if (_gui.MouseAttachment?.Data is not Item { ItemDef.ItemType: ItemType.Enchantment } e) return;
            if (e.ItemDef.EnchantmentProperties?.ValidEquipmentTypes.Contains(_item.ItemDef.EquipmentProperties!.EquipmentType) == false) return;

            e.EjectFromContainer();
            _gui.MouseAttachment.Detach();
            _item.Enchantments!.TryAdd(e, position);
            socket.Content = new Image { Width = 58, Height = 58, Background = new TextureRegion(e.Icon) };
        };
        return socket;
    }

    public void Update()
    {
        if (_item.Enchantments?.MaxEnchantments > _currentSocketCount)
        {
            _currentSocketCount++;
            Widgets.Add(GenerateSocket(_currentSocketCount - 1));
        }
    }
}