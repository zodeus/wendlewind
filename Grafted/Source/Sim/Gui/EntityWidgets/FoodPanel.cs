using Grafted.Definitions;
using Grafted.Sim.Entities.Items;
using Myra.Graphics2D;
using Myra.Graphics2D.TextureAtlases;
using Myra.Graphics2D.UI;

namespace Grafted.Sim.Gui.EntityWidgets;

public class FoodPanel : EntityPanelBase {
    private readonly TextButton _eatButton;

    public FoodPanel(Item item, EntityPanelProperties? properties = null) : base(item, properties) {
        Padding = new Thickness(20);
        MinWidth = 300;
        Spacing = 5;
        AddChild(new Image { Background = new TextureRegion(item.Icon), Width = 48, Height = 48 });
        AddChild(new Label("small") { Text = item.Def.Description, Wrap = true, Margin = new Thickness(10) });

        if (item.ItemDef == Defs.Items.CookedMeat) {
            _eatButton = new TextButton(BaseContent.Styles.Button.Normal) { Text = "Eat" };
            _eatButton.Click += (_, _) => {
                Core.Sim.World.PlayerPawns[0].TryEat(item);
            };
            AddChild(_eatButton);
        }
    }

    public override void Update() {
        if (_eatButton !=null) {
            _eatButton.Enabled = Core.Sim.World.PlayerPawn.IsHungry;    
        }
    }
}