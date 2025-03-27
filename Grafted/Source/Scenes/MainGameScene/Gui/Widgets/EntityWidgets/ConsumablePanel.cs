using System.Globalization;
using Grafted.Sim.Entities;

namespace Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets;

public class ConsumablePanel : EntityPanelBase {
    private readonly Item _item;
    private readonly Label _stackLabel;

    public ConsumablePanel(BaseGui gui, Item item, EntityPanelProperties? properties = null) : base(gui, item, properties) {
        _item = item;
        Padding = new Thickness(20);
        MinWidth = 300;
        _stackLabel = new Label("small") {
            Text = $"Stack Size: x{item.StackSize}", Margin = new Thickness(0, 0, 0, 15),
            Visible = item.IsStackable

        };
        Widgets.Add(new HorizontalStackPanel
        {
            Spacing = 10,
            Widgets =
            {
                new Image { Background = new TextureRegion(item.Icon), Width = 128, Height = 128 },
                new Label(BaseContent.Styles.Label.Normal)
                {
                    Text = item.Def.Description, Wrap = true, MaxWidth = 400,
                    Margin = new Thickness(0, 10, 0, 0)
                },
            }
        });
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
        _stackLabel.Text = $"Stack Size: x{_item.StackSize}";
    }
}