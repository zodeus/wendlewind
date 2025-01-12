using System.Globalization;
using Grafted.Sim.Entities;

namespace Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets;

public class WeaponPanel : EntityPanelBase
{
    private readonly Item _item;
    private readonly Label _durabilityLabel;
    private readonly HorizontalProgressBar _durabilityBar;

    public WeaponPanel(BaseGui gui, Item item, EntityPanelProperties? properties = null) : base(gui, item, properties)
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
            Text = $"Durability: {item.Durability}/{item.MaxDurability}", Margin = new Thickness(0, 0, 0, 15)
        };
        AddChild(new Image { Background = new TextureRegion(item.Icon), Width = 64, Height = 64 });
        AddChild(_durabilityBar);
        AddChild(_durabilityLabel);
        if (item.Def.Description != "undefined")
        {
            AddChild(new Label("small") { Text = item.Def.Description, Wrap = true, MaxWidth = 400 });
        }
        AddChild(new Label("small") { Text = $"Damage Type: {item.ItemDef.WeaponProperties.DamageType}" });
        AddChild(new Label("small") { Text = $"Slot: {(item.ItemDef.EquipmentProperties.SlotUsedToEquip != null ? item.ItemDef.EquipmentProperties.SlotUsedToEquip : "n/a")}" });

        foreach (BaseStat baseStat in item.Def.BaseStats)
        {
            var row = new HorizontalStackPanel { Spacing = 10 };
            row.AddChild(new Label("small") { Text = $"{baseStat.Def.Label}:" });
            row.AddChild(new Label("small") { Text = item.GetStatValue(baseStat.Def).ToString(CultureInfo.InvariantCulture) });
            AddChild(row);

            /*row.RegisterCallback<MouseEnterEvent>(evt => {
                key.AddToClassList("text--hover");
                value.AddToClassList("text--hover");
            });
            row.RegisterCallback<MouseLeaveEvent>(evt => {
                key.RemoveFromClassList("text--hover");
                value.RemoveFromClassList("text--hover");
            });*/
        }

        var destroyButton = new TextButton(BaseContent.Styles.Button.Small) { Text = "Destroy", Margin = new Thickness(0, 10, 0, 0) };
        destroyButton.Click += (_, _) => { item.Destroy(); };
        AddChild(destroyButton);
    }

    public override void Update()
    {
        _durabilityBar.Value = _item.Durability / _item.MaxDurability * 100;
        _durabilityLabel.Text = $"Durability: {_item.Durability}/{_item.MaxDurability}";
    }
}