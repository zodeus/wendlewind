namespace Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.TrinketWidgets;

/// <summary>
/// Panel for the Cloakenator trinket - displays info about combining capes into cloaks.
/// </summary>
[UsedImplicitly]
public sealed class CloakenatorPanel : EntityPanelBase
{
    public CloakenatorPanel(BaseGui gui, Item item, EntityPanelProperties? properties = null) : base(gui, item, properties)
    {
        Padding = new Thickness(20);
        MinWidth = 360;
        Spacing = 12;

        // Header with icon and description
        var headerSection = new HorizontalStackPanel
        {
            Spacing = 15,
            Margin = new Thickness(0, 0, 0, 8)
        };

        // Icon with frame
        var iconFrame = new Panel
        {
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.DeepGold],
            Padding = new Thickness(4),
            Width = 80,
            Height = 80
        };
        iconFrame.Widgets.Add(new Image
        {
            Background = new TextureRegion(item.Icon),
            Width = 72,
            Height = 72,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        });
        headerSection.Widgets.Add(iconFrame);

        // Description area
        var descArea = new VerticalStackPanel { Spacing = 6, VerticalAlignment = VerticalAlignment.Center };
        descArea.Widgets.Add(new Label(BaseContent.Styles.Label.Normal)
        {
            Text = item.Def.Description,
            Wrap = true,
            MaxWidth = 280
        });
        headerSection.Widgets.Add(descArea);

        Widgets.Add(headerSection);

        // Info section about available recipes
        var infoSection = new VerticalStackPanel { Spacing = 8 };
        
        infoSection.Widgets.Add(new Label(BaseContent.Styles.Label.Medium)
        {
            Text = "Available Cloak Recipes",
            TextColor = BaseContent.Colors.Text.Golden,
            Margin = new Thickness(0, 8, 0, 4)
        });

        // List available cloak recipes
        var cloakRecipes = DefRepository<ItemDef>.Defs
            .Where(d => d.EquipmentProperties?.SlotUsedToEquip == EquipmentSlotType.Cloak && d.CraftingProperties != null)
            .OrderBy(d => d.Label)
            .ToList();

        if (cloakRecipes.Count > 0)
        {
            foreach (var recipe in cloakRecipes)
            {
                infoSection.Widgets.Add(CreateRecipeRow(recipe));
            }
            
            infoSection.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
            {
                Text = "Open the Grimoire to craft cloaks!",
                TextColor = new Color(150, 150, 150),
                Margin = new Thickness(0, 8, 0, 0)
            });
        }
        else
        {
            infoSection.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
            {
                Text = "No cloak recipes available.",
                TextColor = new Color(120, 120, 120)
            });
        }

        Widgets.Add(infoSection);
    }

    private Widget CreateRecipeRow(ItemDef recipe)
    {
        var row = new HorizontalStackPanel { Spacing = 8 };

        // Recipe icon
        row.Widgets.Add(new Image
        {
            Background = new TextureRegion(recipe.Icon),
            Width = 32,
            Height = 32,
            VerticalAlignment = VerticalAlignment.Center
        });

        // Recipe name and ingredients
        var textStack = new VerticalStackPanel { Spacing = 2, VerticalAlignment = VerticalAlignment.Center };
        
        textStack.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
        {
            Text = recipe.Label,
            TextColor = BaseContent.Colors.Text.Golden
        });

        // Show required ingredients
        var ingredients = recipe.CraftingProperties?.ResourceRequirements ?? [];
        if (ingredients.Count > 0)
        {
            var ingredientText = string.Join(" + ", ingredients.Select(i => i.Item.Label));
            textStack.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
            {
                Text = ingredientText,
                TextColor = new Color(140, 140, 140)
            });
        }

        row.Widgets.Add(textStack);

        return row;
    }

    public override void Update()
    {
    }
}
