using Grafted.Definitions;
using Grafted.Sim.Entities;
using Grafted.Sim.Entities.Items;
using Myra.Graphics2D;
using Myra.Graphics2D.TextureAtlases;
using Myra.Graphics2D.UI;

namespace Grafted.Sim.Gui.EntityWidgets;

public class PotionPanel : EntityPanelBase {
    public PotionPanel(Item item, EntityPanelProperties? properties = null) : base(item, properties) {
        Padding = new Thickness(20);
        MinWidth = 300;
        Spacing = 5;
        AddChild(new Image { Background = new TextureRegion(item.Icon), Width = 48, Height = 48 });
        AddChild(new Label("small") { Text = item.Def.Description, Wrap = true, Margin = new Thickness(10) });

        if (item.ItemDef == Defs.Items.JarOfBlood) {
            TextButton button = new(BaseContent.Styles.Button.Normal) { Text = "Sip" };
            button.Click += (_, _) => {
                float amount = item.GetStatValue(Defs.Stats.HealingValue);
                Core.Sim.World.PlayerPawns[0].Body.BloodAmount += amount;
                item.StackSize--;
                if (item.StackSize < 1) {
                    item.Destroy();
                }
            };
            AddChild(button);
        }
    }

    public override void Update() { }
}