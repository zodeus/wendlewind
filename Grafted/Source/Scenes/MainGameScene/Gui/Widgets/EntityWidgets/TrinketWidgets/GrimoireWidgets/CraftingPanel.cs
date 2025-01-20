namespace Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.TrinketWidgets.GrimoireWidgets;

public sealed class CraftingPanel : HorizontalStackPanel
{
    private readonly Pawn _pawn;

    public CraftingPanel(string buttonLabel, List<ItemDef> items, Pawn pawn)
    {
        _pawn = pawn;
        var recipeCard = new RecipeCard(buttonLabel);
        Widgets.Add(GenerateItemList(items, recipeCard));
        Widgets.Add(new VerticalSeparator());
        Widgets.Add(recipeCard);
    }

    private Widget GenerateItemList(List<ItemDef> items, RecipeCard recipeCard)
    {
        var vPanel = new VerticalStackPanel() { Spacing = 5, Width = 210};
        var hPanel = new HorizontalStackPanel() { Spacing = 5 };
        vPanel.Widgets.Add(hPanel);
        var itemCount = 0;
        foreach (var item in items)
        {
            var button = new Button() { Content = new Image { Background = new TextureRegion(item.Icon), Width = 96, Height = 96 } };
            button.Click += (_, _) => recipeCard.SetItem(_pawn, item);
            button.MouseEntered += (_, _) => Mouse.SetCursor(Microsoft.Xna.Framework.Input.MouseCursor.Hand);
            button.MouseLeft += (_, _) => Mouse.SetCursor(Microsoft.Xna.Framework.Input.MouseCursor.Arrow);
            hPanel.Widgets.Add(button);
            itemCount++;

            if (itemCount < 2) continue;
            itemCount = 0;
            hPanel = new HorizontalStackPanel() { Spacing = 5 };
            vPanel.Widgets.Add(hPanel);
        }

        return vPanel;
    }
}