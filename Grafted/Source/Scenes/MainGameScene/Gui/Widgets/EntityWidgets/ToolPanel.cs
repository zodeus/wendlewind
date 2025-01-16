using System.Globalization;
using Grafted.Sim.Entities;

namespace Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets;

public class ToolPanel : EntityPanelBase {
    public ToolPanel(BaseGui gui, Item item, EntityPanelProperties? properties = null) : base(gui, item, properties) {
        Padding = new Thickness(20);
        MinWidth = 300;
        Spacing = 5;
        Widgets.Add(new Image { Background = new TextureRegion(item.Icon), Width = 48, Height = 48 });
        Widgets.Add(new Label("small") { Text = item.Def.Description, Wrap = true, Margin = new Thickness(10) });
        Widgets.Add(new Label("small") { Text = $"Tool Type: {item.ItemDef.ToolType}" });
        Widgets.Add(new Label("small") { Text = $"Equipment Type: {item.ItemDef.EquipmentProperties.EquipmentType}" });
        Widgets.Add(new Label("small") { Text = $"Slot: {(item.ItemDef.EquipmentProperties.SlotUsedToEquip != null ? item.ItemDef.EquipmentProperties.SlotUsedToEquip : "n/a")}" });
        Widgets.Add(new Label("small") { Text = $"Damage Type: {item.ItemDef.WeaponProperties.DamageType}" });

        foreach (BaseStat baseStat in item.Def.BaseStats) {
            var row = new HorizontalStackPanel { Spacing = 10 };
            row.Widgets.Add(new Label("small") { Text = $"{baseStat.Def.Label}:" });
            row.Widgets.Add(new Label("small") { Text = item.GetStatValue(baseStat.Def).ToString(CultureInfo.InvariantCulture) });
            Widgets.Add(row);

            /*row.RegisterCallback<MouseEnterEvent>(evt => {
                key.AddToClassList("text--hover");
                value.AddToClassList("text--hover");
            });
            row.RegisterCallback<MouseLeaveEvent>(evt => {
                key.RemoveFromClassList("text--hover");
                value.RemoveFromClassList("text--hover");
            });*/
        }
    }

    public override void Update() { }
}