namespace Wendlewind.Scenes.MainGameScene.Gui.Widgets.CombatWidgets;

public sealed class PotionBar : HorizontalStackPanel, IUpdatable
{
    private readonly List<PotionSlot> _potions = [];

    public PotionBar(Pawn pawn, Action<Item>? clickHandler = null)
    {
        foreach (var potion in pawn.Equipment.Potions)
        {
            var tint = new ColoredRegion(new TextureRegion(potion.GetIcon()), Color.White);
            var icon = new Image
            {
                Background = tint,
                Width = BaseContent.IconSizes.Medium,
                Height = BaseContent.IconSizes.Medium
            };
            var button = new CursorButton
            {
                Content = icon
            };
            button.TouchDown += (_, _) => clickHandler?.Invoke(potion);
            var trigger = potion.PotionTrigger?.Describe() ?? "No trigger";
            button.WithTooltip(potion.Label, trigger);
            Widgets.Add(button);
            _potions.Add(new PotionSlot(potion, button, tint));
        }
    }

    public void NotifyUsed(string? itemMoniker, int itemId = -1)
    {
        foreach (var slot in _potions)
        {
            if (slot.Consumed)
            {
                continue;
            }

            var matches = (itemMoniker != null && slot.Potion.ItemDef.Moniker == itemMoniker)
                          || (itemId >= 0 && slot.Potion.Id == itemId);
            if (!matches)
            {
                continue;
            }

            slot.Consumed = true;
            slot.FlashTime = 0.55f;
            return;
        }
    }

    public void Update()
    {
        Update(1f / 60f);
    }

    public void Update(float deltaTime)
    {
        for (var i = _potions.Count - 1; i >= 0; i--)
        {
            var slot = _potions[i];
            if (slot.Consumed || slot.Potion.IsDestroyed)
            {
                slot.FlashTime -= deltaTime;
                var t = Math.Clamp(slot.FlashTime / 0.55f, 0f, 1f);
                slot.Button.Opacity = 0.35f + t * 0.65f;
                slot.Tint.Color = Color.Lerp(Color.Gray, Color.Gold, t);

                if (slot.FlashTime <= 0)
                {
                    slot.Button.RemoveFromParent();
                    _potions.RemoveAt(i);
                }
            }
        }
    }

    private sealed class PotionSlot(Item potion, Widget button, ColoredRegion tint)
    {
        public readonly Item Potion = potion;
        public readonly Widget Button = button;
        public readonly ColoredRegion Tint = tint;
        public bool Consumed;
        public float FlashTime;
    }
}
