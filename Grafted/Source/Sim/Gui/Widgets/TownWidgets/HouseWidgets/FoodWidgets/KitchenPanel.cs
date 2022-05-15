using Grafted.Definitions;
using Grafted.Sim.Entities.Items;
using Grafted.Utils;
using Myra.Graphics2D.TextureAtlases;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.Styles;

namespace Grafted.Sim.Gui.Widgets.TownWidgets.HouseWidgets.FoodWidgets;

public class KitchenPanel : VerticalStackPanel {
    private readonly TownStructureHouse _house;
    private RecipePanel? _recipePanel;
    private ImageButton _foodButton1;
    private ImageButton _foodButton2;
    private ImageButton _foodButton3;
    private readonly MeatRackPanel _meatRackPanel;
    private bool _isFoodButton1Showing;
    private bool _isFoodButton2Showing;
    private bool _isFoodButton3Showing;

    public KitchenPanel(TownStructureHouse house) {
        _house = house;
        Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrame];
        Spacing = 15;
        _foodButton1 = GenerateFoodButton();
        _foodButton2 = GenerateFoodButton();
        _foodButton3 = GenerateFoodButton();
        _meatRackPanel = new MeatRackPanel(house);
        Panel foodToCookPanel = new() { Height = 350 };
        AddChild(new Label(BaseContent.Styles.Label.Large) { Text = "Kitchen" });
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
                    DefRepository<ItemDef>.Defs.FindAll(i => i == Defs.Items.CookedMeat || i == Defs.Items.CookedCorn),
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
        //button 1
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

        //button 2
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

        //button 3
        if (_isFoodButton3Showing == false && _house.AmountOfItem(Defs.Items.CookedCorn) > 0) {
            _isFoodButton3Showing = true;
            _foodButton3.Image = new TextureRegion(Defs.Items.CookedCorn.Icon);
            _foodButton3.Enabled = true;
            _foodButton3.Tag = Defs.Items.CookedCorn;
        }
        else if (_isFoodButton3Showing && _house.AmountOfItem(Defs.Items.CookedCorn) <= 0) {
            _foodButton3.Image = null;
            _foodButton3.Enabled = false;
            _isFoodButton3Showing = false;
        }

        if (_isFoodButton3Showing) {
            _foodButton3.Enabled = Core.Sim.World.PlayerPawn.IsHungry;
        }
        
        _meatRackPanel.Update();
    }
}