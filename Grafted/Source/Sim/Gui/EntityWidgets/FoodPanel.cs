using Grafted.Definitions;
using Grafted.Sim.Entities;
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
                float amount = item.GetStatValue(Defs.Stats.NutritionalValue);
                Core.Sim.World.ProgressTime(SimTime.MinutesToSeconds(5));
                Core.Sim.World.PlayerPawns[0].Body.StomachLevel = 1;
                Core.Sim.World.PlayerPawns[0].Body.Energy += .3f;
                Core.Sim.World.ProgressTime(SimTime.MinutesToSeconds(5));
                Core.Sim.Messages.Push(new Message(
                    $"\\c[{UiTextColor.TextColorPawn}]{Core.Sim.World.PlayerPawns[0].Label} \\c[{UiTextColor.TextColorDefault}]ate \\c[{UiTextColor.TextColorItem}]{item.Label}"
                ));
                item.StackSize--;
                if (item.StackSize < 1) {
                    item.Destroy();
                }
            };
            AddChild(_eatButton);
        }
    }

    public override void Update() {
        _eatButton.Enabled = Core.Sim.World.PlayerPawns[0].Body.StomachLevel < .7;
    }
}