using System.Globalization;
using Grafted.Sim.Entities;

namespace Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets;

public class ArmorPanel : EntityPanelBase {
    private readonly Item _item;
    private readonly Label _durabilityLabel;

    public ArmorPanel(BaseGui gui, Item item, EntityPanelProperties? properties = null) : base(gui, item, properties) {
        _item = item;
        Padding = new Thickness(20);
        MinWidth = 300;
        _durabilityLabel = new Label("small") {
            Text = $"Durability: {item.Durability}/{item.MaxDurability}", Margin = new Thickness(0, 0, 0, 15)
        };
        AddChild(new Image { Background = new TextureRegion(item.Icon), Width = 48, Height = 48 });
        AddChild(new Label("small") { Text = item.Def.Description, Wrap = true, Margin = new Thickness(10) });
        AddChild(_durabilityLabel);
        AddChild(new Label("small") { Text = $"Equipment Type: {item.ItemDef.EquipmentProperties.EquipmentType}" });
        AddChild(new Label("small") { Text = $"Slot: {(item.ItemDef.EquipmentProperties.SlotUsedToEquip != null ? item.ItemDef.EquipmentProperties.SlotUsedToEquip : "n/a")}" });

        foreach (BaseStat baseStat in item.Def.BaseStats) {
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

    public override void Update() {
        _durabilityLabel.Text = $"Durability: {_item.Durability}/{_item.MaxDurability}";
    }
}