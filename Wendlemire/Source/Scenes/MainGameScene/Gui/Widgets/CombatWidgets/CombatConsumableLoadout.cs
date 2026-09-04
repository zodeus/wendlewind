using Wendlemire.Scenes.MainGameScene.Gui.CombatGui;
using Wendlemire.Scenes.MainGameScene.Gui.Widgets.EntityWidgets;
using Wendlemire.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets.PawnPreparationPanelWidgets;

namespace Wendlemire.Scenes.MainGameScene.Gui.Widgets.CombatWidgets;

internal sealed class CombatConsumableLoadout : Panel, IUpdatable
{
    private const int ColumnWidth = 180;
    private const int CellSpacing = 5;
    private const int CellSize = (ColumnWidth - CellSpacing * 2) / 3;
    private const int CellPad = 3;
    private const int IconSize = CellSize - CellPad * 2;
    private const int IncenseSlotSpacing = 2;
    private const int IncenseFuseWidth = 12;
    private const int IncensePanelHeight = CellSize * 2 + CellSpacing;
    private const int IncenseMiniSize =
        (IncensePanelHeight - IncenseSlotSpacing * (IncenseProperties.MaxActive - 1))
        / IncenseProperties.MaxActive;
    private static readonly Color SpentTint = new(70, 70, 68);
    private static readonly Color SpentDim = new(8, 8, 8, 150);

    public readonly Pawn Pawn;
    private readonly BaseGui _gui;
    private readonly MedicalBar _medicalBar;
    private readonly VerticalStackPanel _stack;
    private readonly Panel[] _foodSlots = new Panel[MealPlan.MaxSlots];
    private readonly IncenseSlotView[] _incenseSlots = new IncenseSlotView[IncenseProperties.MaxActive];
    private Widget _strip = null!;
    private string _foodSignature = "";

    public CombatConsumableLoadout(BaseGui gui, Pawn pawn)
    {
        Pawn = pawn;
        _gui = gui;
        Width = ColumnWidth;
        ClipToBounds = false;
        HorizontalAlignment = HorizontalAlignment.Stretch;

        _medicalBar = new MedicalBar(gui, pawn, def => ViewMedical(def), CellSize, IconSize, CellPad);
        _stack = new VerticalStackPanel
        {
            Spacing = CellSpacing,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Top,
            ClipToBounds = false
        };
        _strip = BuildStrip(pawn);
        _stack.Widgets.Add(_strip);
        _stack.Widgets.Add(_medicalBar);
        Widgets.Add(_stack);
        _foodSignature = FoodSignature();
    }

    public Widget? NotifyMedicalUsed(string? itemMoniker)
    {
        return _medicalBar.NotifyUsed(itemMoniker);
    }

    public bool TryGetMedicalSlot(string? itemMoniker, out Widget slot)
    {
        return _medicalBar.TryGetSlot(itemMoniker, out slot);
    }

    public bool TryGetIncenseSlot(int index, out Widget slot)
    {
        if ((uint)index >= _incenseSlots.Length || _incenseSlots[index]?.Cell == null)
        {
            slot = null!;
            return false;
        }

        slot = _incenseSlots[index].Cell;
        return true;
    }

    public void Update()
    {
        _medicalBar.Update();
        var signature = FoodSignature();
        if (signature != _foodSignature)
        {
            _foodSignature = signature;
            RebuildStrip();
        }

        var tick = Core.Context.CurrentZone?.ActiveEncounter?.Ticks
            ?? Pawn.Zone?.ActiveEncounter?.Ticks
            ?? 0;
        foreach (var slot in _incenseSlots)
        {
            if (slot == null)
            {
                continue;
            }

            slot.Fuse.Update(tick, slot.SlotIndex, slot.Incense, slot.Incense != null && IsBurning(slot.Incense));
            if (slot.Incense == null || slot.Burn == null)
            {
                continue;
            }

            var burning = IsBurning(slot.Incense);
            var spent = slot.Incense.FiredThisEncounter && !burning;
            slot.Burn.Burning = burning;
            slot.Burn.Update();
            if (slot.Tint != null)
            {
                slot.Tint.Color = spent ? SpentTint : Color.White;
            }

            if (slot.Dim != null)
            {
                slot.Dim.Visible = spent;
                slot.Dim.Background = spent ? new SolidBrush(SpentDim) : null;
            }
        }
    }

