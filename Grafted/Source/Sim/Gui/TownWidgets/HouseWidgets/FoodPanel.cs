using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Grafted.Definitions;
using Grafted.Sim.Entities.Items;
using Grafted.Utils;
using Myra.Graphics2D;
using Myra.Graphics2D.TextureAtlases;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.Styles;

namespace Grafted.Sim.Gui.TownWidgets.HouseWidgets;

public class FoodPanel : VerticalStackPanel {
    private readonly TownStructureHouse _house;
    private RecipePanel? _recipePanel;
    private ImageButton _foodButton1;
    private ImageButton _foodButton2;
    private ImageButton _foodButton3;
    private bool _IsFoodButton1Showing;

    public FoodPanel(TownStructureHouse house) {
        _house = house;
        Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrame];
        Spacing = 15;
        _foodButton1 = GenerateFoodButton();
        _foodButton2 = GenerateFoodButton();
        _foodButton3 = GenerateFoodButton();
        Panel foodToCookPanel = new();
        AddChild(new Label(BaseContent.Styles.Label.Medium) { Text = "Food" });
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
        if (_IsFoodButton1Showing == false && _house.AmountOfItem(Defs.Items.CookedMeat) > 0) {
            _IsFoodButton1Showing = true;
            _foodButton1.Image = new TextureRegion(Defs.Items.CookedMeat.Icon);
            _foodButton1.Enabled = true;
            _foodButton1.Tag = Defs.Items.CookedMeat;
        }
        else if (_IsFoodButton1Showing && _house.AmountOfItem(Defs.Items.CookedMeat) <= 0) {
            _foodButton1.Image = null;
            _foodButton1.Enabled = false;
            _IsFoodButton1Showing = false;
        }

        if (_IsFoodButton1Showing) {
            _foodButton1.Enabled = Core.Sim.World.PlayerPawn.IsHungry;
        }
    }
}

public class RecipePanel : VerticalStackPanel {
    private readonly TownStructureHouse _house;
    private readonly ItemDef _food;
    private readonly TextButton _cookButton;
    private int _amountWanted;
    private Dictionary<ResourceCount, Label> _ingredients = new();

    public RecipePanel(TownStructureHouse house, ItemDef food) {
        _house = house;
        _food = food;
        Label icon = new() { Text = food.Label };
        //icon.TouchDown += (sender, args) => Core.Sim.ActiveGui.ViewEntity(item);
        AddChild(icon);
        AddChild(new Image { Background = new TextureRegion(food.Icon), Margin = new Thickness(0, 5, 0, 0), Width = 48, Height = 48 });
        AddChild(GenerateAmountWantedPanel());
        AddChild(new Label("small") { Text = $"Time to make: {food.CraftingProperties.MinutesToMake}/hours" });
        AddChild(new Label("small") { Text = $"Yield: {food.CraftingProperties.AmountProduced}x" });
        if (food.CraftingProperties.RequiredTools != null) {
            AddChild(new Label("small") { Text = "Required Tools: ", VerticalAlignment = VerticalAlignment.Center });
            foreach (ItemDef requiredTool in food.CraftingProperties.RequiredTools) {
                AddChild(new HorizontalStackPanel {
                    Margin = new Thickness(10, 5, 0, 0),
                    Spacing = 8,
                    Widgets = {
                        new Image() { Background = new TextureRegion(requiredTool.Icon), Width = 24, Height = 24, VerticalAlignment = VerticalAlignment.Center },
                        new Label("small") { Text = requiredTool.Label, VerticalAlignment = VerticalAlignment.Center }
                    }
                });
            }
        }

        AddChild(new Label("small") { Text = "Ingredients:" });
        foreach (ResourceCount requirement in food.CraftingProperties.ResourceRequirements) {
            HorizontalStackPanel row = new() { Margin = new Thickness(10, 5, 0, 0) };

            if (requirement.MaterialType != null) {
                row.AddChild(new Label("small") { Text = $"{requirement.Count}x {requirement.MaterialType} T{requirement.MinMaterialTier}+", VerticalAlignment = VerticalAlignment.Center });
            }

            if (requirement.Resource != null) {
                Label resourceLabel = new("small") { VerticalAlignment = VerticalAlignment.Center };
                _ingredients.Add(requirement, resourceLabel);

                row.AddChild(new Image { Background = new TextureRegion(requirement.Resource!.Icon), Width = 24, Height = 24, Margin = new Thickness(0, 0, 10, 0) });
                row.AddChild(resourceLabel);
            }

            AddChild(row);
        }

        _cookButton = new TextButton(BaseContent.Styles.Button.Normal) { Text = "Cook Food", Margin = new Thickness(0, 10, 0, 0) };
        _cookButton.Click += (_, _) => _house.CraftItem(food, _amountWanted);
        AddChild(_cookButton);
    }

    private HorizontalStackPanel GenerateAmountWantedPanel() {
        _amountWanted = 1;
        TextBox textBox = new() {
            Width = 40, Text = _amountWanted.ToString(),
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        textBox.ValueChanging += (_, args) => args.NewValue = Regex.Replace(args.NewValue, "[^0-9]", "");
        textBox.TextChanged += (_, args) => {
            int amount = args.NewValue == "" ? 0 : int.Parse(args.NewValue);
            _amountWanted = amount;
        };

        ImageButton increaseAmount = new(BaseContent.Styles.Button.Plus24) { VerticalAlignment = VerticalAlignment.Center };
        increaseAmount.Click += (_, _) => {
            _amountWanted++;
            textBox.Text = _amountWanted.ToString();
        };
        ImageButton decreaseAmount = new(BaseContent.Styles.Button.Minus24) { VerticalAlignment = VerticalAlignment.Center };
        decreaseAmount.Click += (_, _) => {
            _amountWanted--;
            textBox.Text = _amountWanted.ToString();
        };

        HorizontalStackPanel hPanel = new() {
            Spacing = 5,
            DefaultProportion = Proportion.Auto,
            Margin = new Thickness(0, 10, 0, 10)
        };
        hPanel.AddChild(new Label { Text = "Amount Wanted: ", TextColor = BaseContent.Colors.Text.Golden, VerticalAlignment = VerticalAlignment.Center });
        hPanel.AddChild(textBox);
        hPanel.AddChild(increaseAmount);
        hPanel.AddChild(decreaseAmount);
        return hPanel;
    }

    public void Update() {
        _cookButton.Enabled = _house.HasRequirementsFor(_food, _amountWanted);
        foreach ((ResourceCount requirement, Label label) in _ingredients) {
            label.Text = $"{requirement.Count}x {requirement.Resource!.Label} ({_house.AmountOfItem(requirement.Resource)}/{_amountWanted * requirement.Count})";
        }
    }
}

public class RecipePicker : ComboBox {
    public RecipePicker(List<ItemDef> defs, EventHandler changeAction) {
        ListItem unselectedItem = new() { Text = "Pick a recipe" };
        base.Items.Add(unselectedItem);
        base.SelectedItem = unselectedItem;
        foreach (ItemDef def in defs) {
            ListItem comboItem = new() { Text = def.Label, Tag = def };
            base.Items.Add(comboItem);
        }

        base.SelectedIndexChanged += changeAction;
    }
}