using Wendlemire.Scenes.MainGameScene.Gui;
using Wendlemire.Scenes.MainGameScene.Gui.Widgets.EntityWidgets;
using Wendlemire.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets.PawnPreparationPanelWidgets;
using Wendlemire.Sim.Arena;
using Image = Myra.Graphics2D.UI.Image;

namespace Wendlemire.Scenes.MainGameScene.Gui.Widgets.CombatWidgets;

public sealed class MedicalBar : Grid, IUpdatable
{
    public const int Columns = 3;
    public const int Rows = 4;

    private readonly Pawn _pawn;
    private readonly List<MedicalSlotView> _slots = [];

    public MedicalBar(
        BaseGui gui,
        Pawn pawn,
        Action<ItemDef>? clickHandler,
        int cellSize,
        int iconSize,
        int cellPad)
    {
        _pawn = pawn;
        pawn.MedicalChest.Prune();
        ColumnSpacing = 5;
        RowSpacing = 5;
        ClipToBounds = false;
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Top;
        for (var i = 0; i < Columns; i++)
        {
            ColumnsProportions.Add(new Proportion(ProportionType.Pixels, cellSize));
        }

        for (var i = 0; i < Rows; i++)
        {
            RowsProportions.Add(new Proportion(ProportionType.Pixels, cellSize));
        }

        var armed = pawn.MedicalChest.Slots;
        var capacity = Math.Clamp(pawn.MedicalChest.Capacity, 0, MedicalChest.MaxSlots);
        for (var i = 0; i < MedicalChest.MaxSlots; i++)
        {
            Widget cell;
            if (i < armed.Count)
            {
                var view = new MedicalSlotView(gui, pawn, armed[i], iconSize, clickHandler);
                _slots.Add(view);
                cell = Frame(view, cellSize, cellPad, clip: false);
            }
            else if (i < capacity)
            {
                cell = UnusedFrame(cellSize, cellPad);
            }
            else
            {
                var tip = SlotUnlockTooltip.ForSlot(PrepSlotKind.Medical, i + 1);
                cell = LockedFrame(cellSize, cellPad, tip.title, tip.description);
            }

            Grid.SetRow(cell, i / Columns);
            Grid.SetColumn(cell, i % Columns);
            Widgets.Add(cell);
        }
    }

    public Widget? NotifyUsed(string? itemMoniker)
    {
        foreach (var slot in _slots)
        {
            if (itemMoniker == null || slot.ChestSlot.Def.Moniker != itemMoniker)
            {
                continue;
            }

            slot.Flash();
            return slot;
        }

        return null;
    }

    public bool TryGetSlot(string? itemMoniker, out Widget slot)
    {
        foreach (var view in _slots)
        {
            if (itemMoniker != null && view.ChestSlot.Def.Moniker != itemMoniker)
            {
                continue;
            }

            slot = view;
            return true;
        }

        slot = null!;
        return false;
    }

    public void Update()
    {
        Update(1f / 60f);
    }

    public void Update(float deltaTime)
    {
        var tick = Core.Context.CurrentZone?.ActiveEncounter?.Ticks
            ?? _pawn.Zone?.ActiveEncounter?.Ticks
            ?? 0;
        foreach (var slot in _slots)
        {
            slot.Update(deltaTime, tick);
        }
    }

    private static Panel Frame(Widget content, int cellSize, int cellPad, bool clip = true)
    {
        return new Panel
        {
            Width = cellSize,
            Height = cellSize,
            ClipToBounds = clip,
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.IconFrame],
            Padding = new Thickness(cellPad),
            Widgets = { content }
        };
    }

    private static Panel UnusedFrame(int cellSize, int cellPad)
    {
        var cell = Frame(new Panel(), cellSize, cellPad);
        cell.Opacity = 0.7f;
        cell.WithTooltip("Unused medical slot", "This slot is empty and will not heal in the fight.");
        return cell;
    }

    private static Panel LockedFrame(int cellSize, int cellPad, string title, string? description)
    {
        var icon = LockedSlotChrome.Icon(Math.Max(12, cellSize - cellPad * 2 - 8));
        var cell = Frame(icon, cellSize, cellPad);
        cell.Opacity = 0.7f;
        cell.WithTooltip(title, description);
        return cell;
    }
}

internal sealed class MedicalSlotView : Panel
{
    private const float GlowDuration = 1.45f;
    private static readonly Color HealColor = new(90, 210, 140);
    private static readonly Color CauterizeColor = new(255, 140, 50);

