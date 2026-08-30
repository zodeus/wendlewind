namespace Wendlewind.Scenes.MainGameScene.Gui.Widgets.CombatWidgets;

/// <summary>
/// Combat Blood / Body meter with a living background: circulating corpuscles
/// for blood, a heartbeat pulse and muscle fibers for body.
/// </summary>
public sealed class CombatVitalMeter : Panel, IUpdatable
{
    public enum VitalKind
    {
        Blood,
        Body
    }

    private readonly VitalKind _kind;
    private readonly List<BloodCell> _cells = [];
    private readonly List<Fiber> _fibers = [];
    private readonly List<Speck> _specks = [];
    private readonly float _phase;
    private static Texture2D? _glowTexture;
    private static Texture2D? _pixelTexture;

    public Label ValueLabel { get; }
    public HorizontalProgressBar Bar { get; }
    public float Fill { get; set; } = 1f;

    public CombatVitalMeter(VitalKind kind, string name, int height)
    {
        _kind = kind;
        _phase = kind == VitalKind.Blood ? 0.3f : 1.1f;
        ClipToBounds = true;
        Height = height;
        HorizontalAlignment = HorizontalAlignment.Stretch;
        Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.IconFrame];
        Padding = new Thickness(4, 2);

        ValueLabel = new Label(BaseContent.Styles.Label.Small)
        {
            TextColor = new Color(200, 180, 120),
            HorizontalAlignment = HorizontalAlignment.Center
        };
        Bar = new HorizontalProgressBar(BaseContent.Styles.Bar.Health)
        {
            Height = 12,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Value = 100
        };

        Widgets.Add(new VerticalStackPanel
        {
            Spacing = 3,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Center,
            Widgets =
            {
                new HorizontalStackPanel
                {
                    Spacing = 6,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Widgets =
                    {
                        new Label(BaseContent.Styles.Label.Small)
                        {
                            Text = name,
                            TextColor = new Color(170, 165, 155),
                            HorizontalAlignment = HorizontalAlignment.Center
                        },
                        ValueLabel
                    }
                },
                Bar
            }
        });