    public bool IsBurning(ActiveIncense incense)
    {
        return incense.FiredThisEncounter
            && incense.Def != null
            && Pawn.Body.Effects.Has(incense.Def);
    }

    private Widget BuildStrip(Pawn pawn)
    {
        var foods = DisplayedFoodDefs();
        var foodCap = Math.Clamp(pawn.MealPlan.Capacity, 0, MealPlan.MaxSlots);

        var grid = new Grid
        {
            ColumnSpacing = CellSpacing,
            RowSpacing = CellSpacing,
            ClipToBounds = false,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Top
        };
        for (var i = 0; i < 3; i++)
        {
            grid.ColumnsProportions.Add(new Proportion(ProportionType.Pixels, CellSize));
        }

        for (var i = 0; i < 2; i++)
        {
            grid.RowsProportions.Add(new Proportion(ProportionType.Pixels, CellSize));
        }

        int[] foodRows = [0, 0, 1, 1];
        int[] foodCols = [0, 1, 0, 1];
        for (var i = 0; i < MealPlan.MaxSlots; i++)
        {
            Panel cell;
            if (i < foods.Count)
            {
                cell = Cell(FoodIcon(foods[i]));
            }
            else if (i < foodCap)
            {
                cell = GhostCell("Unused meal slot", "This slot is empty and will not feed in the fight.");
            }
            else
            {
                var tip = SlotUnlockTooltip.ForSlot(PrepSlotKind.Food, i + 1);
                cell = LockedCell(tip.title, tip.description);
            }

            _foodSlots[i] = cell;
            Place(grid, cell, foodRows[i], foodCols[i]);
        }

        var incensePanel = BuildIncensePanel(pawn);
        Grid.SetRow(incensePanel, 0);
        Grid.SetColumn(incensePanel, 2);
        Grid.SetRowSpan(incensePanel, 2);
        grid.Widgets.Add(incensePanel);

        return grid;
    }

    private Widget BuildIncensePanel(Pawn pawn)
    {
        var incense = pawn.ActiveIncense;
        var incenseCap = Math.Clamp(pawn.IncenseCapacity, 0, IncenseProperties.MaxActive);
        var stack = new VerticalStackPanel
        {
            Spacing = IncenseSlotSpacing,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        for (var i = 0; i < IncenseProperties.MaxActive; i++)
        {
            var view = new IncenseSlotView
            {
                SlotIndex = i,
                Fuse = new IncenseFuse()
            };
            Widget icon;
            if (i < incense.Count)
            {
                view.Incense = incense[i];
                view.Cell = MiniCell(CreateIncenseIcon(incense[i], view), clip: false);
                icon = view.Cell;
            }
            else if (i < incenseCap)
            {
                icon = MiniGhost("Unused incense slot", "This slot is empty and will not burn in the fight.");
            }
            else
            {
                var tip = SlotUnlockTooltip.ForSlot(PrepSlotKind.Incense, i + 1);
                icon = MiniLocked(tip.title, tip.description);
            }

            _incenseSlots[i] = view;
            stack.Widgets.Add(IncenseRow(view.Fuse, icon));
        }

        return new Panel
        {
            Width = CellSize,
            Height = IncensePanelHeight,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            ClipToBounds = false,
            Widgets = { stack }
        };
    }

    private void ViewMedical(ItemDef def)
    {
        var live = Pawn.Inventory.FirstOrDefault(i => i.Def == def && !i.IsDestroyed);
        if (live != null)
        {
            _gui.ViewEntity(live);
            return;
        }

        _gui.ViewEntity(Pawn.Context.Factory.CreateEntity<Item>(def, 1));
    }

    private static Widget IncenseRow(Widget fuse, Widget icon)
    {
        return new HorizontalStackPanel
        {
            Spacing = 2,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Widgets = { fuse, icon }
        };
    }

    private static void Place(Grid grid, Widget cell, int row, int column)
    {
        Grid.SetRow(cell, row);
        Grid.SetColumn(cell, column);
        grid.Widgets.Add(cell);
    }

    private Widget CreateIncenseIcon(ActiveIncense incense, IncenseSlotView view)
    {
        var itemDef = incense.SourceMoniker != null
            ? DefRepository<ItemDef>.GetByMoniker(incense.SourceMoniker, raiseError: false)
            : null;
        var icon = new Image
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };
        if (itemDef != null)
        {
            view.Tint = new ColoredIcon(itemDef.GetIconImage(), Color.White);
            icon.Background = view.Tint;
        }
        else if (incense.Def != null)
        {
            view.Tint = new ColoredIcon(new TextureRegion(incense.Def.GetTexture()), Color.White);
            icon.Background = view.Tint;
        }
        view.Dim = new Panel
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Background = new SolidBrush(Color.Transparent),
            Visible = false
        };

