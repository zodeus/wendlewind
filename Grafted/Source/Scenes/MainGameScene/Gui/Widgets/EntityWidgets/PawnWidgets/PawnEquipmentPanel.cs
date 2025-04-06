using Grafted.Sim.Entities;

namespace Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets;

public class PawnEquipmentPanel : HorizontalStackPanel, IUpdatable
{
    private readonly BaseGui _gui;
    private readonly Pawn _pawn;
    private readonly Dictionary<BodyPart, EquipmentColumn> _panels = new();
    private static readonly Color DestroyedEquipmentColor = new(255, 0, 0, 15);

    public PawnEquipmentPanel(BaseGui gui, Pawn pawn, Action<BodyPart, EquipmentSlotType>? clickAction = null)
    {
        _gui = gui;
        _pawn = pawn;
        Spacing = 2;
        foreach (var (bodyPart, slots) in pawn.Equipment.Slots)
        {
            if (slots.Count ==0)
            {
                continue;
            }
            if (bodyPart.Type is BodyPartType.Finger or BodyPartType.Thumb or BodyPartType.Eye)
            {
                continue;
            }

            EquipmentColumn partPanel = new(gui, bodyPart, slots, (part, type) =>
            {
                if (clickAction != null)
                {
                    clickAction.Invoke(part, type);
                    return;
                }

                HandleClick(part, type);
            });
            Widgets.Add(partPanel);
            _panels.Add(bodyPart, partPanel);
        }
    }

    private void HandleClick(BodyPart part, EquipmentSlotType slot)
    {
        // UnEquip
        if (_gui.MouseAttachment == null && Input.RightMouseButtonReleased && slot != EquipmentSlotType.BuiltIn)
        {
            var unEquippedItem = _pawn.Equipment.UnEquip(part, slot);
            if (unEquippedItem != null)
            {
                if (_pawn.Inventory.Entities.TryAdd(unEquippedItem) == false)
                {
                    //return item, failed to place in inventory
                    _pawn.Equipment.TryEquip(part, slot, unEquippedItem);
                }
            }

            return;
        }

        if (_gui.MouseAttachment?.Data is Item item)
        {
            if (item.Def == Defs.Items.EnchantmentExpander)
            {
                if (_pawn.Equipment.GetBySlot(part, slot) is { Enchantments: not null } equipmentItem)
                {
                    equipmentItem.Enchantments.MaxEnchantments++;
                    item.StackSize--;
                    _gui.WorldTextHandler.Add(new WorldSpaceText
                    {
                        Font = BaseContent.Fonts.Default.Normal,
                        Color = Color.Azure,
                        Text = $"Socket added to {equipmentItem.Label}",
                        DurationInTicks = 120,
                        Speed = -2,
                        Position = Mouse.GetState().Position.ToVector2()
                    });

                    if (item.StackSize != 0) return;

                    item.Destroy();
                    _gui.MouseAttachment.Detach();
                }

                return;
            }

            if (item.Def == Defs.Items.RepairKit)
            {
                if (_pawn.Equipment.GetBySlot(part, slot) is { } equipmentItem && equipmentItem.Durability < equipmentItem.MaxDurability)
                {
                    equipmentItem.Repair();
                    item.StackSize--;
                    if (item.StackSize == 0)
                    {
                        item.Destroy();
                        _gui.MouseAttachment.Detach();
                    }
                }

                return;
            }

            // Try Equip
            if (item.ItemDef.EquipmentProperties?.SlotUsedToEquip == slot || (item.ItemDef.ItemType == ItemType.Potion && slot is EquipmentSlotType.PotionSlot1 or EquipmentSlotType.PotionSlot2))
            {
                // Item that can be potentially unEquipped, if this is set it will re-add this item to the pawns inventory
                Item? unEquippedItem;
                if (item.ItemDef.ItemType == ItemType.Potion)
                {
                    //todo implement splitting
                    Item potion;
                    if (item.StackSize > 1)
                    {
                        item.StackSize--;
                        potion = EntityGenerator.CreateEntity<Item>(item.ItemDef, 1);
                    }
                    else
                    {
                        potion = item;
                        item.EjectFromContainer();
                    }

                    unEquippedItem = _pawn.Equipment.TryEquip(part, slot, potion);
                }
                else
                {
                    item.EjectFromContainer();
                    unEquippedItem = _pawn.Equipment.TryEquip(part, slot, item);
                }

                _gui.MouseAttachment.Detach();
                if (unEquippedItem != null)
                {
                    _pawn.Inventory.TryAdd(unEquippedItem);
                }
            }

            return;
        }

        if (part.Equipment[slot] != null)
        {
            _gui.ViewEntity(part.Equipment[slot]!);
        }
    }

