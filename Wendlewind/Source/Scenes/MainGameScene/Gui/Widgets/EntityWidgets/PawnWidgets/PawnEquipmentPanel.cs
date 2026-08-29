namespace Wendlewind.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets;

public class PawnEquipmentPanel : Grid, IUpdatable
{
    private readonly BaseGui _gui;
    private readonly Pawn _pawn;
    private readonly Dictionary<BodyPart, List<Widget>> _partWidgets = new();
    private readonly Dictionary<(BodyPart Part, EquipmentSlotType Slot), CursorButton> _slots = new();
    private static readonly Color DestroyedEquipmentColor = new(255, 0, 0, 15);
    private static readonly Color SlotHintColor = new(140, 130, 115);
    private readonly SelectionPopup<Item> _selectionPopup;
    private readonly int _cellSize = BaseContent.IconSizes.Large;
    private readonly Dictionary<ItemDef, ColoredRegion> _iconCache = new();
    private readonly IImage _potionSlotIcon;
    private readonly IImage _bagSlotIcon;

    public PawnEquipmentPanel(BaseGui gui, Pawn pawn, Action<BodyPart, EquipmentSlotType>? clickAction = null)
    {
        _gui = gui;
        _pawn = pawn;
        _selectionPopup = new SelectionPopup<Item>(gui.Desktop);
        _potionSlotIcon = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Icon.PotionSlot];
        _bagSlotIcon = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Icon.BagSlot];
        ColumnSpacing = 2;
        RowSpacing = 2;

        var layout = EquipmentGridLayout.Build(pawn);
        for (var i = 0; i < layout.Columns; i++)
        {
            ColumnsProportions.Add(new Proportion(ProportionType.Pixels, _cellSize));
        }

        for (var i = 0; i < layout.Rows; i++)
        {
            RowsProportions.Add(new Proportion(ProportionType.Pixels, _cellSize));
        }

        Action<BodyPart, EquipmentSlotType> onClick = (part, type) =>
        {
            if (clickAction != null)
            {
                clickAction.Invoke(part, type);
                return;
            }

            HandleClick(part, type);
        };

        foreach (var (key, cell) in layout.Slots)
        {
            var slotFrame = CreateSlotButton(key.Part, key.Slot, onClick);
            Place(slotFrame, cell.Col, cell.Row);
            Track(key.Part, slotFrame);
            _slots[key] = slotFrame;
        }
    }

    private void Place(Widget widget, int col, int row)
    {
        Widgets.Add(widget);
        SetColumn(widget, col);
        SetRow(widget, row);
    }

    private void Track(BodyPart bodyPart, Widget widget)
    {
        if (!_partWidgets.TryGetValue(bodyPart, out var widgets))
        {
            widgets = [];
            _partWidgets[bodyPart] = widgets;
        }

        widgets.Add(widget);
    }

    private CursorButton CreateSlotButton(BodyPart bodyPart, EquipmentSlotType slot, Action<BodyPart, EquipmentSlotType> onClick)
    {
        var slotFrame = new CursorButton(BaseContent.Styles.Button.Icon)
        {
            Content = new Panel
            {
                Widgets =
                {
                    new HorizontalProgressBar(BaseContent.Styles.Bar.Durability)
                    {
                        Width = _cellSize - 4, Height = 12, HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Bottom
                    },
                    new Label(BaseContent.Styles.Label.Small)
                    {
                        Text = GetSlotHint(slot),
                        TextColor = SlotHintColor,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                }
            },
            Width = _cellSize,
            Height = _cellSize
        };
        slotFrame.TouchDown += (_, _) => onClick(bodyPart, slot);
        return slotFrame;
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
                    // ApprenticeFixer trait: repairing also increases max durability by 10%
                    if (_pawn.Traits.HasTrait(Defs.Traits.ApprenticeFixer))
                    {
                        equipmentItem.MaxDurability *= 1.1f;
                    }

                    equipmentItem.Repair();

                    // Track achievement progress
                    Core.Context.Achievements.OnItemUsed(_pawn, item);

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
                        potion = Core.Context.Factory.CreateEntity<Item>(item.ItemDef, 1);
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
        if (_selectionPopup.IsOpen) return;

        // Find available items in inventory that can be equipped to this slot
        var availableItems = _pawn.Inventory
            .Where(i => i.ItemDef.EquipmentProperties?.SlotUsedToEquip == slot ||
                       (i.ItemDef.ItemType == ItemType.Potion && slot is EquipmentSlotType.PotionSlot1 or EquipmentSlotType.PotionSlot2));

        _selectionPopup.Show(
            availableItems,
            i => i.GetIcon(),
            i => EquipItemFromInventory(part, slot, i));
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
                potion = Core.Context.Factory.CreateEntity<Item>(item.ItemDef, 1);
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
        _selectionPopup.Update();

        foreach (var ((bodyPart, slot), image) in _slots)
        {
            UpdateSlot(bodyPart, slot, image);
        }

        List<BodyPart>? severed = null;
        foreach (var bodyPart in _partWidgets.Keys)
        {
            if (!bodyPart.IsSevered)
            {
                continue;
            }

            severed ??= [];
            severed.Add(bodyPart);
        }

        if (severed == null)
        {
            return;
        }

        foreach (var bodyPart in severed)
        {
            if (_partWidgets.Remove(bodyPart, out var widgets))
            {
                foreach (var widget in widgets)
                {
                    widget.RemoveFromParent();
                }
            }

            var staleSlots = _slots.Keys.Where(key => key.Part == bodyPart).ToList();
            foreach (var key in staleSlots)
            {
                _slots.Remove(key);
            }
        }
    }

    private void UpdateSlot(BodyPart bodyPart, EquipmentSlotType slot, CursorButton image)
    {
        bool isSlotEmpty = bodyPart.Equipment[slot] == null;

        bool hasAvailableEquipment = false;
        if (isSlotEmpty)
        {
            hasAvailableEquipment = _pawn.Inventory.Any(i =>
                i.ItemDef.EquipmentProperties?.SlotUsedToEquip == slot ||
                (i.ItemDef.ItemType == ItemType.Potion && slot is EquipmentSlotType.PotionSlot1 or EquipmentSlotType.PotionSlot2));
        }

        if (hasAvailableEquipment)
        {
            image.Content.BorderThickness = new Thickness(2);
            image.Content.Border = new SolidBrush(Color.DarkGoldenrod);
        }
        else
        {
            image.Content.BorderThickness = new Thickness(0);
            image.Content.Border = null;
        }

        var progressBar = (HorizontalProgressBar)((Panel)image.Content).Widgets[0];
        var hintLabel = (Label)((Panel)image.Content).Widgets[1];

        if (bodyPart.Equipment[slot] is { IsDestroyed: false } item)
        {
            if (_iconCache.ContainsKey(item.ItemDef) == false)
            {
                _iconCache[item.ItemDef] = new ColoredRegion(new TextureRegion(item.GetIcon()), Color.White);
            }

            progressBar.Visible = item.Durability > 1;
            image.Content.Background = _iconCache[item.ItemDef];
            progressBar.Value = item.Durability / item.MaxDurability * 100;
            ((ColoredRegion)image.Content.Background).Color = GetEquipmentColor(item, bodyPart);
            hintLabel.Visible = false;
        }
        else
        {
            if (slot is EquipmentSlotType.PotionSlot1 or EquipmentSlotType.PotionSlot2)
            {
                image.Content.Background = _potionSlotIcon;
                hintLabel.Visible = false;
            }
            else if (slot is EquipmentSlotType.Bag)
            {
                image.Content.Background = _bagSlotIcon;
                hintLabel.Visible = false;
            }
            else
            {
                image.Content.Background = null;
                hintLabel.Visible = true;
            }

            progressBar.Visible = false;
        }
    }

    /// <summary>
    /// Short word shown in an empty slot so it's clear what the slot is for.
    /// </summary>
    private static string GetSlotHint(EquipmentSlotType slot) => slot switch
    {
        EquipmentSlotType.HandWeapon => "Wpn",
        EquipmentSlotType.HandArmor => "Hand",
        EquipmentSlotType.FootWeapon => "Kick",
        EquipmentSlotType.FootArmor => "Foot",
        EquipmentSlotType.LegArmor => "Leg",
        EquipmentSlotType.ArmArmor => "Arm",
        EquipmentSlotType.TorsoArmor => "Body",
        EquipmentSlotType.NeckArmor => "Neck",
        EquipmentSlotType.HeadArmor => "Head",
        EquipmentSlotType.Cloak => "Cloak",
        EquipmentSlotType.Necklace => "Amulet",
        _ => string.Empty
    };

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
