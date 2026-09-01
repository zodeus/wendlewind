using Wendlemire.Scenes.MainGameScene.Gui;
using Wendlemire.Scenes.MainGameScene.Gui.Widgets.EntityWidgets;
using Image = Myra.Graphics2D.UI.Image;

namespace Wendlemire.Scenes.MainGameScene.Gui.Widgets.CombatWidgets;

public sealed class MedicalBar : HorizontalStackPanel, IUpdatable
{
    private readonly Pawn _pawn;
    private readonly List<MedicalSlotView> _slots = [];

    public MedicalBar(BaseGui gui, Pawn pawn, Action<ItemDef>? clickHandler = null, int? iconSize = null)
    {
        _pawn = pawn;
        pawn.MedicalChest.Prune();
        var size = iconSize ?? BaseContent.IconSizes.Medium;
        foreach (var chestSlot in pawn.MedicalChest.Slots)
        {
            var view = new MedicalSlotView(gui, pawn, chestSlot, size, clickHandler);
            Widgets.Add(view);
            _slots.Add(view);
        }
    }

    public void NotifyUsed(string? itemMoniker)
    {
        foreach (var slot in _slots)
        {
            if (itemMoniker == null || slot.ChestSlot.Def.Moniker != itemMoniker)
            {
                continue;
            }

            slot.Flash();
            return;
        }
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
}

internal sealed class MedicalSlotView : Panel
{
    private const float GlowDuration = 1.45f;
    private static readonly Color HealColor = new(90, 210, 140);
    private static readonly Color CauterizeColor = new(255, 140, 50);

    private static readonly Color ReadyTint = Color.White;
    private static readonly Color CooldownTint = new(70, 70, 68);
    private static readonly Color EmptyTint = new(48, 48, 46);
    private static readonly Color TimerColor = new(220, 200, 150);

    private readonly BaseGui _gui;
    private readonly Pawn _pawn;
    private readonly CursorButton _button;
    private readonly ColoredIcon _tint;
    private readonly Panel _dim;
    private readonly Label _chargeLabel;
    private readonly Panel _cooldownChip;
    private readonly Label _cooldownLabel;
    private readonly List<SlotSpark> _sparks = [];
    private static Texture2D? _glowTexture;
    private float _flashRemaining;
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
                Widgets = { icon, _dim, _chargeLabel, _cooldownChip }
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
            ShowTitle = true,
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
        var remainingTicks = MedicalChest.IsLockedForRestOfCombat(ChestSlot)
            ? 0
            : ChestSlot.NextReadyTick - tick;
        var cooling = remainingTicks > 0;
        var muted = empty || cooling;

        if (_flashRemaining > 0f)
        {
            var progress = Math.Clamp(1f - _flashRemaining / GlowDuration, 0f, 1f);
            var pulse = MathF.Sin(progress * MathF.PI);
            _tint.Color = Color.Lerp(muted ? CooldownTint : ReadyTint, FlashColor(), pulse);
            _dim.Visible = muted;
            _dim.Background = new SolidBrush(new Color(8, 8, 8, (int)(90 * (1f - pulse))));
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
            var seconds = remainingTicks / (float)GameContext.TicksPerSecond;
            _cooldownLabel.Text = seconds >= 10f ? $"{seconds:0}s" : $"{seconds:0.0}s";
            _cooldownLabel.TextColor = seconds < 1f ? FlashColor() : TimerColor;
            _cooldownChip.Visible = true;
            _cooldownChip.Border = new SolidBrush(seconds < 1f
                ? FlashColor() * 0.55f
                : new Color(200, 180, 120, 70));
        }
        else
        {
            _cooldownChip.Visible = false;
        }

        UpdateHoverInspect();
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
        if (_flashRemaining <= 0f)
        {
            return;
        }

        var progress = Math.Clamp(1f - _flashRemaining / GlowDuration, 0f, 1f);
        var pulse = MathF.Sin(progress * MathF.PI);
        if (pulse < 0.02f)
        {
            return;
        }

        var bounds = ActualBounds;
        var center = new Vector2(bounds.X + bounds.Width * 0.5f, bounds.Y + bounds.Height * 0.5f);
        var cell = Math.Max(bounds.Width, bounds.Height);
        var glow = EnsureGlowTexture();
        var color = FlashColor();
        DrawHalo(context, glow, center, cell * (1.85f + 0.45f * pulse), color * (0.55f * pulse));
        DrawHalo(context, glow, center, cell * (1.15f + 0.2f * pulse), Color.Lerp(color, Color.White, 0.25f) * (0.22f * pulse));
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
            return "∞";
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
