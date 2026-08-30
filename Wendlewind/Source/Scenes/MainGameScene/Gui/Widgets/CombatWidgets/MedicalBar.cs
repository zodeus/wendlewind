namespace Wendlewind.Scenes.MainGameScene.Gui.Widgets.CombatWidgets;

public sealed class MedicalBar : HorizontalStackPanel, IUpdatable
{
    private readonly List<MedicalSlot> _slots = [];

    public MedicalBar(Pawn pawn, Action<Item>? clickHandler = null, int? iconSize = null)
    {
        pawn.MedicalChest.Prune();
        var size = iconSize ?? BaseContent.IconSizes.Medium;
        foreach (var chestSlot in pawn.MedicalChest.Slots)
        {
            var item = chestSlot.Item;
            var tint = new ColoredIcon(item.GetIconImage(), Color.White);
            var icon = new Image
            {
                Background = tint,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            var button = new CursorButton
            {
                Width = size,
                Height = size,
                Padding = new Thickness(0),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Content = icon
            };
            button.TouchDown += (_, _) => clickHandler?.Invoke(item);
            button.WithTooltip(item.Label, chestSlot.Trigger.Describe());
            Widgets.Add(button);
            _slots.Add(new MedicalSlot(item, button, tint));
        }
    }

    public void NotifyUsed(string? itemMoniker, int itemId = -1)
    {
        foreach (var slot in _slots)
        {
            if (slot.Consumed)
            {
                continue;
            }

            var matches = (itemMoniker != null && slot.Item.ItemDef.Moniker == itemMoniker)
                          || (itemId >= 0 && slot.Item.Id == itemId);
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
        for (var i = _slots.Count - 1; i >= 0; i--)
        {
            var slot = _slots[i];
            if (slot.Consumed || slot.Item.IsDestroyed)
            {
                slot.FlashTime -= deltaTime;
                var t = Math.Clamp(slot.FlashTime / 0.55f, 0f, 1f);
                slot.Button.Opacity = 0.35f + t * 0.65f;
                slot.Tint.Color = Color.Lerp(Color.Gray, Color.Gold, t);

                if (slot.FlashTime <= 0)
                {
                    slot.Button.RemoveFromParent();
                    _slots.RemoveAt(i);
                }
            }
        }
    }

    private sealed class MedicalSlot(Item item, Widget button, ColoredIcon tint)
    {
        public readonly Item Item = item;
        public readonly Widget Button = button;
        public readonly ColoredIcon Tint = tint;
        public bool Consumed;
        public float FlashTime;
    }
}
