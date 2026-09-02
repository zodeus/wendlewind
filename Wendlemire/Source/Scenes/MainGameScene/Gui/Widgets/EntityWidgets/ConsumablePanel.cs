using System.Globalization;

namespace Wendlemire.Scenes.MainGameScene.Gui.Widgets.EntityWidgets;

public class ConsumablePanel : EntityPanelBase {
    private readonly Item _item;
    private readonly Label _stackLabel;

    public ConsumablePanel(BaseGui gui, Item item, EntityPanelProperties? properties = null) : base(gui, item, properties) {
        _item = item;
        EntityCardChrome.ApplyCard(this);
        _stackLabel = new Label("small") {
            Text = $"Stack: x{item.StackSize}",
            Visible = item.IsStackable
        };
        Widgets.Add(EntityCardChrome.Header(item));
        Widgets.Add(_stackLabel);

        foreach (var baseStat in item.Def.BaseStats) {
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

    public override void Update() {
        _stackLabel.Text = $"Stack: x{_item.StackSize}";
    }
}