    public void Update()
    {
        foreach ((var bodyPart, var widget) in _panels)
        {
            widget.Update();
            if (bodyPart.IsSevered)
            {
                _panels.Remove(bodyPart);
                widget.RemoveFromParent();
            }
        }
    }

    private class EquipmentColumn : VerticalStackPanel
    {
        private readonly int _cellSize = BaseContent.IconSizes.Large;
        private readonly BodyPart _bodyPart;
        private readonly Dictionary<EquipmentSlotType, Button> _slots = new();
        private readonly Image _imageFrame;
        private event Action<BodyPart, EquipmentSlotType>? ClickAction;
        private Dictionary<ItemDef, ColoredRegion> _iconCache = new();
        private IImage _potionSlotIcon;
        private IImage _bagSlotIcon;

        public EquipmentColumn(BaseGui gui, BodyPart bodyPart, List<EquipmentSlotType> slots, Action<BodyPart, EquipmentSlotType>? clickAction = null)
        {
            _bodyPart = bodyPart;
            ClickAction = clickAction;
            Spacing = 2;
            _potionSlotIcon = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Icon.PotionSlot];
            _bagSlotIcon = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Icon.BagSlot];
            _imageFrame = new Image { Background = new ColoredRegion(new TextureRegion(bodyPart.WhiteIcon), BodyPartColor.Get(bodyPart)), Width = _cellSize, Height = _cellSize };
            _imageFrame.TouchDown += (_, _) => gui.ViewEntity(bodyPart);
            Widgets.Add(_imageFrame);
            foreach (var slot in slots)
            {
                Button slotFrame = new(BaseContent.Styles.Button.Icon)
                {
                    Content = new HorizontalStackPanel
                    {
                        Widgets =
                        {
                            new HorizontalProgressBar(BaseContent.Styles.Bar.Durability)
                            {
                                Width = _cellSize - 4, Height = 12, HorizontalAlignment = HorizontalAlignment.Center,
                                VerticalAlignment = VerticalAlignment.Bottom
                            }
                        }
                    },
                    Width = _cellSize, Height = _cellSize
                };
                _slots.Add(slot, slotFrame);
                slotFrame.Click += (_, _) => ClickAction?.Invoke(bodyPart, slot);
                Widgets.Add(slotFrame);
            }
        }

        public void Update()
        {
            foreach ((var slot, var image) in _slots)
            {
                if (_bodyPart.Equipment[slot] is { IsDestroyed: false } item)
                {
                    if (_iconCache.ContainsKey(item.ItemDef) == false)
                    {
                        _iconCache[item.ItemDef] = new ColoredRegion(new TextureRegion(item.Icon), Color.White);
                    }

                    ((HorizontalProgressBar)((HorizontalStackPanel)image.Content).Widgets[0]).Visible = item.Durability > 1;
                    image.Content.Background = _iconCache[item.ItemDef];
                    ((HorizontalProgressBar)((HorizontalStackPanel)image.Content).Widgets[0]).Value = item.Durability / item.MaxDurability * 100;
                    ((ColoredRegion)image.Content.Background).Color = GetEquipmentColor(item, _bodyPart);
                }
                else
                {
                    if (slot is EquipmentSlotType.PotionSlot1 or EquipmentSlotType.PotionSlot2)
                    {
                        image.Content.Background = _potionSlotIcon;
                    }
                    else if (slot is EquipmentSlotType.Bag)
                    {
                        image.Content.Background = _bagSlotIcon;
                    }
                    else
                    {
                        image.Content.Background = null;
                    }

                    ((HorizontalProgressBar)((HorizontalStackPanel)image.Content).Widgets[0]).Visible = false;
                }
            }

            ((ColoredRegion)_imageFrame.Background).Color = BodyPartColor.Get(_bodyPart);
        }
    }

    private static Color GetEquipmentColor(Item item, BodyPart bodyPart)
    {
        if (item.IsDestroyed)
        {
            return DestroyedEquipmentColor;
        }

        if (item.ItemDef.EquipmentProperties.EquipmentType == EquipmentType.Weapon && bodyPart.HasMobility == false)
        {
            return Color.Red;
        }

        return Color.White;
    }
}