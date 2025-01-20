namespace Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets;

internal sealed class ItemEnchantmentSlotsPanel : HorizontalStackPanel
{
    public ItemEnchantmentSlotsPanel(BaseGui gui, Item item)
    {
        var maxEnchantments = item.Enchantments?.MaxEnchantments;
        if (!(maxEnchantments > 0)) return;

        for (var i = 0; i < maxEnchantments; i++)
        {
            var slot = new Button
            {
                //Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.DeepGold],
                Padding = new Thickness(8),
                Width = 64, Height = 64
            };
            if (item.Enchantments?.TryGetAtSlot(i) is { } enchantment)
            {
                slot.Content = new Image { Width = 64, Height = 64, Background = new TextureRegion(enchantment.Icon) };
            }
            else
            {
                slot.Content = new Image { Width = 64, Height = 64, Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.RoundElite64] };
            }

            var position = i;
            slot.Click += (_, _) =>
            {
                var existing = item.Enchantments?.TryGetAtSlot(position);
                if (existing != null)
                {
                    gui.ViewEntity(existing);
                    return;
                }

                if (gui.MouseAttachment?.Data is not Item { ItemDef.ItemType: ItemType.Enchantment } e) return;
                if (e.ItemDef.EnchantmentProperties?.ValidEquipmentTypes.Contains(item.ItemDef.EquipmentProperties!.EquipmentType) == false) return;

                e.EjectFromContainer();
                gui.MouseAttachment.Detach();
                item.Enchantments!.TryAdd(e, position);
                slot.Content = new Image { Width = 58, Height = 58, Background = new TextureRegion(e.Icon) };
            };

            Widgets.Add(slot);
        }
    }
}