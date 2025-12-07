using SolidBrush = Myra.Graphics2D.Brushes.SolidBrush;

namespace Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.TrinketWidgets.GrimoireWidgets;

public sealed class CraftingPanel : VerticalStackPanel
{
    private readonly Pawn _pawn;
    private readonly RecipeCard _recipeCard;
    private readonly Dictionary<ItemDef, Panel> _itemButtons = new();

    public CraftingPanel(string buttonLabel, List<ItemDef> items, Pawn pawn)
    {
        _pawn = pawn;
        _recipeCard = new RecipeCard(buttonLabel);
        Spacing = 0;
        
        // Horizontal item strip at top
        var itemStripContainer = new Panel
        {
            Background = new SolidBrush(new Color(15, 12, 10)),
            Padding = new Thickness(8),
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        itemStripContainer.Widgets.Add(GenerateItemStrip(items, _recipeCard));
        
        // Divider between items and recipe
        var divider = new Panel
        {
            Height = 2,
            Background = new SolidBrush(new Color(60, 50, 40)),
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        
        Widgets.Add(itemStripContainer);
        Widgets.Add(divider);
        Widgets.Add(_recipeCard);
    }

    private Widget GenerateItemStrip(List<ItemDef> items, RecipeCard recipeCard)
    {
        var scrollViewer = new ScrollViewer
        {
            MaxHeight = 90,
        };
        
        // Horizontal row of items
        var itemRow = new HorizontalStackPanel { Spacing = 6 };
        scrollViewer.Content = itemRow;
        
        foreach (var item in items)
        {
            var itemButton = CreateItemButton(item, recipeCard);
            _itemButtons[item] = itemButton;
            itemRow.Widgets.Add(itemButton);
        }

        return scrollViewer;
    }

    private Panel CreateItemButton(ItemDef item, RecipeCard recipeCard)
    {
        var canCraft = item.CraftingProperties?.CanCraft(_pawn) == true;
        
        // Outer frame panel
        var framePanel = new Panel
        {
            Width = 72,
            Height = 72,
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.RoundDark64],
            Padding = new Thickness(4),
        };
        
        // Inner content panel with item icon
        var iconPanel = new Panel
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
        };
        
        var itemImage = new Image
        {
            Background = new TextureRegion(item.Icon),
            Width = 64,
            Height = 64,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Opacity = canCraft ? 1.0f : 0.5f
        };
        iconPanel.Widgets.Add(itemImage);
        
        // Craftable indicator (small green dot in corner)
        if (canCraft)
        {
            var indicator = new Panel
            {
                Width = 10,
                Height = 10,
                Background = new SolidBrush(new Color(60, 200, 60)),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 2, 2, 0)
            };
            iconPanel.Widgets.Add(indicator);
        }
        
        framePanel.Widgets.Add(iconPanel);
        
        // Hover and click handling
        framePanel.MouseEntered += (_, _) =>
        {
            Mouse.SetCursor(Microsoft.Xna.Framework.Input.MouseCursor.Hand);
            framePanel.Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrameBright];
        };
        
        framePanel.MouseLeft += (_, _) =>
        {
            Mouse.SetCursor(Microsoft.Xna.Framework.Input.MouseCursor.Arrow);
            framePanel.Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.RoundDark64];
        };
        
        framePanel.TouchDown += (_, _) =>
        {
            // Clear previous selection styling
            foreach (var btn in _itemButtons.Values)
            {
                btn.Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.RoundDark64];
            }
            
            // Apply selected styling
            framePanel.Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.DeepGold];
            recipeCard.SetItem(_pawn, item);
        };
        
        return framePanel;
    }
}
