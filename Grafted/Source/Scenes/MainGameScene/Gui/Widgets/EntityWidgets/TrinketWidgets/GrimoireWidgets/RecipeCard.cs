using Grafted.Sim.Entities;

namespace Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.TrinketWidgets.GrimoireWidgets;

public class RecipeCard : VerticalStackPanel,
    IUpdatable
{
    private readonly string _buttonLabel;

    public RecipeCard(string buttonLabel)
    {
        _buttonLabel = buttonLabel;
        Padding = new Thickness(20);
        Width = 500;
        Height = 800;
        Spacing = 10;
    }

    public void SetItem(Pawn pawn, ItemDef itemDef)
    {
        ClearCard();

        var detailsPanel = GenerateDetailsPanel(itemDef);
        var ingredientsPanel = GenerateIngredientsPanel(pawn, itemDef);
        var trinketsPanel = GenerateTrinketsPanel(pawn, itemDef);
        var button = new Button(BaseContent.Styles.Button.Large)
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Enabled = itemDef.CraftingProperties?.CanCraft(pawn) == true,
            Content = new Label(BaseContent.Styles.Label.Medium)
            {
                TextColor = itemDef.CraftingProperties?.CanCraft(pawn) == true ? Color.DarkGoldenrod : new Color(80, 80, 80),
                Text = _buttonLabel
            }
        };
        button.Click += (_, _) =>
        {
            CraftItem(pawn, itemDef);
            SetItem(pawn, itemDef);
        };
        SetProportionType(detailsPanel, ProportionType.Auto);
        SetProportionType(ingredientsPanel, ProportionType.Auto);
        SetProportionType(trinketsPanel, ProportionType.Fill);
        SetProportionType(button, ProportionType.Auto);

        Widgets.Add(new HorizontalStackPanel
        {
            Spacing = 10,
            Widgets =
            {
                new HorizontalStackPanel()
                {
                    Spacing = 10,
                    Widgets =
                    {
                        new Image { Width = 64, Height = 64, Background = new TextureRegion(itemDef.Texture) },
                        new Label(BaseContent.Styles.Label.Medium) { Text = itemDef.Label, Wrap = true, Height = 80 },
                    }
                }
            }
        });
        Widgets.Add(detailsPanel);
        Widgets.Add(ingredientsPanel);
        Widgets.Add(trinketsPanel);
        Widgets.Add(button);
    }

    private Widget GenerateDetailsPanel(ItemDef itemDef)
    {
        return new VerticalStackPanel
        {
            Widgets = { new Label() { Text = $"Amount Produced: /c[{TC.Golden}]{itemDef.CraftingProperties?.AmountProduced}" } }
        };
    }

    private bool CraftItem(Pawn pawn, ItemDef itemToCraft)
    {
        List<Item> resourcesTaken = [];
        foreach (var resource in itemToCraft.CraftingProperties!.ResourceRequirements)
        {
            var resourceToUse = pawn.Inventory.Entities.Take(resource);

            if (resourceToUse == null)
            {
                foreach (var resourceTaken in resourcesTaken)
                {
                    pawn.Inventory.TryAdd(resourceTaken);
                }

                return false;
            }

            resourcesTaken.Add(resourceToUse);
            if (resourceToUse.StackSize < resource.Count)
            {
                return false;
            }
        }

        foreach (var resourceTaken in resourcesTaken)
        {
            resourceTaken.Destroy();
        }

        pawn.Inventory.TryAdd(EntityGenerator.CreateEntity<Item>(itemToCraft, itemToCraft.CraftingProperties.AmountProduced));

        return true;
    }

    private static VerticalStackPanel GenerateIngredientsPanel(Pawn pawn, ItemDef itemDef)
    {
        var panel = new VerticalStackPanel { Spacing = 10, Widgets = { new Label { Text = "Ingredients", Margin = new Thickness(0, 20, 0, 0) } } };
        foreach (var itemCount in itemDef.CraftingProperties!.ResourceRequirements)
        {
            var amountColor = pawn.Inventory.AmountOf(itemCount.Item) >= itemCount.Count ? $"/c[{TC.Green}]" : "";
            var amountLabel = $"{itemCount.Item.Label} {amountColor}{pawn.Inventory.AmountOf(itemCount.Item)}/{itemCount.Count}";
            panel.Widgets.Add(new HorizontalStackPanel
            {
                Spacing = 5, Widgets =
                {
                    new Image { Background = new TextureRegion(itemCount.Item.Texture), Width = 32, Height = 32 },
                    new Label { Text = amountLabel, VerticalAlignment = VerticalAlignment.Stretch }
                }
            });
        }

        panel.Visible = itemDef.CraftingProperties?.ResourceRequirements?.Count > 0;
        return panel;
    }

    private static VerticalStackPanel GenerateTrinketsPanel(Pawn pawn, ItemDef itemDef)
    {
        var panel = new VerticalStackPanel { Spacing = 10, Widgets = { new Label { Text = "Trinkets", Margin = new Thickness(0, 20, 0, 0) } } };
        foreach (var item in itemDef.CraftingProperties?.RequiredTrinkets ?? [])
        {
            var hasTrinketIcon = pawn.Inventory.Trinkets.Any(t => t.Def == item)
                ? new Image
                {
                    Width = 32, Height = 32, Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Icon.ArrowPositive]
                }
                : new Widget();
            var hasColor = pawn.Inventory.Trinkets.Any(t => t.Def == item)? $"/c[{TC.Green}]" : "";
            panel.Widgets.Add(new HorizontalStackPanel
            {
                Spacing = 5, Widgets =
                {
                    new Image { Background = new TextureRegion(item.Texture), Width = 32, Height = 32 },
                    new Label { Text = $"{hasColor}{item.Label}", VerticalAlignment = VerticalAlignment.Stretch },
                    //hasTrinketIcon
                }
            });
        }

        panel.Visible = itemDef.CraftingProperties?.RequiredTrinkets?.Count > 0;

        return panel;
    }

    private void ClearCard()
    {
        for (var index = Widgets.Count - 1; index >= 0; index--)
        {
            Widgets.RemoveAt(index);
        }
    }

    public void Update()
    {
    }
}