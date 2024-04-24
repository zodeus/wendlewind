using Grafted.Sim.Entities;

namespace Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets;

public class FoodPanel : EntityPanelBase {
    private readonly TextButton _eatButton;

    public FoodPanel(BaseGui gui, Item item, EntityPanelProperties? properties = null) : base(gui, item, properties) {
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
            Core.Context.PlayerPawn.TryEat(item);
        };
        AddChild(_eatButton);
    }

    public override void Update() {
        _eatButton.Enabled = Core.Context.World.PlayerPawn.IsHungry;
    }
}