        view.Burn = new IncenseSlotBurnFx
        {
            Tint = CombatIncenseSmokeFx.TintFor(incense)
        };

        var stack = new Panel
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            ClipToBounds = false,
            Widgets = { icon, view.Dim, view.Burn }
        };
        var name = incense.Def?.Label ?? itemDef?.Label ?? "Incense";
        stack.WithDynamicTooltip(() => name, () => IncenseStatusText(view));
        return stack;
    }

    private string IncenseStatusText(IncenseSlotView view)
    {
        var incense = view.Incense;
        if (incense == null)
        {
            return $"Lights at {IncenseProperties.GetIgniteTick(view.SlotIndex)}";
        }

        if (IsBurning(incense))
        {
            return "Burning";
        }

        if (incense.FiredThisEncounter)
        {
            return "Burned out";
        }

        return $"Lights at {IncenseProperties.GetIgniteTick(view.SlotIndex)}";
    }

    private void RebuildStrip()
    {
        _strip.RemoveFromParent();

        Array.Clear(_foodSlots);
        Array.Clear(_incenseSlots);
        _strip = BuildStrip(Pawn);
        _stack.Widgets.Insert(0, _strip);
    }

    private List<ItemDef> DisplayedFoodDefs()
    {
        if (Pawn.CombatStomach.Items.Count > 0)
        {
            return Pawn.CombatStomach.Items
                .Where(f => f.Def != null)
                .Select(f => f.Def)
                .ToList();
        }

        return Pawn.MealPlan.Items
            .Where(i => i != null)
            .Select(i => i.ItemDef)
            .ToList();
    }

    private string FoodSignature()
    {
        var defs = DisplayedFoodDefs();
        return string.Join(",", defs.Select(d => d.Moniker));
    }

    private Widget FoodTooltip(ItemDef def)
    {
        var container = new VerticalStackPanel { Spacing = 4, Padding = new Thickness(4) };
        container.Widgets.Add(new Label(BaseContent.Styles.Label.Normal)
        {
            Text = def.Label,
            TextColor = Color.Gold
        });

        var description = def.Description;
        if (!string.IsNullOrWhiteSpace(description))
        {
            container.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
            {
                Text = description,
                Wrap = true,
                MaxWidth = 400,
                TextColor = new Color(200, 200, 200)
            });
        }

        var nutrition = def.BaseStats.FirstOrDefault(s => s.Def == Defs.Stats.NutritionalValue)?.Value ?? 0f;
        container.Widgets.Add(new HorizontalStackPanel
        {
            Spacing = 6,
            Widgets =
            {
                new Label(BaseContent.Styles.Label.Small) { Text = "Nutrition:", TextColor = new Color(180, 180, 180) },
                new Label(BaseContent.Styles.Label.Small)
                {
                    Text = $"{nutrition:0.##}",
                    TextColor = FoodProperties.GetNutritionColor(nutrition)
                }
            }
        });

        var effects = def.FoodProperties?.Effects;
        if (effects is { Count: > 0 })
        {
            container.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
            {
                Text = "Effects:",
                TextColor = new Color(220, 180, 100),
                Margin = new Thickness(0, 4, 0, 2)
            });

            foreach (var effect in effects)
            {
                var row = new HorizontalStackPanel
                {
                    Spacing = 6,
                    Margin = new Thickness(8, 0, 0, 0)
                };
                row.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
                {
                    Text = effect.Def.Label,
                    TextColor = FoodProperties.GetEffectColor(effect.Def)
                });

                container.Widgets.Add(row);
                FoodPanel.AddAffectedStatRows(container, effect.Def.AffectedStats);
            }
        }

        return container;
    }

    private CursorButton FoodIcon(ItemDef def)
    {
        var icon = new CursorButton
        {
            Width = IconSize,
            Height = IconSize,
            Padding = new Thickness(0),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Content = new Image
            {
                Background = def.GetIconImage(),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            }
        };

        var live = Pawn.Inventory.FirstOrDefault(i => i.Def == def && !i.IsDestroyed);
        if (live != null)
        {
            icon.TouchDown += (_, _) => _gui.ViewEntity(live);
        }

        icon.WithTooltip(() => FoodTooltip(def));
        return icon;
    }

    private static Panel Cell(Widget content, bool clip = true)
    {
        return new Panel
        {
            Width = CellSize,
            Height = CellSize,
            ClipToBounds = clip,
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.IconFrame],
            Padding = new Thickness(CellPad),
            Widgets = { content }
        };
    }

    private static Panel GhostCell(string? title = null, string? description = null)
    {
        var cell = Cell(new Panel());
        cell.Opacity = 0.7f;
        if (!string.IsNullOrEmpty(title))
        {
            cell.WithTooltip(title, description);
        }

        return cell;
    }

    private static Panel LockedCell(string title, string? description)
    {
        var icon = LockedSlotChrome.Icon(Math.Max(12, IconSize - 8));
        var cell = Cell(icon);
        cell.Opacity = 0.7f;
        cell.WithTooltip(title, description);
        return cell;
    }

    private static Panel MiniCell(Widget content, bool clip = true)
    {
        return new Panel
        {
            Width = IncenseMiniSize,
            Height = IncenseMiniSize,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top,
            ClipToBounds = clip,
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.IconFrame],
            Padding = new Thickness(1),
            Widgets = { content }
        };
    }

    private static Panel MiniGhost(string title, string? description)
    {
        var cell = MiniCell(new Panel());
        cell.Opacity = 0.7f;
        cell.WithTooltip(title, description);
        return cell;
    }

    private static Panel MiniLocked(string title, string? description)
    {
        var icon = LockedSlotChrome.Icon(14);
        var cell = MiniCell(icon);
        cell.Opacity = 0.7f;
        cell.WithTooltip(title, description);
        return cell;
    }

    private sealed class IncenseSlotView
    {
        public Panel Cell = null!;
        public IncenseFuse Fuse = null!;
        public ColoredIcon? Tint;
        public Panel? Dim;
        public IncenseSlotBurnFx? Burn;
        public ActiveIncense? Incense;
        public int SlotIndex;
    }

    private sealed class IncenseFuse : Widget
    {
        private const float WickSpawn = 0.045f;
        private const float TipSpawn = 0.03f;
        private static readonly Color Wick = new(220, 170, 80);
        private static readonly Color Ember = new(255, 130, 45);
        private static readonly Color Hot = new(255, 220, 160);

        private readonly List<FuseSpark> _sparks = [];
        private float _phase;
        private float _wickTimer;
        private float _tipTimer;
        private float _remaining = 1f;
        private bool _burning;
        private bool _lit;
        private static Texture2D? _glow;

        public IncenseFuse()
        {
            Width = IncenseFuseWidth;
            Height = IncenseMiniSize;
            HorizontalAlignment = HorizontalAlignment.Center;
            VerticalAlignment = VerticalAlignment.Center;
            ClipToBounds = false;
        }

        public override Widget? HitTest(Point p) => null;

        public void Update(int tick, int slotIndex, ActiveIncense? incense, bool burning)
        {
            const float dt = 1f / 60f;
            _phase += dt;
            _burning = burning;
            _lit = incense != null && !incense.FiredThisEncounter;
            if (incense == null)
            {
                _remaining = 0f;
            }
            else if (burning)
            {
                _remaining = 1f;
            }
            else if (incense.FiredThisEncounter)
            {
                _remaining = 0f;
            }
            else
            {
                var ignite = IncenseProperties.GetIgniteTick(slotIndex);
                _remaining = ignite <= 0 ? 0f : Math.Clamp(1f - tick / (float)ignite, 0f, 1f);
            }

            if (_lit || _burning)
            {
                _wickTimer -= dt;
                var wickRate = _burning ? WickSpawn * 0.45f : WickSpawn;
                while (_wickTimer <= 0)
                {
                    _wickTimer += wickRate;
                    SpawnWick();
                }

                _tipTimer -= dt;
                var tipRate = _burning || _remaining < 0.22f ? TipSpawn * 0.55f : TipSpawn;
                while (_tipTimer <= 0)
                {
                    _tipTimer += tipRate;
                    SpawnTip();
                }
            }

            for (var i = _sparks.Count - 1; i >= 0; i--)
            {
                var spark = _sparks[i];
                spark.Life -= dt;
                if (spark.Life <= 0)
                {
                    _sparks.RemoveAt(i);
                    continue;
                }

                spark.VX *= 1f - 1.8f * dt;
                spark.VY += spark.Gravity * dt;
                spark.X += spark.VX * dt;
                spark.Y += spark.VY * dt;
                spark.Size += spark.Grow * dt;
            }
        }

        public override void InternalRender(RenderContext context)
        {
            var bounds = ActualBounds;
            if (bounds.Width <= 0 || bounds.Height <= 0)
            {
                return;
            }

            var glow = Glow();
            var tipY = bounds.Y + bounds.Height - Math.Max(3, (int)(bounds.Height * _remaining));
            if ((_lit || _burning) && _remaining > 0.02f)
            {
                var pulse = 0.65f + 0.35f * MathF.Sin(_phase * (_burning ? 10f : 5f));
                var tipColor = _burning || _remaining < 0.2f ? Ember : Wick;
                var tipSize = (int)((_burning ? 11 : 8) + 3 * pulse);
                context.Draw(glow, new Rectangle(
                    bounds.X + bounds.Width / 2 - tipSize / 2,
                    tipY - tipSize / 2,
                    tipSize,
                    tipSize), tipColor * (0.55f * pulse));
            }

            foreach (var spark in _sparks)
            {
                var t = Math.Clamp(spark.Life / spark.MaxLife, 0f, 1f);
                var fade = t < 0.2f ? t / 0.2f : t;
                var size = Math.Max(3, (int)spark.Size);
                var x = bounds.X + (int)spark.X;
                var y = bounds.Y + (int)spark.Y;
                context.Draw(glow, new Rectangle(x - size / 2, y - size / 2, size, size),
                    spark.Color * (0.85f * fade));
            }
        }

        private void SpawnWick()
        {
            var height = (float)IncenseMiniSize;
            var wickTop = height * (1f - _remaining);
            var y = wickTop + Rng.Visual.Next(0, Math.Max(1, (int)(height * _remaining + 1)));
            _sparks.Add(new FuseSpark
            {
                X = IncenseFuseWidth * 0.5f + Rng.Visual.Next(-3, 4),
                Y = y,
                VX = -8f + Rng.Visual.Next(0, 16),
                VY = _burning ? -10f - Rng.Visual.Next(0, 12) : -4f - Rng.Visual.Next(0, 8),
                Life = 0.22f + Rng.Visual.Next(0, 18) / 100f,
                MaxLife = 0.4f,
                Size = 2.5f + Rng.Visual.Next(0, 3),
                Grow = -4f,
                Gravity = _burning ? -18f : 6f,
                Color = _burning
                    ? Color.Lerp(Ember, Hot, Rng.Visual.Next(0, 50) / 100f)
                    : Color.Lerp(Wick, Hot, Rng.Visual.Next(0, 40) / 100f)
            });
        }

        private void SpawnTip()
        {
            var height = (float)IncenseMiniSize;
            var y = height * (1f - _remaining);
            _sparks.Add(new FuseSpark
            {
                X = IncenseFuseWidth * 0.5f + Rng.Visual.Next(-2, 3),
                Y = y + Rng.Visual.Next(-2, 3),
                VX = -14f + Rng.Visual.Next(0, 28),
                VY = -16f - Rng.Visual.Next(0, 18),
                Life = 0.28f + Rng.Visual.Next(0, 20) / 100f,
                MaxLife = 0.5f,
                Size = 3.5f + Rng.Visual.Next(0, 4),
                Grow = -3f,
                Gravity = -24f,
                Color = _remaining < 0.22f || _burning ? Ember : Hot
            });
        }

        private static Texture2D Glow()
        {
            if (_glow != null)
            {
                return _glow;
            }

            const int size = 64;
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

            _glow = new Texture2D(Core.GraphicsDevice, size, size);
            _glow.SetData(data);
            return _glow;
        }

        private sealed class FuseSpark
        {
            public float X;
            public float Y;
            public float VX;
            public float VY;
            public float Life;
            public float MaxLife;
            public float Size;
            public float Grow;
            public float Gravity;
            public Color Color;
        }
    }
}
