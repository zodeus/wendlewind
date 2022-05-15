using System.Linq;
using Grafted.Definitions;
using Grafted.Sim.Entities;
using Grafted.Sim.Entities.Items;
using Myra.Graphics2D;
using Myra.Graphics2D.TextureAtlases;
using Myra.Graphics2D.UI;

namespace Grafted.Sim.Gui.Widgets.EntityWidgets;

public class FoodPanel : EntityPanelBase {
    private readonly TextButton _eatButton;

    public FoodPanel(Item item, EntityPanelProperties? properties = null) : base(item, properties) {
        Padding = new Thickness(20);
        MinWidth = 300;
        Spacing = 5;
        AddChild(new Image { Background = new TextureRegion(item.Icon), Width = 64, Height = 64 });
        AddChild(new Label("small") { Text = item.Def.Description, Wrap = true, Margin = new Thickness(0, 10, 0, 10) });
        AddChild(new Label() { Text = $"Nutritional Value: {item.GetStatValue(Defs.Stats.NutritionalValue)}", Wrap = true });
        if (item.ItemDef.FoodProperties?.Effects.Any() == true) {
            AddChild(new Label() {
                Text = $"Effects: {string.Join(", ", item.ItemDef.FoodProperties.Effects.Select(e => e.Def.Label))}", Wrap = true
            });
        }

        _eatButton = new TextButton(BaseContent.Styles.Button.Normal) { Text = "Eat", Margin = new Thickness(0, 20, 0, 0) };
        _eatButton.Click += (_, _) => {
            Core.Sim.World.PlayerPawns[0].TryEat(item);
        };
        AddChild(_eatButton);
    }

    public override void Update() {
        _eatButton.Enabled = Core.Sim.World.PlayerPawn.IsHungry;
    }
}