    private static readonly Color ReadyTint = Color.White;
    private static readonly Color CooldownTint = new(70, 70, 68);
    private static readonly Color EmptyTint = new(48, 48, 46);
    private static readonly Color LockedTint = new(58, 56, 52);
    private static readonly Color TimerColor = new(220, 200, 150);
    private static readonly Color TimePip = new(220, 190, 90);
    private static readonly Color BloodPip = new(186, 40, 35);
    private static readonly Color PartsPip = new(220, 160, 60);
    private static readonly Color CrisisPip = new(255, 140, 50);
    private static readonly Color StatusPip = new(140, 190, 70);

    private readonly BaseGui _gui;
    private readonly Pawn _pawn;
    private readonly CursorButton _button;
    private readonly ColoredIcon _tint;
    private readonly Panel _dim;
    private readonly Panel _pip;
    private readonly Label _chargeLabel;
    private readonly Panel _cooldownChip;
    private readonly Label _cooldownLabel;
    private readonly List<SlotSpark> _sparks = [];
    private static Texture2D? _glowTexture;
    private float _flashRemaining;
    private float _urgency;
    private float _urgencyPhase;
    private Item? _hoverInspectItem;
    private Widget? _hoverInspectOwner;
    private Item? _previewItem;

    public readonly MedicalChestSlot ChestSlot;

    public MedicalSlotView(BaseGui gui, Pawn pawn, MedicalChestSlot chestSlot, int size, Action<ItemDef>? clickHandler)
    {
        _gui = gui;
        _pawn = pawn;
        ChestSlot = chestSlot;
        ClipToBounds = false;
        Width = size;
        Height = size;
        HorizontalAlignment = HorizontalAlignment.Center;
        VerticalAlignment = VerticalAlignment.Center;

        _tint = new ColoredIcon(chestSlot.Def.GetIconImage(), Color.White);
        var icon = new Image
        {
            Background = _tint,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };

        _dim = new Panel
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Background = new SolidBrush(new Color(8, 8, 8, 0)),
            Visible = false
        };

        _pip = new Panel
        {
            Width = 7,
            Height = 7,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(1),
            Background = new SolidBrush(TriggerColor(chestSlot.Trigger?.Type ?? MedicalTriggerType.Immediately)),
            Border = new SolidBrush(new Color(10, 8, 6, 180)),
            BorderThickness = new Thickness(1)
        };

        _chargeLabel = new Label(BaseContent.Styles.Label.Small)
        {
            Text = ChargeText(chestSlot),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom,
            TextColor = Color.White
        };

        _cooldownLabel = new Label(BaseContent.Styles.Label.Small)
        {
            Text = "",
            TextColor = TimerColor,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        _cooldownChip = new Panel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Padding = new Thickness(4, 1),
            Background = new SolidBrush(new Color(10, 8, 6, 210)),
            Border = new SolidBrush(new Color(200, 180, 120, 70)),
            BorderThickness = new Thickness(1),
            Visible = false,
            Widgets = { _cooldownLabel }
        };

