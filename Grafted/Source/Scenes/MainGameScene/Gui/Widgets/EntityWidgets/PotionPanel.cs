using Grafted.Sim.Entities;

namespace Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets;

public class PotionPanel : EntityPanelBase {
    public PotionPanel(BaseGui gui, Item item, EntityPanelProperties? properties = null) : base(gui, item, properties) {
        Padding = new Thickness(20);
        MinWidth = 300;
        Spacing = 5;
        AddChild(new Image { Background = new TextureRegion(item.Icon), Width = 48, Height = 48 });
        AddChild(new Label(BaseContent.Styles.Label.Small) { Text = item.Def.Description, Wrap = true, Margin = new Thickness(10), Width = 600});

        if (item.ItemDef == Defs.Items.JarOfBlood) {
            TextButton button = new(BaseContent.Styles.Button.Normal) { Text = "Sip" };
            button.Click += (_, _) => {
                float amount = item.GetStatValue(Defs.Stats.HealingValue);
                Core.Context.PlayerPawn.Body.BloodAmount += amount;
                item.StackSize--;
                if (item.StackSize < 1) {
                    item.Destroy();
                }
                Core.Context.TickOnce();
            };
            AddChild(button);
        }
    }

    public override void Update() { }
}