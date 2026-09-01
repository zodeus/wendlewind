﻿using SolidBrush = Myra.Graphics2D.Brushes.SolidBrush;

namespace Wendlemire.Scenes.MainGameScene.Gui.Widgets.Grimoire;

public sealed class CraftingPanel : VerticalStackPanel, IUpdatable
{
    private readonly Pawn _pawn;
    private readonly RecipeCard _recipeCard;
    private readonly List<ItemDef> _items;
    private readonly Dictionary<ItemDef, CursorButton> _itemButtons = new();

    /// <summary>
    /// Returns true if any recipe in this panel can be crafted with current inventory
    /// </summary>
    public bool HasCraftableRecipe => _items.Any(item => item.CraftingProperties?.CanCraft(_pawn) == true);

    public CraftingPanel(string buttonLabel, List<ItemDef> items, Pawn pawn)
    {
        _pawn = pawn;
        _items = items;
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

    private CursorButton CreateItemButton(ItemDef item, RecipeCard recipeCard)
    {
        var canCraft = item.CraftingProperties?.CanCraft(_pawn) == true;

        // Outer frame panel
        var framePanel = new CursorButton(BaseContent.Styles.Button.Gold)
        {
            Width = 72,
        };

        // Inner content panel with item icon
        var iconPanel = new Panel
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
        };

        var itemImage = new Image
        {
            Background = item.GetIconImage(),
            Width = 48,
            Height = 48,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Opacity = canCraft ? 1.0f : 0.4f
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
                Margin = new Thickness(0, 0, 0, 0)
            };
            iconPanel.Widgets.Add(indicator);
        }

        framePanel.Content = iconPanel;


        framePanel.TouchDown += (_, _) =>
        {
            recipeCard.SetItem(_pawn, item);
        };

        // Add tooltip with item info
        framePanel.WithTooltip(() => CreateItemTooltip(item));

        return framePanel;
    }

    private Widget CreateItemTooltip(ItemDef item)
    {
        var container = new VerticalStackPanel { Spacing = 6, MaxWidth = 280 };

        // Item name
        container.Widgets.Add(new Label(BaseContent.Styles.Label.Normal)
        {
            Text = item.Label,
            TextColor = Color.Gold
        });

        return container;
    }

    public override void InternalRender(RenderContext context)
    {
        base.InternalRender(context);
        TooltipHelper.UpdatePosition();
    }

    public void Update()
    {
        // Refresh button states based on current inventory
        foreach (var item in _items)
        {
            if (!_itemButtons.TryGetValue(item, out var framePanel)) continue;
            var canCraft = item.CraftingProperties?.CanCraft(_pawn) == true;
            framePanel.Enabled = canCraft;
            // Find and update the icon panel (first widget in frame)
            if (framePanel.Content is Panel iconPanel)
            {
                // Update item image opacity
                var itemImage = iconPanel.Widgets.OfType<Image>().FirstOrDefault();
                if (itemImage != null)
                {
                    itemImage.Opacity = canCraft ? 1.0f : 0.4f;
                }

                // Update or add/remove the craftable indicator
                var existingIndicator = iconPanel.Widgets.OfType<Panel>()
                    .FirstOrDefault(p => p.Width == 10 && p.Height == 10);

                if (canCraft && existingIndicator == null)
                {
                    // Add indicator
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
                else if (!canCraft && existingIndicator != null)
                {
                    // Remove indicator
                    iconPanel.Widgets.Remove(existingIndicator);
                }
            }
        }

        // Also refresh the recipe card if it's showing an item
        _recipeCard.Update();
    }
}