        _button = new CursorButton
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Padding = new Thickness(0),
            Content = new Panel
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                ClipToBounds = false,
                Widgets = { icon, _dim, _pip, _chargeLabel, _cooldownChip }
            }
        };

        var def = chestSlot.Def;
        _button.TouchDown += (_, _) =>
        {
            HideHoverInspect();
            clickHandler?.Invoke(def);
        };

        Widgets.Add(_button);
    }

    protected override void OnPlacedChanged()
    {
        base.OnPlacedChanged();
        if (!IsPlaced)
        {
            HideHoverInspect();
        }
    }

    private void HideHoverInspect()
    {
        if (_hoverInspectItem == null && _hoverInspectOwner == null)
        {
            return;
        }

        TooltipHelper.Hide(_hoverInspectOwner);
        _hoverInspectItem = null;
        _hoverInspectOwner = null;
    }

    private void UpdateHoverInspect()
    {
        var desktop = Desktop ?? _gui.Desktop;
        if (desktop == null)
        {
            return;
        }

        if (!_button.Visible || !_button.ContainsGlobalPoint(desktop.MousePosition))
        {
            HideHoverInspect();
            return;
        }

        var item = ResolveInspectItem();
        if (ReferenceEquals(_hoverInspectItem, item) && _hoverInspectOwner == _button)
        {
            TooltipHelper.UpdatePosition();
            return;
        }

        _hoverInspectItem = item;
        _hoverInspectOwner = _button;
        ShowInspectPopup();
    }

    private void ShowInspectPopup()
    {
        var desktop = Desktop ?? _gui.Desktop;
        if (desktop == null || _hoverInspectItem == null || _hoverInspectOwner == null)
        {
            return;
        }

        var panel = EntityPanelFactory.Create(_gui, _hoverInspectItem, new EntityPanelProperties
        {
            ShowTitle = false,
            ShowCloseButton = false,
            Background = null
        });

        TooltipHelper.ShowCustom(desktop, panel, _hoverInspectOwner, TooltipPlacement.BottomCorner);
    }

    private Item ResolveInspectItem()
    {
        var live = _pawn.Inventory.FirstOrDefault(i => i.Def == ChestSlot.Def && !i.IsDestroyed);
        if (live != null)
        {
            return live;
        }

        return _previewItem ??= _pawn.Context.Factory.CreateEntity<Item>(ChestSlot.Def, 1);
    }

    public void Flash()
    {
        _flashRemaining = GlowDuration;
        SpawnSparks(FlashColor());
    }

    public void Update(float deltaTime, int tick)
    {
        _flashRemaining = Math.Max(0f, _flashRemaining - deltaTime);
        TickSparks(deltaTime);
        _chargeLabel.Text = ChargeText(ChestSlot);

        var empty = !ChestSlot.HasCharge;
        var locked = MedicalChest.IsLockedForRestOfCombat(ChestSlot);
        var remainingTicks = locked ? 0 : ChestSlot.NextReadyTick - tick;
        var cooling = remainingTicks > 0;
        var muted = empty || cooling || locked;
        var trigger = ChestSlot.Trigger;
        _urgency = !muted && trigger != null
            ? trigger.GetUrgency(_pawn, _pawn, tick, ChestSlot.Def)
            : 0f;
        _urgencyPhase += deltaTime * (0.8f + _urgency * 3.2f);
        _pip.Background = new SolidBrush(TriggerColor(trigger?.Type ?? MedicalTriggerType.Immediately)
            * (locked || empty ? 0.45f : 1f));

        if (_flashRemaining > 0f)
        {
            var progress = Math.Clamp(1f - _flashRemaining / GlowDuration, 0f, 1f);
            var pulse = MathF.Sin(progress * MathF.PI);
            _tint.Color = Color.Lerp(muted ? CooldownTint : ReadyTint, FlashColor(), pulse);
            _dim.Visible = muted;
            _dim.Background = new SolidBrush(new Color(8, 8, 8, (int)(90 * (1f - pulse))));
        }
        else if (locked)
        {
            _tint.Color = LockedTint;
            _dim.Visible = true;
            _dim.Background = new SolidBrush(new Color(8, 8, 8, 150));
        }
        else if (empty)
        {
            _tint.Color = EmptyTint;
            _dim.Visible = true;
            _dim.Background = new SolidBrush(new Color(8, 8, 8, 170));
        }
        else if (cooling)
        {
            _tint.Color = CooldownTint;
            _dim.Visible = true;
            _dim.Background = new SolidBrush(new Color(8, 8, 8, 150));
        }
        else
        {
            _tint.Color = ReadyTint;
            _dim.Visible = false;
        }

        if (cooling)
        {
            ShowChip(remainingTicks / (float)GameContext.TicksPerSecond, imminent: remainingTicks < GameContext.TicksPerSecond);
        }
        else if (!locked && !empty && trigger?.Type == MedicalTriggerType.AfterSeconds)
        {
            var untilFire = trigger.AfterSeconds * GameContext.TicksPerSecond - tick;
            if (untilFire > 0)
            {
                ShowChip(untilFire / GameContext.TicksPerSecond, imminent: untilFire < GameContext.TicksPerSecond);
            }
            else
            {
                _cooldownChip.Visible = false;
            }
        }
        else
        {
            _cooldownChip.Visible = false;
        }

        UpdateHoverInspect();
    }

    private void ShowChip(float seconds, bool imminent)
    {
        _cooldownLabel.Text = seconds >= 10f ? $"{seconds:0}s" : $"{seconds:0.0}s";
        _cooldownLabel.TextColor = imminent ? FlashColor() : TimerColor;
        _cooldownChip.Visible = true;
        _cooldownChip.Border = new SolidBrush(imminent
            ? FlashColor() * 0.55f
            : new Color(200, 180, 120, 70));
    }

    public override void InternalRender(RenderContext context)
    {
        base.InternalRender(context);
        DrawGlow(context);
        DrawSparks(context);
    }

    private Color FlashColor()
    {
        return ChestSlot.Def == Defs.Items.Cauterize ? CauterizeColor : HealColor;
    }

    private static Color TriggerColor(MedicalTriggerType type)
    {
        return type switch
        {
            MedicalTriggerType.Immediately or MedicalTriggerType.AfterSeconds => TimePip,
            MedicalTriggerType.SelfBloodBelow => BloodPip,
            MedicalTriggerType.SelfPartsDamaged or MedicalTriggerType.PartBelowHealth => PartsPip,
            MedicalTriggerType.PartSevered or MedicalTriggerType.BurningOrAcid => CrisisPip,
            MedicalTriggerType.HasNecrosis or MedicalTriggerType.HasPoison => StatusPip,
            _ => TimePip
        };
    }

    private void SpawnSparks(Color color)
    {
        for (var i = 0; i < 12; i++)
        {
            var angle = MathHelper.ToRadians(Rng.Visual.Next(0, 360));
            var speed = Rng.Visual.Next(35, 86);
            _sparks.Add(new SlotSpark
            {
                Offset = new Vector2(Rng.Visual.Next(-3, 4), Rng.Visual.Next(-3, 4)),
                Velocity = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * speed,
                Life = 0.75f,
                MaxLife = 0.75f,
                Size = Rng.Visual.Next(4, 8),
                Color = color,
                Gravity = -20f
            });
        }
    }

    private void TickSparks(float deltaTime)
    {
        for (var i = _sparks.Count - 1; i >= 0; i--)
        {
            var spark = _sparks[i];
            spark.Life -= deltaTime;
            if (spark.Life <= 0)
            {
                _sparks.RemoveAt(i);
                continue;
            }

            spark.Velocity.Y += spark.Gravity * deltaTime;
            spark.Offset += spark.Velocity * deltaTime;
        }
    }

    private void DrawGlow(RenderContext context)
    {
        var bounds = ActualBounds;
        var center = new Vector2(bounds.X + bounds.Width * 0.5f, bounds.Y + bounds.Height * 0.5f);
        var cell = Math.Max(bounds.Width, bounds.Height);
        var glow = EnsureGlowTexture();

        if (_flashRemaining > 0f)
        {
            var progress = Math.Clamp(1f - _flashRemaining / GlowDuration, 0f, 1f);
            var pulse = MathF.Sin(progress * MathF.PI);
            if (pulse >= 0.02f)
            {
                var color = FlashColor();
                DrawHalo(context, glow, center, cell * (1.85f + 0.45f * pulse), color * (0.55f * pulse));
                DrawHalo(context, glow, center, cell * (1.15f + 0.2f * pulse), Color.Lerp(color, Color.White, 0.25f) * (0.22f * pulse));
            }

            return;
        }

        if (_urgency < 0.08f)
        {
            return;
        }

        var breathe = 0.5f + 0.5f * MathF.Sin(_urgencyPhase * MathF.PI * 2f);
        var warmth = _urgency * (0.35f + 0.65f * breathe);
        var pip = TriggerColor(ChestSlot.Trigger?.Type ?? MedicalTriggerType.Immediately);
        DrawHalo(context, glow, center, cell * (1.2f + 0.4f * warmth), pip * (0.32f * warmth));
        if (_urgency > 0.85f)
        {
            DrawHalo(context, glow, center, cell * (1.55f + 0.25f * breathe), pip * (0.18f * warmth));
        }
    }

    private void DrawSparks(RenderContext context)
    {
        if (_sparks.Count == 0)
        {
            return;
        }

        var glow = EnsureGlowTexture();
        var bounds = ActualBounds;
        var origin = new Vector2(bounds.X + bounds.Width * 0.5f, bounds.Y + bounds.Height * 0.5f);
        foreach (var spark in _sparks)
        {
            var pos = origin + spark.Offset;
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

    private static void DrawHalo(RenderContext context, Texture2D glow, Vector2 center, float diameter, Color color)
    {
        var size = Math.Max(8, (int)diameter);
        context.Draw(glow, new Rectangle(
            (int)center.X - size / 2,
            (int)center.Y - size / 2,
            size,
            size), color);
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

    private static string ChargeText(MedicalChestSlot slot)
    {
        if (slot.IsInfinite)
        {
            return MedicalChest.IsLockedForRestOfCombat(slot) ? "" : "∞";
        }

        return slot.Charges > 0 ? slot.Charges.ToString() : "";
    }

    private sealed class SlotSpark
    {
        public Vector2 Offset;
        public Vector2 Velocity;
        public float Life;
        public float MaxLife;
        public int Size;
        public Color Color;
        public float Gravity;
    }
}
