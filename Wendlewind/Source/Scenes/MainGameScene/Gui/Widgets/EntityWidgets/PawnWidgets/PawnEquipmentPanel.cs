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
    private const float GlowDuration = 0.7f;

    private readonly BaseGui _gui;
    private readonly Pawn _pawn;
    private readonly Dictionary<BodyPart, List<Widget>> _partWidgets = new();
    private readonly Dictionary<(BodyPart Part, EquipmentSlotType Slot), CursorButton> _slots = new();
    private readonly Dictionary<(BodyPart Part, EquipmentSlotType Slot), (int Col, int Row)> _slotCells = new();
    private readonly Dictionary<(BodyPart Part, EquipmentSlotType Slot), SlotFlash> _flashes = new();
    private readonly Dictionary<(BodyPart Part, EquipmentSlotType Slot), string> _lastMonikers = new();
    private readonly List<SlotSpark> _sparks = [];
    private static readonly Color DestroyedEquipmentColor = new(255, 0, 0, 15);
    private static readonly Color SlotHintColor = new(140, 130, 115);
    private readonly SelectionPopup<Item> _selectionPopup;
    private readonly int _cellSize;
    private readonly int _iconBaseSize;
    private readonly bool _showSlotHints;
    private readonly bool _readOnly;
    private readonly bool _hoverToInspect;
    private readonly Dictionary<ItemDef, ColoredIcon> _iconCache = new();
    private readonly Dictionary<(BodyPart Part, EquipmentSlotType Slot), bool> _lastAvailable = new();
    private readonly List<(BodyPart Part, EquipmentSlotType Slot)> _staleSlotKeys = [];
    private readonly SolidBrush _availableSlotBrush = new(Color.DarkGoldenrod);
    private readonly IImage _potionSlotIcon;
    private readonly IImage _bagSlotIcon;
    private static Texture2D? _glowTexture;

    public int PixelWidth { get; }

    public PawnEquipmentPanel(
        BaseGui gui,
        Pawn pawn,
        Action<BodyPart, EquipmentSlotType>? clickAction = null,
        int? cellSize = null,
        bool showSlotHints = true,
        bool readOnly = false,
        bool hoverToInspect = false)
    {
        _gui = gui;
        _pawn = pawn;
        _cellSize = cellSize ?? BaseContent.IconSizes.Large;
        _iconBaseSize = Math.Max(8, _cellSize - 6);
        _showSlotHints = showSlotHints;
        _readOnly = readOnly;
        _hoverToInspect = hoverToInspect;
        _selectionPopup = new SelectionPopup<Item>(gui.Desktop);
        _potionSlotIcon = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Icon.PotionSlot];
        _bagSlotIcon = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Icon.BagSlot];
        ColumnSpacing = 2;
        RowSpacing = 2;
        ClipToBounds = false;

        var layout = EquipmentGridLayout.Build(pawn);
        PixelWidth = layout.Columns * _cellSize + Math.Max(0, layout.Columns - 1) * ColumnSpacing;
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
            _slotCells[key] = (cell.Col, cell.Row);
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

        if (_hoverToInspect)
        {
            slotFrame.MouseEntered += (_, _) => ShowInspectPopup(bodyPart, slot, slotFrame);
            slotFrame.MouseLeft += (_, _) => TooltipHelper.Hide(slotFrame);
        }

        return slotFrame;
    }

    private void ShowInspectPopup(BodyPart bodyPart, EquipmentSlotType slot, Widget owner)
    {
        if (Desktop == null)
        {
            return;
        }

        if (bodyPart.Equipment[slot] is not { IsDestroyed: false } item)
        {
            return;
        }

        var panel = EntityPanelFactory.Create(_gui, item, new EntityPanelProperties
        {
            ShowTitle = true,
            ShowCloseButton = false,
            Background = null
        });

        TooltipHelper.ShowCustom(Desktop, panel, owner, TooltipPlacement.BottomCorner);
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

            _flashes[key] = new SlotFlash { Remaining = GlowDuration, Color = color };
            SpawnSparks(key, color, kind);
        }
    }

    public void Update()
    {
        Update(1f / 60f);
    }

    public void Update(float deltaTime)
    {
        _selectionPopup.Update();
        if (_hoverToInspect)
        {
            TooltipHelper.UpdatePosition();
        }
        TickFlashes(deltaTime);
        TickSparks(deltaTime);

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

            _staleSlotKeys.Clear();
            foreach (var key in _slots.Keys)
            {
                if (key.Part == bodyPart)
                {
                    _staleSlotKeys.Add(key);
                }
            }

            foreach (var key in _staleSlotKeys)
            {
                _slots.Remove(key);
                _slotCells.Remove(key);
                _flashes.Remove(key);
                _lastMonikers.Remove(key);
                _lastAvailable.Remove(key);
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
            // Glow is drawn in InternalRender; slot widgets stay put.
        }
    }

    private void UpdateSlot(BodyPart bodyPart, EquipmentSlotType slot, CursorButton image)
    {
        var key = (bodyPart, slot);
        var glowing = _flashes.ContainsKey(key);
        bool isSlotEmpty = bodyPart.Equipment[slot] == null;

        bool hasAvailableEquipment = false;
        if (!_readOnly && isSlotEmpty)
        {
            foreach (var inventoryItem in _pawn.Inventory)
            {
                if (inventoryItem.ItemDef.EquipmentProperties?.SlotUsedToEquip == slot
                    || (inventoryItem.ItemDef.ItemType == ItemType.Potion && slot is EquipmentSlotType.PotionSlot1 or EquipmentSlotType.PotionSlot2))
                {
                    hasAvailableEquipment = true;
                    break;
                }
            }
        }

        if (!_lastAvailable.TryGetValue(key, out var lastAvailable) || lastAvailable != hasAvailableEquipment)
        {
            _lastAvailable[key] = hasAvailableEquipment;
            if (hasAvailableEquipment)
            {
                image.Content.BorderThickness = new Thickness(2);
                image.Content.Border = _availableSlotBrush;
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
                _iconCache[item.ItemDef] = new ColoredIcon(item.GetIconImage(), Color.White);
            }

            _lastMonikers[key] = item.ItemDef.Moniker;
            progressBar.Visible = item.Durability > 1;
            progressBar.Value = item.Durability / item.MaxDurability * 100;
            icon.Visible = true;
            icon.Background = _iconCache[item.ItemDef];
            image.Content.Background = null;
            _iconCache[item.ItemDef].Color = GetEquipmentColor(item, bodyPart);

            hintLabel.Visible = false;
        }
        else if (glowing && icon.Background != null)
        {
            hintLabel.Visible = false;
            progressBar.Visible = false;
        }
        else
        {
            icon.Visible = false;
            icon.Background = null;
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

    private sealed class SlotSpark
    {
        public (BodyPart Part, EquipmentSlotType Slot) Key;
        public Vector2 Offset;
        public Vector2 Velocity;
        public float Life;
        public float MaxLife;
        public float Size;
        public Color Color;
        public float Gravity;
    }

    private Rectangle SlotBounds((BodyPart Part, EquipmentSlotType Slot) key)
    {
        if (!_slotCells.TryGetValue(key, out var cell))
        {
            return Rectangle.Empty;
        }

        var origin = ActualBounds;
        return new Rectangle(
            origin.X + cell.Col * (_cellSize + ColumnSpacing),
            origin.Y + cell.Row * (_cellSize + RowSpacing),
            _cellSize,
            _cellSize);
    }

    private void SpawnSparks((BodyPart Part, EquipmentSlotType Slot) key, Color color, EquipmentFlashKind kind)
    {
        var (count, speedMin, speedMax, gravity, life) = kind switch
        {
            EquipmentFlashKind.Block => (8, 20f, 50f, 0f, 0.4f),
            EquipmentFlashKind.Proc => (12, 35f, 85f, -20f, 0.5f),
            EquipmentFlashKind.Potion => (8, 24f, 60f, -80f, 0.45f),
            EquipmentFlashKind.Destroyed => (14, 50f, 110f, 180f, 0.5f),
            _ => (10, 28f, 75f, 40f, 0.45f)
        };

        for (var i = 0; i < count; i++)
        {
            var angle = kind == EquipmentFlashKind.Potion
                ? MathHelper.ToRadians(Rng.Visual.Next(-50, 51) - 90)
                : MathHelper.ToRadians(Rng.Visual.Next(0, 360));
            var speed = Rng.Visual.Next((int)speedMin, (int)speedMax + 1);
            _sparks.Add(new SlotSpark
            {
                Key = key,
                Offset = new Vector2(Rng.Visual.Next(-3, 4), Rng.Visual.Next(-3, 4)),
                Velocity = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * speed,
                Life = life,
                MaxLife = life,
                Size = Rng.Visual.Next(4, 8),
                Color = color,
                Gravity = gravity
            });
        }
    }

    private void TickSparks(float deltaTime)
    {
        for (var i = _sparks.Count - 1; i >= 0; i--)
        {
            var spark = _sparks[i];
            spark.Life -= deltaTime;
            if (spark.Life <= 0 || !_slotCells.ContainsKey(spark.Key))
            {
                _sparks.RemoveAt(i);
                continue;
            }

            spark.Velocity.Y += spark.Gravity * deltaTime;
            spark.Offset += spark.Velocity * deltaTime;
        }
    }

    public override void InternalRender(RenderContext context)
    {
        base.InternalRender(context);
        DrawGlows(context);
        DrawSparks(context);
    }

    private void DrawGlows(RenderContext context)
    {
        if (_flashes.Count == 0)
        {
            return;
        }

        var glow = EnsureGlowTexture();
        foreach (var (key, flash) in _flashes)
        {
            var bounds = SlotBounds(key);
            if (bounds.Width <= 0 || bounds.Height <= 0)
            {
                continue;
            }

            var progress = Math.Clamp(1f - flash.Remaining / GlowDuration, 0f, 1f);
            var pulse = MathF.Sin(progress * MathF.PI);
            if (pulse < 0.02f)
            {
                continue;
            }

            var center = new Vector2(bounds.X + bounds.Width * 0.5f, bounds.Y + bounds.Height * 0.5f);
            var cell = Math.Max(bounds.Width, bounds.Height);
            DrawHalo(context, glow, center, cell * (1.85f + 0.45f * pulse), flash.Color * (0.55f * pulse));
            DrawHalo(context, glow, center, cell * (1.15f + 0.2f * pulse), Color.Lerp(flash.Color, Color.White, 0.25f) * (0.22f * pulse));
        }
    }

    private static void DrawHalo(RenderContext context, Texture2D glow, Vector2 center, float diameter, Color color)
    {
        var size = Math.Max(8, (int)diameter);
        context.Draw(glow, new Rectangle(
            (int)center.X - size / 2,
            (int)center.Y - size / 2,
            size,
            size), color);
    }

    private void DrawSparks(RenderContext context)
    {
        if (_sparks.Count == 0)
        {
            return;
        }

        var glow = EnsureGlowTexture();
        foreach (var spark in _sparks)
        {
            var bounds = SlotBounds(spark.Key);
            if (bounds.Width <= 0 || bounds.Height <= 0)
            {
                continue;
            }

            var pos = new Vector2(bounds.X + bounds.Width * 0.5f, bounds.Y + bounds.Height * 0.5f) + spark.Offset;
            var t = Math.Clamp(spark.Life / spark.MaxLife, 0f, 1f);
            var fade = t < 0.25f ? t / 0.25f : 1f;
            var size = Math.Max(6, (int)(spark.Size * 2.6f));
            context.Draw(glow, new Rectangle(
                (int)pos.X - size / 2,
                (int)pos.Y - size / 2,
                size,
                size), spark.Color * (0.75f * fade));
        }
    }

    private static Texture2D EnsureGlowTexture()
    {
        if (_glowTexture != null)
        {
            return _glowTexture;
        }

        const int size = 128;
        var data = new Color[size * size];
        var center = (size - 1) * 0.5f;
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var dx = x - center;
                var dy = y - center;
                var dist = MathF.Sqrt(dx * dx + dy * dy) / center;
                var alpha = MathF.Max(0f, 1f - dist);
                alpha = alpha * alpha * alpha;
                var a = (byte)(alpha * 255f);
                data[y * size + x] = new Color(a, a, a, a);
            }
        }

        _glowTexture = new Texture2D(Core.GraphicsDevice, size, size);
        _glowTexture.SetData(data);
        return _glowTexture;
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
