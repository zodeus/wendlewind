namespace Wendlewind.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets;

public enum EquipmentFlashKind
{
    Strike,
    Block,
    Proc,
    Potion,
    Destroyed
}

public class PawnEquipmentPanel : Grid, IUpdatable
{
    private const float FlashDuration = 0.4f;
    private const int PulsePixels = 4;

    private readonly BaseGui _gui;
    private readonly Pawn _pawn;
    private readonly Dictionary<BodyPart, List<Widget>> _partWidgets = new();
    private readonly Dictionary<(BodyPart Part, EquipmentSlotType Slot), CursorButton> _slots = new();
    private readonly Dictionary<(BodyPart Part, EquipmentSlotType Slot), SlotFlash> _flashes = new();
    private readonly Dictionary<(BodyPart Part, EquipmentSlotType Slot), string> _lastMonikers = new();
    private static readonly Color DestroyedEquipmentColor = new(255, 0, 0, 15);
    private static readonly Color SlotHintColor = new(140, 130, 115);
    private readonly SelectionPopup<Item> _selectionPopup;
    private readonly int _cellSize;
    private readonly int _iconBaseSize;
    private readonly bool _showSlotHints;
    private readonly bool _readOnly;
    private readonly Dictionary<ItemDef, ColoredRegion> _iconCache = new();
    private readonly IImage _potionSlotIcon;
    private readonly IImage _bagSlotIcon;

