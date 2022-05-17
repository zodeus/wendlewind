using Grafted.Definitions;
using Grafted.Sim.Entities.Items;
using Grafted.Utils;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.Styles;

namespace Grafted.Sim.Gui.Widgets.TownWidgets.HouseWidgets.FoodWidgets;

public class AlchemyPanel : VerticalStackPanel {
    private readonly TownStructureHouse _house;

    private RecipePanel? _recipePanel;

    public AlchemyPanel(TownStructureHouse house) {
        _house = house;
        Spacing = 15;
        Padding = new Thickness(20);
        Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrame];
        base.Visible = false;
        Panel itemToBrewPanel = new() { MinHeight = 380 };
        AddChild(new Label(BaseContent.Styles.Label.Large) { Text = "Alchemy" });
        AddChild(new HorizontalSeparator());
        AddChild(new Label { Text = $"The brewing barrel is empty" });
        AddChild(new RecipePicker(
            DefRepository<ItemDef>.Defs.FindAll(i => i == Defs.Items.BalmyOintment),
            (sender, _) => {
                ListItem comboItem = ((ListBox) sender!).SelectedItem;
                itemToBrewPanel.Widgets.Clear();
                if (comboItem.Tag == null) { return; }

                _recipePanel = new RecipePanel(house, (ItemDef) comboItem.Tag, "Start Brewing");
                itemToBrewPanel.AddChild(_recipePanel);
            }
        ) {
            VerticalAlignment = VerticalAlignment.Bottom
        });

        AddChild(new HorizontalSeparator());
        AddChild(itemToBrewPanel);
    }



    public void Update() {
        if (Visible == false && _house.HasMeatRack) {
            Visible = true;
        }

        _recipePanel?.Update();
    }
}