        SeedBackdrop();
    }

    public void Update()
    {
        Tick(1f / 60f);
    }

    public void Tick(float deltaTime)
    {
        var strain = Strain;
        if (_kind == VitalKind.Blood)
        {
            var speed = 18f + strain * 36f;
            foreach (var cell in _cells)
            {
                cell.X += cell.Speed * speed * deltaTime;
                if (cell.X > 1.15f)
                {
                    cell.X = -0.15f;
                    cell.Y = 0.12f + (float)Rng.Visual.NextDouble() * 0.76f;
                }

                cell.BobPhase += deltaTime * (1.6f + strain * 2.4f);
            }

            return;
        }

        foreach (var fiber in _fibers)
        {
            fiber.Phase += deltaTime * (1.2f + strain * 1.8f);
        }

        foreach (var speck in _specks)
        {
            speck.X += speck.Drift * deltaTime;
            if (speck.X > 1.05f)
            {
                speck.X = -0.05f;
            }
            else if (speck.X < -0.05f)
            {
                speck.X = 1.05f;
            }
        }
    }

    public override void InternalRender(RenderContext context)
    {
        var background = Background;
        Background = null;
        background?.Draw(context, ActualBounds, Color.White);
        DrawBackdrop(context);
        base.InternalRender(context);
        Background = background;
    }

    private float Strain => Math.Clamp((0.85f - Fill) / 0.85f, 0f, 1f);

    private void SeedBackdrop()
    {
        if (_kind == VitalKind.Blood)
        {
            for (var i = 0; i < 8; i++)
            {
                _cells.Add(new BloodCell
                {
                    X = (float)Rng.Visual.NextDouble(),
                    Y = 0.12f + (float)Rng.Visual.NextDouble() * 0.76f,
                    Width = 7f + (float)Rng.Visual.NextDouble() * 6f,
                    Height = 4f + (float)Rng.Visual.NextDouble() * 3f,
                    Speed = 0.35f + (float)Rng.Visual.NextDouble() * 0.55f,
                    BobPhase = (float)Rng.Visual.NextDouble() * MathF.PI * 2f
                });
            }

            return;
        }

        for (var i = 0; i < 5; i++)
        {
            _fibers.Add(new Fiber
            {
                Y = 0.18f + i * 0.16f,
                Thickness = 1 + i % 2,
                Phase = (float)Rng.Visual.NextDouble() * MathF.PI * 2f
            });
        }

        for (var i = 0; i < 10; i++)
        {
            _specks.Add(new Speck
            {
                X = (float)Rng.Visual.NextDouble(),
                Y = 0.1f + (float)Rng.Visual.NextDouble() * 0.8f,
                Drift = ((float)Rng.Visual.NextDouble() - 0.5f) * 0.08f,
                Size = 1.5f + (float)Rng.Visual.NextDouble() * 2f
            });
        }
    }

    private void DrawBackdrop(RenderContext context)
    {
        var outer = ActualBounds;
        var bounds = new Rectangle(outer.X + 2, outer.Y + 2, outer.Width - 4, outer.Height - 4);
        if (bounds.Width <= 2 || bounds.Height <= 2)
        {
            return;
        }

        if (_kind == VitalKind.Blood)
        {
            DrawBloodBackdrop(context, bounds);
            return;
        }

        DrawBodyBackdrop(context, bounds);
    }

    private void DrawBloodBackdrop(RenderContext context, Rectangle bounds)
    {
        var strain = Strain;
        var time = Core.TotalTime + _phase;
        var pulse = 0.07f + 0.05f * (0.5f + 0.5f * MathF.Sin(time * 2.4f));
        var pixel = PixelTexture();
        var healthy = new Color(70, 10, 14);
        var wounded = new Color(130, 12, 16);
        context.Draw(pixel, bounds, Color.Lerp(healthy, wounded, strain) * (0.16f + pulse + strain * 0.18f));

        var glow = GlowTexture();
        foreach (var cell in _cells)
        {
            var bob = MathF.Sin(cell.BobPhase) * (1.4f + strain * 1.2f);
            var x = bounds.X + (int)(cell.X * bounds.Width);
            var y = bounds.Y + (int)(cell.Y * bounds.Height + bob);
            var w = Math.Max(4, (int)cell.Width);
            var h = Math.Max(3, (int)cell.Height);
            var anemia = Color.Lerp(new Color(150, 18, 24), new Color(90, 16, 28), strain);
            var highlight = Color.Lerp(new Color(210, 50, 48), new Color(160, 40, 50), strain);
            var fade = 0.28f + 0.22f * (1f - strain * 0.35f);
            context.Draw(glow, new Rectangle(x - w / 2, y - h / 2, w, h), anemia * fade);
            context.Draw(glow, new Rectangle(x - w / 4, y - h / 3, Math.Max(2, w / 2), Math.Max(2, h / 2)), highlight * (fade * 0.45f));
        }
    }

    private void DrawBodyBackdrop(RenderContext context, Rectangle bounds)
    {
        var strain = Strain;
        var time = Core.TotalTime + _phase;
        var beat = Heartbeat(time, 1.15f + strain * 0.9f);
        var pixel = PixelTexture();
        var healthy = new Color(90, 42, 32);
        var wounded = new Color(120, 28, 22);
        context.Draw(pixel, bounds, Color.Lerp(healthy, wounded, strain) * (0.12f + beat * 0.16f + strain * 0.1f));

        var glow = GlowTexture();
        var thump = (int)(2 + beat * (4f + strain * 3f));
        context.Draw(glow, new Rectangle(
            bounds.X + bounds.Width / 2 - (bounds.Width + thump) / 2,
            bounds.Y + bounds.Height / 2 - (bounds.Height + thump) / 2,
            bounds.Width + thump,
            bounds.Height + thump), Color.Lerp(new Color(200, 80, 55), new Color(180, 40, 30), strain) * (0.08f + beat * 0.2f));

        foreach (var fiber in _fibers)
        {
            var shimmer = 0.08f + 0.1f * (0.5f + 0.5f * MathF.Sin(time * 2.8f + fiber.Phase));
            var y = bounds.Y + (int)(fiber.Y * bounds.Height);
            context.Draw(pixel, new Rectangle(bounds.X + 3, y, bounds.Width - 6, fiber.Thickness),
                Color.Lerp(new Color(170, 90, 60), new Color(140, 50, 40), strain) * (shimmer + beat * 0.08f));
        }

        foreach (var speck in _specks)
        {
            var x = bounds.X + (int)(speck.X * bounds.Width);
            var y = bounds.Y + (int)(speck.Y * bounds.Height);
            var size = Math.Max(2, (int)speck.Size);
            context.Draw(glow, new Rectangle(x, y, size, size),
                Color.Lerp(new Color(210, 130, 90), new Color(160, 60, 50), strain) * (0.16f + beat * 0.1f));
        }
    }

    private static float Heartbeat(float time, float bpmScale)
    {
        var cycle = time * bpmScale;
        var t = cycle - MathF.Floor(cycle);
        var lub = Pulse(t, 0.12f, 18f);
        var dub = Pulse(t, 0.28f, 16f);
        return Math.Max(lub, dub * 0.7f);
    }

    private static float Pulse(float t, float center, float sharpness)
    {
        var d = t - center;
        return MathF.Exp(-d * d * sharpness);
    }

    private static Texture2D GlowTexture()
    {
        if (_glowTexture != null)
        {
            return _glowTexture;
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

        _glowTexture = new Texture2D(Core.GraphicsDevice, size, size);
        _glowTexture.SetData(data);
        return _glowTexture;
    }

    private static Texture2D PixelTexture()
    {
        if (_pixelTexture != null)
        {
            return _pixelTexture;
        }

        _pixelTexture = new Texture2D(Core.GraphicsDevice, 1, 1);
        _pixelTexture.SetData([Color.White]);
        return _pixelTexture;
    }

    private sealed class BloodCell
    {
        public float X;
        public float Y;
        public float Width;
        public float Height;
        public float Speed;
        public float BobPhase;
    }

    private sealed class Fiber
    {
        public float Y;
        public int Thickness;
        public float Phase;
    }

    private sealed class Speck
    {
        public float X;
        public float Y;
        public float Drift;
        public float Size;
    }
}
