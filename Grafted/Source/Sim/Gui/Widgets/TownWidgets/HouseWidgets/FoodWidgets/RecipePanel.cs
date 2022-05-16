using System.Collections.Generic;
using System.Text.RegularExpressions;
using Grafted.Sim.Entities.Items;
using Myra.Graphics2D;
using Myra.Graphics2D.TextureAtlases;
using Myra.Graphics2D.UI;

namespace Grafted.Sim.Gui.Widgets.TownWidgets.HouseWidgets.FoodWidgets;

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
        AddChild(new Label("small") { Text = $"Time to make: \\c[{UiTextColor.TextColorTime}]{food.CraftingProperties.MinutesToMake} minutes" });
        AddChild(new Label("small") { Text = $"Yield: \\c[{UiTextColor.TextColorGreen}]{food.CraftingProperties.AmountProduced}x" });
        if (food.CraftingProperties.RequiredTools != null) {
            AddChild(new Label("small") { Text = "Required Tools: ", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 10, 0, 0) });
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

        AddChild(new Label("small") { Text = "Ingredients:", Margin = new Thickness(0, 10, 0, 0) });
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
        _cookButton.Enabled = _amountWanted > 0 && _house.HasRequirementsFor(_food, _amountWanted);
        foreach ((ResourceCount requirement, Label label) in _ingredients) {
            label.Text = $"{requirement.Count}x {requirement.Resource!.Label} ({_house.AmountOfItem(requirement.Resource)}/{_amountWanted * requirement.Count})";
        }
    }
}