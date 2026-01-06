namespace Grafted.Scenes.MainGameScene.Gui.Widgets.CombatWidgets;

public sealed class PotionBar : HorizontalStackPanel, IUpdatable
{
    private List<KeyValuePair<Item, Widget>> _potions = new();

    public PotionBar(Pawn pawn, Action<Item>? clickHandler = null)
    {
        foreach (var potion in pawn.Equipment.Potions)
        {
            var button = new CursorButton
            {
                Content = new Image
                {
                    Background = new TextureRegion(potion.Icon),
                    Width = BaseContent.IconSizes.Medium, Height = BaseContent.IconSizes.Medium
                }
            };
            button.TouchDown += (_, _) => clickHandler?.Invoke(potion);
            _potions.Add(new(potion, button));
            Widgets.Add(button);
        }
    }

    public void Update()
    {
        for (var i = _potions.Count - 1; i >= 0; i--)
        {
            if (!_potions[i].Key.IsDestroyed) continue;
            _potions[i].Value.RemoveFromParent();

            _potions.RemoveAt(i);
        }
    }
}