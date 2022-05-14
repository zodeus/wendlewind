using Grafted.Definitions;
using Grafted.Sim.Entities.Items;
using Grafted.Utils;
using Myra.Graphics2D.TextureAtlases;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.Styles;

namespace Grafted.Sim.Gui.Widgets.TownWidgets.HouseWidgets.FoodWidgets;

public class FoodPanel : VerticalStackPanel {
    private readonly TownStructureHouse _house;
    private RecipePanel? _recipePanel;
    private ImageButton _foodButton1;
    private ImageButton _foodButton2;
    private ImageButton _foodButton3;
    private readonly MeatRackPanel _meatRackPanel;
    private bool _isFoodButton1Showing;
    private bool _isFoodButton2Showing;

    public FoodPanel(TownStructureHouse house) {
        _house = house;
        Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrame];
        Spacing = 15;
        _foodButton1 = GenerateFoodButton();
        _foodButton2 = GenerateFoodButton();
        _foodButton3 = GenerateFoodButton();
        _meatRackPanel = new MeatRackPanel(house);
        Panel foodToCookPanel = new() { Height = 250 };
        AddChild(new Label(BaseContent.Styles.Label.Large) { Text = "Foods" });
        AddChild(_meatRackPanel);
        AddChild(new HorizontalSeparator());
        AddChild(new Grid {
            ColumnsProportions = {
                Proportion.Fill,
                Proportion.Auto
            },
            ColumnSpacing = 5,
            Widgets = {
                new RecipePicker(
                    DefRepository<ItemDef>.Defs.FindAll(i => i == Defs.Items.CookedMeat),
                    (sender, _) => {
                        ListItem comboItem = ((ListBox) sender!).SelectedItem;
                        foodToCookPanel.Widgets.Clear();
                        if (comboItem.Tag == null) { return; }

                        _recipePanel = new RecipePanel(house, (ItemDef) comboItem.Tag);
                        foodToCookPanel.AddChild(_recipePanel);
                    }
                ) {
                    VerticalAlignment = VerticalAlignment.Bottom
                },
                new VerticalStackPanel {
                    GridColumn = 1,
                    Spacing = 5,
                    Widgets = {
                        new Label { Text = "Available" },
                        new HorizontalStackPanel {
                            Spacing = 5,
                            Widgets = { _foodButton1, _foodButton2, _foodButton3 }
                        }
                    }
                }
            }
        });

        AddChild(new HorizontalSeparator());
        AddChild(foodToCookPanel);
    }

    private ImageButton GenerateFoodButton() {
        ImageButton button = new(BaseContent.Styles.Button.Icon) {
            Width = 24, Height = 24, Enabled = false
        };
        button.Click += (sender, _) => {
            Core.Sim.World.PlayerPawn.TryEat(_house.TakeItem((ItemDef) ((ImageButton) sender!).Tag, 1)!);

        };
        return button;
    }

    public void Update() {
        _recipePanel?.Update();
        if (_isFoodButton1Showing == false && _house.AmountOfItem(Defs.Items.CookedMeat) > 0) {
            _isFoodButton1Showing = true;
            _foodButton1.Image = new TextureRegion(Defs.Items.CookedMeat.Icon);
            _foodButton1.Enabled = true;
            _foodButton1.Tag = Defs.Items.CookedMeat;
        }
        else if (_isFoodButton1Showing && _house.AmountOfItem(Defs.Items.CookedMeat) <= 0) {
            _foodButton1.Image = null;
            _foodButton1.Enabled = false;
            _isFoodButton1Showing = false;
        }

        if (_isFoodButton1Showing) {
            _foodButton1.Enabled = Core.Sim.World.PlayerPawn.IsHungry;
        }

        if (_isFoodButton2Showing == false && _house.AmountOfItem(Defs.Items.DriedMeat) > 0) {
            _isFoodButton2Showing = true;
            _foodButton2.Image = new TextureRegion(Defs.Items.DriedMeat.Icon);
            _foodButton2.Enabled = true;
            _foodButton2.Tag = Defs.Items.DriedMeat;
        }
        else if (_isFoodButton2Showing && _house.AmountOfItem(Defs.Items.DriedMeat) <= 0) {
            _foodButton2.Image = null;
            _foodButton2.Enabled = false;
            _isFoodButton1Showing = false;
        }

        if (_isFoodButton2Showing) {
            _foodButton2.Enabled = Core.Sim.World.PlayerPawn.IsHungry;
        }

        _meatRackPanel.Update();
    }
}