using System.Globalization;

namespace Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets;

[UsedImplicitly]
public sealed class ArmorPanel : EntityPanelBase
{
    private readonly Item _item;
    private readonly Label _durabilityLabel;
    private readonly HorizontalProgressBar _durabilityBar;
    private readonly ItemEnchantmentSocketsPanel _socketsPanel;

    public ArmorPanel(BaseGui gui, Item item, EntityPanelProperties? properties = null) : base(gui, item, properties)
    {
        _item = item;
        Padding = new Thickness(20);
        MinWidth = 300;
        _durabilityBar = new HorizontalProgressBar(BaseContent.Styles.Bar.Durability)
        {
            Width = 100, Height = 20,
            VerticalAlignment = VerticalAlignment.Bottom
        };

        _durabilityLabel = new Label("small")
        {
            Text = $"Durability: {item.Durability}/{item.MaxDurability}", Margin = new Thickness(0, 5, 0, 0)
        };
        Widgets.Add(new HorizontalStackPanel
        {
            Spacing = 10,
            Widgets =
            {
                new Image { Background = new TextureRegion(item.Icon), Width = 128, Height = 128 },
                new Label(BaseContent.Styles.Label.Normal)
                {
                    Text = item.Def.Description, Wrap = true, MaxWidth = 500,
                    Margin = new Thickness(0, 10, 0, 0)
                },
            }
        });
        Widgets.Add(_durabilityBar);
        Widgets.Add(_durabilityLabel);
        if (item.Def.Description != "undefined")
        {
            Widgets.Add(new Label("small") { Text = item.Def.Description, Wrap = true, MaxWidth = 400 });
        }

        Widgets.Add(new Label("small") { Text = $"Equipment Type: {item.ItemDef.EquipmentProperties?.EquipmentType}" });
        Widgets.Add(new Label("small") { Text = $"Slot: {(item.ItemDef.EquipmentProperties?.SlotUsedToEquip != null ? item.ItemDef.EquipmentProperties.SlotUsedToEquip : "n/a")}" });

        foreach (var baseStat in item.Def.BaseStats)
        {
            var row = new HorizontalStackPanel { Spacing = 10 };
            row.Widgets.Add(new Label("small") { Text = $"{baseStat.Def.Label}:" });
            row.Widgets.Add(new Label("small") { Text = item.GetStatValue(baseStat.Def).ToString(CultureInfo.InvariantCulture) });
            Widgets.Add(row);
        }

        _socketsPanel = new ItemEnchantmentSocketsPanel(gui, item)
        {
            Margin = new Thickness(0, 10, 0, 10)
        };
        Widgets.Add(_socketsPanel);
    }


    public override void Update()
    {
        _durabilityLabel.Text = $"Durability: {_item.Durability}/{_item.MaxDurability}";
        _durabilityBar.Value = _item.Durability / _item.MaxDurability * 100;
        _socketsPanel.Update();
    }
}