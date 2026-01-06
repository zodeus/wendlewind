using Myra.Graphics2D.Brushes;

namespace Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets;

public class PawnEquipmentPanel : HorizontalStackPanel, IUpdatable
{
    private readonly BaseGui _gui;
    private readonly Pawn _pawn;
    private readonly Dictionary<BodyPart, EquipmentColumn> _panels = new();
    private static readonly Color DestroyedEquipmentColor = new(255, 0, 0, 15);
    private Window? _equipmentPopup;
    private const int PopupCloseDistance = 10;

    public PawnEquipmentPanel(BaseGui gui, Pawn pawn, Action<BodyPart, EquipmentSlotType>? clickAction = null)
    {
        _gui = gui;
        _pawn = pawn;
        Spacing = 2;
        foreach (var (bodyPart, slots) in pawn.Equipment.Slots)
        {
            if (slots.Count == 0)
            {
                continue;
            }
            if (bodyPart.Type is BodyPartType.Finger or BodyPartType.Thumb or BodyPartType.Eye)
            {
                continue;
            }

            EquipmentColumn partPanel = new(gui, pawn, bodyPart, slots, (part, type) =>
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
        if (_gui.MouseAttachment == null && Mouse.GetState().RightButton == ButtonState.Pressed && slot != EquipmentSlotType.BuiltIn)
        {
            var unEquippedItem = _pawn.Equipment.UnEquip(part, slot);
            if (unEquippedItem != null)
            {
                if (_pawn.Inventory.TryAdd(unEquippedItem) == false)
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

        // View equipped item if slot has one
        if (part.Equipment[slot] != null)
        {
            _gui.ViewEntity(part.Equipment[slot]!);
            return;
        }

        // Show equipment selection popup for empty slots
        if (slot != EquipmentSlotType.BuiltIn)
        {
            ShowEquipmentSelectionPopup(part, slot);
        }
    }

    private void ShowEquipmentSelectionPopup(BodyPart part, EquipmentSlotType slot)
    {
        // Don't open another popup if one is already open
        if (_equipmentPopup?.IsPlaced == true)
        {
            return;
        }

        // Find available items in inventory that can be equipped to this slot
        var availableItems = _pawn.Inventory
            .Where(i => i.ItemDef.EquipmentProperties?.SlotUsedToEquip == slot ||
                       (i.ItemDef.ItemType == ItemType.Potion && slot is EquipmentSlotType.PotionSlot1 or EquipmentSlotType.PotionSlot2))
            .ToList();

        if (availableItems.Count == 0)
        {
            return;
        }

        _equipmentPopup = new Window
        {
            Title = null,
            Background = null,
            Padding = new Thickness(0)
        };
        _equipmentPopup.TitlePanel.Visible = false;

        var itemList = new VerticalStackPanel { Spacing = 4 };
        var scrollViewer = new ScrollViewer
        {
            Content = itemList,
            MaxHeight = 300,
        };

        foreach (var availableItem in availableItems)
        {
            var itemButton = new Button(BaseContent.Styles.Button.Dark)
            {
                Content = new Image
            {
                Background = new TextureRegion(availableItem.Icon),
                Width = BaseContent.IconSizes.Default,
                Height = BaseContent.IconSizes.Default
            }
            };

            var capturedItem = availableItem;
            itemButton.Click += (_, _) =>
            {
                EquipItemFromInventory(part, slot, capturedItem);
                CloseEquipmentPopup();
            };

            itemList.Widgets.Add(itemButton);
        }

        _equipmentPopup.Content = scrollViewer;
        // get scaled position of mouse
        var mousePos = Mouse.GetState().Position;
        var scaledPos = new Point((int)(mousePos.X / Core.UiScale), (int)(mousePos.Y / Core.UiScale));
        _equipmentPopup.Show(_gui.Desktop, scaledPos);
    }

    private void CloseEquipmentPopup()
    {
        _equipmentPopup?.Close();
        _equipmentPopup = null;
    }

    private void EquipItemFromInventory(BodyPart part, EquipmentSlotType slot, Item item)
    {
        Item? unEquippedItem;

        if (item.ItemDef.ItemType == ItemType.Potion)
        {
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

        if (unEquippedItem != null)
        {
            _pawn.Inventory.TryAdd(unEquippedItem);
        }
    }

    public void Update()
    {
        // Check if mouse is too far from equipment popup and close it
        if (_equipmentPopup?.IsPlaced == true)
        {
            var mousePos = Mouse.GetState().Position;
            var scaledMousePos = new Vector2(mousePos.X / Core.UiScale, mousePos.Y / Core.UiScale);
            
            // Combine Left/Top (screen position) with content bounds dimensions
            // Add buffer for button styling/borders not reflected in logical bounds
            var contentBounds = _equipmentPopup.Content?.Bounds ?? _equipmentPopup.Bounds;
            const int styleBuffer = 20;
            var popupBounds = new Rectangle(
                _equipmentPopup.Left,
                _equipmentPopup.Top,
                contentBounds.Width + styleBuffer,
                contentBounds.Height + styleBuffer
            );

            // Calculate distance from mouse to popup bounds (expanded by close distance)
            var expandedBounds = new Rectangle(
                popupBounds.X - PopupCloseDistance,
                popupBounds.Y - PopupCloseDistance,
                popupBounds.Width + PopupCloseDistance * 2,
                popupBounds.Height + PopupCloseDistance * 2
            );
            
            if (!expandedBounds.Contains((int)scaledMousePos.X, (int)scaledMousePos.Y))
            {
                CloseEquipmentPopup();
            }
        }

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
        private readonly BaseGui _gui;
        private readonly Pawn _pawn;
        private readonly int _cellSize = BaseContent.IconSizes.Large;
        private readonly BodyPart _bodyPart;
        private readonly Dictionary<EquipmentSlotType, Button> _slots = new();
        private readonly Image _imageFrame;
        private event Action<BodyPart, EquipmentSlotType>? ClickAction;
        private Dictionary<ItemDef, ColoredRegion> _iconCache = new();
        private IImage _potionSlotIcon;
        private IImage _bagSlotIcon;

        public EquipmentColumn(BaseGui gui, Pawn pawn, BodyPart bodyPart, List<EquipmentSlotType> slots, Action<BodyPart, EquipmentSlotType>? clickAction = null)
        {
            _gui = gui;
            _pawn = pawn;
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
                    Width = _cellSize,
                    Height = _cellSize
                };
                _slots.Add(slot, slotFrame);
                slotFrame.TouchDown += (_, _) => ClickAction?.Invoke(bodyPart, slot);
                Widgets.Add(slotFrame);
            }
        }

        public void Update()
        {
            foreach ((var slot, var image) in _slots)
            {
                bool isSlotEmpty = _bodyPart.Equipment[slot] == null;

                // Check if there's an available item in inventory for this empty slot
                bool hasAvailableEquipment = false;
                if (isSlotEmpty)
                {
                    hasAvailableEquipment = _pawn.Inventory.Any(i =>
                        i.ItemDef.EquipmentProperties?.SlotUsedToEquip == slot ||
                        (i.ItemDef.ItemType == ItemType.Potion && slot is EquipmentSlotType.PotionSlot1 or EquipmentSlotType.PotionSlot2));
                }

                if (hasAvailableEquipment)
                {                    
                    image.Content.BorderThickness = new Thickness(1);
                    image.Content.Border = new SolidBrush(Color.DarkRed);
                }
                else
                {
                    image.Content.BorderThickness = new Thickness(0);
                    image.Content.Border = null;
                }

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

        if (item.ItemDef.EquipmentProperties?.EquipmentType == EquipmentType.Weapon && bodyPart.HasMobility == false)
        {
            return Color.Red;
        }

        return Color.White;
    }
}