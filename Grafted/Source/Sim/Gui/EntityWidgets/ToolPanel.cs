using System.Globalization;
using Grafted.Sim.Entities;
using Grafted.Sim.Entities.Items;
using Myra.Graphics2D;
using Myra.Graphics2D.TextureAtlases;
using Myra.Graphics2D.UI;

namespace Grafted.Sim.Gui.EntityWidgets;

public class ToolPanel : EntityPanelBase {
    public ToolPanel(Item item, EntityPanelProperties? properties = null) : base(item, properties) {
        Padding = new Thickness(20);
        MinWidth = 300;
        Spacing = 5;
        AddChild(new Image { Background = new TextureRegion(item.Icon), Width = 48, Height = 48 });
        AddChild(new Label("small") { Text = item.Def.Description, Wrap = true, Margin = new Thickness(10) });
        AddChild(new Label("small") { Text = $"Tool Type: {item.ItemDef.ToolType}" });
        AddChild(new Label("small") { Text = $"Tool Categories: {string.Join(", ", item.ItemDef.ToolCategories)}" });
        AddChild(new Label("small") { Text = $"Equipment Type: {item.ItemDef.EquipmentProperties.EquipmentType}" });
        AddChild(new Label("small") { Text = $"Slot: {(item.ItemDef.EquipmentProperties.SlotUsedToEquip != null ? item.ItemDef.EquipmentProperties.SlotUsedToEquip : "n/a")}" });
        AddChild(new Label("small") { Text = $"Damage Type: {item.ItemDef.DamageType}" });

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
    }

    public override void Update() { }
}