    public PawnEquipmentPanel(
        BaseGui gui,
        Pawn pawn,
        Action<BodyPart, EquipmentSlotType>? clickAction = null,
        int? cellSize = null,
        bool showSlotHints = true,
        bool readOnly = false)
    {
        _gui = gui;
        _pawn = pawn;
        _cellSize = cellSize ?? BaseContent.IconSizes.Large;
        _iconBaseSize = Math.Max(8, _cellSize - 6);
        _showSlotHints = showSlotHints;
        _readOnly = readOnly;
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
                    new Image
                    {
                        Width = _iconBaseSize,
                        Height = _iconBaseSize,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        Visible = false
                    },
                    new HorizontalProgressBar(BaseContent.Styles.Bar.Durability)
                    {
                        Width = Math.Max(8, _cellSize - 4),
                        Height = Math.Max(8, _cellSize / 6),
                        Minimum = 0,
                        Maximum = 100,
                        Padding = new Thickness(1),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Bottom
                    },
                    new Label(BaseContent.Styles.Label.Small)
                    {
                        Text = GetSlotHint(slot),
                        TextColor = SlotHintColor,
                        Visible = _showSlotHints,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                }
            },
            Width = _cellSize,
            Height = _cellSize
        };
        if (!_readOnly)
        {
            slotFrame.TouchDown += (_, _) => onClick(bodyPart, slot);
        }
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

    public void FlashSlot(string? itemMoniker, EquipmentFlashKind kind)
    {
        if (string.IsNullOrEmpty(itemMoniker))
        {
            return;
        }

        var color = ColorFor(kind);
        foreach (var key in _slots.Keys)
        {
            var current = key.Part.Equipment[key.Slot];
            var moniker = current is { IsDestroyed: false }
                ? current.ItemDef.Moniker
                : _lastMonikers.GetValueOrDefault(key);
            if (moniker != itemMoniker)
            {
                continue;
            }

            _flashes[key] = new SlotFlash { Remaining = FlashDuration, Color = color };
        }
    }

    public void Update()
    {
        Update(1f / 60f);
    }

    public void Update(float deltaTime)
    {
        _selectionPopup.Update();
        TickFlashes(deltaTime);

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
                _flashes.Remove(key);
                _lastMonikers.Remove(key);
            }
        }
    }

    private void TickFlashes(float deltaTime)
    {
        if (_flashes.Count == 0)
        {
            return;
        }

        var expired = new List<(BodyPart Part, EquipmentSlotType Slot)>();
        foreach (var (key, flash) in _flashes)
        {
            flash.Remaining -= deltaTime;
            if (flash.Remaining <= 0)
            {
                expired.Add(key);
            }
        }

        foreach (var key in expired)
        {
            _flashes.Remove(key);
            if (_slots.TryGetValue(key, out var button))
            {
                ResetPulse(button);
            }
        }
    }

    private void UpdateSlot(BodyPart bodyPart, EquipmentSlotType slot, CursorButton image)
    {
        var key = (bodyPart, slot);
        var flashing = _flashes.TryGetValue(key, out var flash);
        bool isSlotEmpty = bodyPart.Equipment[slot] == null;

        bool hasAvailableEquipment = false;
        if (!_readOnly && isSlotEmpty)
        {
            hasAvailableEquipment = _pawn.Inventory.Any(i =>
                i.ItemDef.EquipmentProperties?.SlotUsedToEquip == slot ||
                (i.ItemDef.ItemType == ItemType.Potion && slot is EquipmentSlotType.PotionSlot1 or EquipmentSlotType.PotionSlot2));
        }

        if (!flashing)
        {
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
        }

        var (icon, progressBar, hintLabel) = SlotParts(image);

        if (bodyPart.Equipment[slot] is { IsDestroyed: false } item)
        {
            if (_iconCache.ContainsKey(item.ItemDef) == false)
            {
                _iconCache[item.ItemDef] = new ColoredRegion(new TextureRegion(item.GetIcon()), Color.White);
            }

            _lastMonikers[key] = item.ItemDef.Moniker;
            progressBar.Visible = item.Durability > 1;
            progressBar.Value = item.Durability / item.MaxDurability * 100;
            icon.Visible = true;
            icon.Background = _iconCache[item.ItemDef];
            image.Content.Background = null;
            if (!flashing)
            {
                _iconCache[item.ItemDef].Color = GetEquipmentColor(item, bodyPart);
                ResetPulse(image);
            }

            hintLabel.Visible = false;
        }
        else if (flashing && icon.Background != null)
        {
            hintLabel.Visible = false;
            progressBar.Visible = false;
        }
        else
        {
            icon.Visible = false;
            icon.Background = null;
            ResetPulse(image);
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
                hintLabel.Visible = _showSlotHints;
            }

            progressBar.Visible = false;
        }

        if (flashing && flash != null)
        {
            ApplyFlash(image, icon, flash);
        }
    }

    private void ApplyFlash(CursorButton button, Image icon, SlotFlash flash)
    {
        var t = Math.Clamp(1f - flash.Remaining / FlashDuration, 0f, 1f);
        var pulse = MathF.Sin(t * MathF.PI);
        var size = _iconBaseSize + (int)(PulsePixels * pulse);
        icon.Visible = true;
        icon.Width = size;
        icon.Height = size;
        button.Content.BorderThickness = new Thickness(1 + (int)(2 * pulse));
        button.Content.Border = new SolidBrush(flash.Color);
        if (icon.Background is ColoredRegion tint)
        {
            tint.Color = Color.Lerp(Color.White, flash.Color, pulse);
        }
    }

    private void ResetPulse(CursorButton button)
    {
        var (icon, _, _) = SlotParts(button);
        icon.Width = _iconBaseSize;
        icon.Height = _iconBaseSize;
    }

    private static (Image Icon, HorizontalProgressBar Bar, Label Hint) SlotParts(CursorButton button)
    {
        var panel = (Panel)button.Content;
        return ((Image)panel.Widgets[0], (HorizontalProgressBar)panel.Widgets[1], (Label)panel.Widgets[2]);
    }

    private static Color ColorFor(EquipmentFlashKind kind) => kind switch
    {
        EquipmentFlashKind.Strike => new Color(220, 180, 70),
        EquipmentFlashKind.Block => new Color(140, 190, 210),
        EquipmentFlashKind.Proc => new Color(255, 215, 80),
        EquipmentFlashKind.Potion => new Color(255, 215, 80),
        EquipmentFlashKind.Destroyed => Color.Red,
        _ => Color.White
    };

    private sealed class SlotFlash
    {
        public float Remaining;
        public Color Color;
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
