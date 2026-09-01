namespace Wendlemire.Scenes.MainGameScene.Gui.Widgets.CombatWidgets;

/// <summary>
/// Combat Blood / Body meter. Blood uses a proc-gen rain of droplets and
/// floor splatters that ramps in density and speed as blood falls.
/// </summary>
public sealed class CombatVitalMeter : Panel, IUpdatable
{
    public enum VitalKind
    {
        Blood,
        Body
    }

    private readonly VitalKind _kind;
    private readonly List<RainDrop> _drops = [];
    private readonly List<Splash> _splashes = [];
    private float _spawnTimer;
    private static Texture2D? _glowTexture;
    private static Texture2D? _pixelTexture;

    public Label ValueLabel { get; }
    public HorizontalProgressBar Bar { get; }
    public float Fill { get; set; } = 1f;

    public CombatVitalMeter(VitalKind kind, string name, int height)
    {
        _kind = kind;
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
            Height = 8,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Value = 100
        };

        Widgets.Add(new VerticalStackPanel
        {
            Spacing = 1,
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

    }

    public void Update()
    {
        Tick(1f / 60f);
    }

    public void Tick(float deltaTime)
    {
        if (_kind == VitalKind.Blood)
        {
            TickBlood(deltaTime);
        }
    }

    public override void InternalRender(RenderContext context)
    {
        base.InternalRender(context);
        if (_kind == VitalKind.Blood && RainIntensity > 0f)
        {
            DrawBloodRain(context, CellBounds());
        }
    }

    private float RainIntensity
    {
        get
        {
            if (Fill >= 0.9f)
            {
                return 0f;
            }

            if (Fill <= 0.1f)
            {
                return 1f;
            }

            return (0.9f - Fill) / 0.8f;
        }
    }

    private Rectangle CellBounds()
    {
        var outer = ActualBounds;
        return new Rectangle(outer.X + 1, outer.Y + 1, Math.Max(0, outer.Width - 2), Math.Max(0, outer.Height - 2));
    }

    private void TickBlood(float deltaTime)
    {
        var intensity = RainIntensity;
        if (intensity <= 0f)
        {
            _spawnTimer = 0f;
            _drops.Clear();
            _splashes.Clear();
            return;
        }

        var density = intensity * intensity;
        var spawnRate = 1.2f + density * 22f;
        var maxDrops = 1 + (int)(density * 20f);
        _spawnTimer += deltaTime;
        var interval = 1f / spawnRate;
        while (_spawnTimer >= interval && _drops.Count < maxDrops)
        {
            _spawnTimer -= interval;
            SpawnDrop(intensity);
        }

        AdvanceDrops(deltaTime, intensity);

    }

    private void AdvanceDrops(float deltaTime, float intensity)
    {
        for (var i = _drops.Count - 1; i >= 0; i--)
        {
            var drop = _drops[i];
            drop.Y += drop.Speed * (0.35f + intensity * 1.4f) * deltaTime;
            drop.X += MathF.Sin(drop.Wobble + drop.Y * 8f) * drop.Drift * deltaTime;
            if (drop.Y < 1.02f)
            {
                continue;
            }

            if (intensity > 0f)
            {
                SpawnSplash(drop, intensity);
            }

            _drops.RemoveAt(i);
        }

        for (var i = _splashes.Count - 1; i >= 0; i--)
        {
            var splash = _splashes[i];
            splash.Life -= deltaTime;
            splash.X += splash.VX * deltaTime;
            splash.Y += splash.VY * deltaTime;
            splash.VY += 3.8f * deltaTime;
            if (splash.Life <= 0f || splash.Y > 1.2f)
            {
                _splashes.RemoveAt(i);
            }
        }
    }

    private void SpawnDrop(float strain)
    {
        var glob = Rng.Visual.NextDouble() < 0.12 + strain * 0.12;
        _drops.Add(new RainDrop
        {
            X = 0.02f + (float)Rng.Visual.NextDouble() * 0.96f,
            Y = -0.12f - (float)Rng.Visual.NextDouble() * 0.2f,
            Speed = (glob ? 0.28f : 0.4f) + (float)Rng.Visual.NextDouble() * (0.18f + strain * 0.35f),
            Length = glob ? 4f + (float)Rng.Visual.NextDouble() * 3f : 2f + (float)Rng.Visual.NextDouble() * 3f + strain * 2f,
            Width = glob ? 2.4f : 1.2f + strain * 0.6f,
            Drift = 0.08f + (float)Rng.Visual.NextDouble() * 0.12f,
            Wobble = (float)Rng.Visual.NextDouble() * MathF.PI * 2f,
            IsGlob = glob,
            Color = glob
                ? Color.Lerp(new Color(160, 12, 18), new Color(210, 30, 32), (float)Rng.Visual.NextDouble())
                : Color.Lerp(new Color(110, 8, 14), new Color(180, 20, 24), (float)Rng.Visual.NextDouble())
        });
    }

    private void SpawnSplash(RainDrop drop, float strain)
    {
        var count = drop.IsGlob ? 4 + (int)(strain * 3) : 2 + (int)(strain * 2);
        for (var i = 0; i < count; i++)
        {
            var angle = -0.2f + (float)Rng.Visual.NextDouble() * (MathF.PI + 0.4f);
            var speed = 0.35f + (float)Rng.Visual.NextDouble() * (0.55f + strain * 0.4f);
            _splashes.Add(new Splash
            {
                X = Math.Clamp(drop.X + ((float)Rng.Visual.NextDouble() - 0.5f) * 0.06f, 0.02f, 0.98f),
                Y = 0.92f,
                VX = MathF.Cos(angle) * speed,
                VY = -MathF.Abs(MathF.Sin(angle)) * speed,
                Life = 0.18f + (float)Rng.Visual.NextDouble() * 0.22f,
                MaxLife = 0.35f,
                Size = drop.IsGlob ? 2.4f : 1.6f,
                Color = drop.Color
            });
        }
    }

    private void DrawBloodRain(RenderContext context, Rectangle bounds)
    {
        if (bounds.Width <= 2 || bounds.Height <= 2)
        {
            return;
        }

        var intensity = RainIntensity;
        var pixel = PixelTexture();
        var glow = GlowTexture();
        foreach (var drop in _drops)
        {
            var x = bounds.X + (int)(drop.X * bounds.Width);
            var y = bounds.Y + (int)(drop.Y * bounds.Height);
            var length = Math.Max(3, (int)drop.Length);
            var width = Math.Max(1, (int)drop.Width);
            context.Draw(pixel, new Rectangle(x, y, width, length), drop.Color * (0.82f + intensity * 0.15f));
            if (drop.IsGlob)
            {
                var size = 5 + (int)(intensity * 2);
                context.Draw(glow, new Rectangle(x - size / 2, y + length - 2, size, size), drop.Color * 0.6f);
            }
        }

        foreach (var splash in _splashes)
        {
            var fade = Math.Clamp(splash.Life / splash.MaxLife, 0f, 1f);
            var x = bounds.X + (int)(splash.X * bounds.Width);
            var y = bounds.Y + (int)(splash.Y * bounds.Height);
            var size = Math.Max(2, (int)(splash.Size * (0.7f + fade)));
            context.Draw(glow, new Rectangle(x - size / 2, y - size / 2, size, size), splash.Color * (0.8f * fade));
        }
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

    private sealed class RainDrop
    {
        public float X;
        public float Y;
        public float Speed;
        public float Length;
        public float Width;
        public float Drift;
        public float Wobble;
        public bool IsGlob;
        public Color Color;
    }

    private sealed class Splash
    {
        public float X;
        public float Y;
        public float VX;
        public float VY;
        public float Life;
        public float MaxLife;
        public float Size;
        public Color Color;
    }

}
