using SolidBrush = Myra.Graphics2D.Brushes.SolidBrush;

namespace Wendlewind.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets;

/// <summary>
/// A compact capabilities overlay for display within the pawn renderer.
/// Shows Sight, Breathing, Circulation, and Mobility as horizontal bars.
/// Failed (0%) capabilities render red with a bleeding / on-fire overlay.
/// Yellow (weak) capabilities use a milder amber variation of the same effect.
/// </summary>
public sealed class PawnCapabilitiesOverlay : Panel, IUpdatable
{
    private readonly PawnCapabilities _capabilities;
    private readonly Dictionary<string, CapabilityRow> _rows = new();
    private readonly Dictionary<Color, SolidBrush> _brushes = new();

    private static readonly Color ColorExcellent = new(120, 200, 80);
    private static readonly Color ColorGood = new(180, 200, 80);
    private static readonly Color ColorWeak = new(220, 160, 60);
    private static readonly Color ColorCritical = new(200, 80, 80);
    private static readonly Color ColorFailed = new(220, 35, 28);
    private static readonly Color ColorUnavailable = new(100, 95, 90);
    private static readonly Color BarBackgroundDefault = new(35, 32, 28);
    private static readonly Color BarBackgroundWarning = new(52, 36, 12);
    private static readonly Color BarBackgroundFailed = new(72, 16, 12);

    private enum DistressKind
    {
        None,
        Warning,
        Failed
    }

    private static readonly Color BackgroundColor = new(20, 18, 15, 220);
    private static readonly Color BorderColor = new(80, 70, 55);
    private static readonly Color HeaderColor = new(200, 170, 100);
    private static readonly Color LabelColor = new(170, 165, 155);

    private static Texture2D? _glowTexture;
    private static Texture2D? _pixelTexture;

    public PawnCapabilitiesOverlay(PawnBody body)
    {
        _capabilities = body.Capabilities;
        ClipToBounds = false;

        Background = new SolidBrush(BackgroundColor);
        Border = new SolidBrush(BorderColor);
        BorderThickness = new Thickness(1);
        Padding = new Thickness(8, 6, 8, 8);

        var container = new VerticalStackPanel { Spacing = 4 };
        Widgets.Add(container);

        var headerRow = new HorizontalStackPanel
        {
            Spacing = 6,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Margin = new Thickness(0, 0, 0, 2)
        };

        headerRow.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
        {
            Text = "Capabilities",
            TextColor = HeaderColor
        });

        var headerLine = new Panel
        {
            Height = 1,
            Background = new SolidBrush(BorderColor),
            VerticalAlignment = VerticalAlignment.Center
        };
        HorizontalStackPanel.SetProportionType(headerLine, ProportionType.Fill);
        headerRow.Widgets.Add(headerLine);

        container.Widgets.Add(headerRow);

        var rowsContainer = new VerticalStackPanel { Spacing = 3 };
        container.Widgets.Add(rowsContainer);

        rowsContainer.Widgets.Add(CreateCapabilityRow("Sight", _capabilities.Sight));
        rowsContainer.Widgets.Add(CreateCapabilityRow("Breathing", _capabilities.Breathing));
        rowsContainer.Widgets.Add(CreateCapabilityRow("Circulation", _capabilities.Circulation));
        rowsContainer.Widgets.Add(CreateCapabilityRow("Mobility", _capabilities.Mobility));
    }

    private Widget CreateCapabilityRow(string name, float value)
    {
        var row = new CapabilityRow
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            ClipToBounds = false
        };

        var stack = new HorizontalStackPanel
        {
            Spacing = 6,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        var indicator = new Panel
        {
            Width = 5,
            Height = 5,
            Background = Brush(GetStatusColor(value)),
            VerticalAlignment = VerticalAlignment.Center
        };
        stack.Widgets.Add(indicator);

        var nameLabel = new Label(BaseContent.Styles.Label.Small)
        {
            Text = name,
            TextColor = DistressLabelColor(value),
            Width = 90,
            VerticalAlignment = VerticalAlignment.Center
        };
        stack.Widgets.Add(nameLabel);

        var barBackground = new Panel
        {
            Width = 60,
            Height = 6,
            Background = Brush(DistressBarBackground(value)),
            VerticalAlignment = VerticalAlignment.Center
        };

        var fillWidth = Math.Clamp((int)(value * 60), 0, 60);
        var barFill = new Panel
        {
            Width = fillWidth,
            Height = 6,
            Background = Brush(GetBarColor(value)),
            HorizontalAlignment = HorizontalAlignment.Left
        };
        barBackground.Widgets.Add(barFill);
        stack.Widgets.Add(barBackground);

        row.Widgets.Add(stack);
        row.Bind(nameLabel, indicator, barFill, barBackground, value, name.GetHashCode());
        _rows[name] = row;
        return row;
    }

    private static string FormatValue(float value)
    {
        if (float.IsNaN(value)) return "n/a";
        return $"{(int)(value * 100)}%";
    }

    private static DistressKind GetDistressKind(float value)
    {
        if (float.IsNaN(value) || value >= 0.6f)
        {
            return DistressKind.None;
        }

        if (value <= 0f)
        {
            return DistressKind.Failed;
        }

        return value >= 0.3f ? DistressKind.Warning : DistressKind.None;
    }

    private static Color DistressLabelColor(float value) => GetDistressKind(value) switch
    {
        DistressKind.Failed => ColorFailed,
        DistressKind.Warning => ColorWeak,
        _ => LabelColor
    };

    private static Color DistressBarBackground(float value) => GetDistressKind(value) switch
    {
        DistressKind.Failed => BarBackgroundFailed,
        DistressKind.Warning => BarBackgroundWarning,
        _ => BarBackgroundDefault
    };

    private static Color GetStatusColor(float value)
    {
        if (float.IsNaN(value)) return ColorUnavailable;
        return value switch
        {
            >= 1f => ColorExcellent,
            >= 0.6f => ColorGood,
            >= 0.3f => ColorWeak,
            > 0f => ColorCritical,
            _ => ColorFailed
        };
    }

    private static Color GetValueColor(float value)
    {
        if (float.IsNaN(value)) return ColorUnavailable;
        return value switch
        {
            >= 1f => ColorExcellent,
            >= 0.6f => ColorGood,
            >= 0.3f => ColorWeak,
            > 0f => ColorCritical,
            _ => ColorFailed
        };
    }

    private static Color GetBarColor(float value)
    {
        if (float.IsNaN(value)) return new Color(45, 42, 38);
        return value switch
        {
            >= 1f => new Color(100, 170, 70),
            >= 0.6f => new Color(160, 180, 70),
            >= 0.3f => new Color(200, 140, 50),
            > 0f => new Color(180, 70, 70),
            _ => ColorFailed
        };
    }

    public void Update()
    {
        UpdateCapability("Sight", _capabilities.Sight);
        UpdateCapability("Breathing", _capabilities.Breathing);
        UpdateCapability("Circulation", _capabilities.Circulation);
        UpdateCapability("Mobility", _capabilities.Mobility);

        var deltaTime = 1f / 60f;
        foreach (var row in _rows.Values)
        {
            row.Tick(deltaTime);
        }
    }

    private void UpdateCapability(string name, float value)
    {
        if (!_rows.TryGetValue(name, out var row)) return;
        if (row.LastValue == value)
        {
            return;
        }

        row.LastValue = value;
        row.BarFill.Width = Math.Clamp((int)(value * 60), 0, 60);
        row.BarFill.Background = Brush(GetBarColor(value));
        row.Indicator.Background = Brush(GetStatusColor(value));
        row.NameLabel.TextColor = DistressLabelColor(value);
        row.BarBackground.Background = Brush(DistressBarBackground(value));
    }

    private SolidBrush Brush(Color color)
    {
        if (!_brushes.TryGetValue(color, out var brush))
        {
            brush = new SolidBrush(color);
            _brushes[color] = brush;
        }

        return brush;
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

    private sealed class CapabilityRow : Panel
    {
        public Label NameLabel = null!;
        public Panel Indicator = null!;
        public Panel BarFill = null!;
        public Panel BarBackground = null!;
        public float LastValue;

        private readonly List<DistressParticle> _particles = [];
        private float _spawnTimer;
        private float _phase;
        private DistressKind _kind;

        public void Bind(Label nameLabel, Panel indicator, Panel barFill, Panel barBackground, float value, int phaseSeed)
        {
            NameLabel = nameLabel;
            Indicator = indicator;
            BarFill = barFill;
            BarBackground = barBackground;
            LastValue = value;
            _phase = (phaseSeed & 0xFF) / 40f;
            _kind = GetDistressKind(value);
        }

        public void Tick(float deltaTime)
        {
            var kind = GetDistressKind(LastValue);
            if (kind != _kind)
            {
                _particles.Clear();
                _spawnTimer = 0f;
                _kind = kind;
            }

            if (kind == DistressKind.None)
            {
                return;
            }

            var maxParticles = kind == DistressKind.Failed ? 18 : 7;
            var spawnInterval = kind == DistressKind.Failed ? 0.06f : 0.14f;
            _spawnTimer += deltaTime;
            while (_spawnTimer >= spawnInterval && _particles.Count < maxParticles)
            {
                _spawnTimer -= spawnInterval;
                SpawnParticle(kind);
            }

            var rise = kind == DistressKind.Failed ? 70f : 40f;
            var sway = kind == DistressKind.Failed ? 28f : 16f;
            var gravity = kind == DistressKind.Failed ? 220f : 140f;
            for (var i = _particles.Count - 1; i >= 0; i--)
            {
                var particle = _particles[i];
                particle.Life -= deltaTime;
                particle.Position += particle.Velocity * deltaTime;
                if (particle.IsEmber)
                {
                    particle.Velocity.Y -= rise * deltaTime;
                    particle.Velocity.X += MathF.Sin((particle.MaxLife - particle.Life) * 14f) * sway * deltaTime;
                }
                else
                {
                    particle.Velocity.Y += gravity * deltaTime;
                    particle.Velocity.X *= 0.96f;
                }

                if (particle.Life <= 0f)
                {
                    _particles.RemoveAt(i);
                }
            }
        }

        public override void InternalRender(RenderContext context)
        {
            var kind = GetDistressKind(LastValue);
            if (kind != DistressKind.None)
            {
                DrawDistressWash(context, kind);
            }

            base.InternalRender(context);

            if (kind != DistressKind.None)
            {
                DrawParticles(context, kind);
            }
        }

        private void SpawnParticle(DistressKind kind)
        {
            var bounds = ActualBounds;
            if (bounds.Width <= 0 || bounds.Height <= 0)
            {
                return;
            }

            var warning = kind == DistressKind.Warning;
            var isEmber = Rng.Visual.NextDouble() < (warning ? 0.75 : 0.55);
            var originX = bounds.Width * (0.35f + (float)Rng.Visual.NextDouble() * 0.6f);
            var originY = bounds.Height * (0.35f + (float)Rng.Visual.NextDouble() * 0.35f);
            var scale = warning ? 0.7f : 1f;
            if (isEmber)
            {
                _particles.Add(new DistressParticle
                {
                    Position = new Vector2(originX, originY),
                    Velocity = new Vector2(
                        (float)(Rng.Visual.NextDouble() - 0.5) * (warning ? 10f : 18f),
                        -(warning ? 10f : 18f) - (float)Rng.Visual.NextDouble() * (warning ? 16f : 28f)),
                    Life = (warning ? 0.35f : 0.45f) + (float)Rng.Visual.NextDouble() * 0.3f,
                    MaxLife = warning ? 0.55f : 0.7f,
                    Size = (4f + (float)Rng.Visual.NextDouble() * 4f) * scale,
                    Color = warning
                        ? Color.Lerp(new Color(255, 170, 50), new Color(255, 230, 120), (float)Rng.Visual.NextDouble())
                        : Color.Lerp(new Color(255, 70, 20), new Color(255, 210, 70), (float)Rng.Visual.NextDouble()),
                    IsEmber = true
                });
                return;
            }

            _particles.Add(new DistressParticle
            {
                Position = new Vector2(originX, originY),
                Velocity = new Vector2(
                    (float)(Rng.Visual.NextDouble() - 0.5) * (warning ? 8f : 14f),
                    (warning ? 10f : 18f) + (float)Rng.Visual.NextDouble() * (warning ? 12f : 22f)),
                Life = (warning ? 0.3f : 0.4f) + (float)Rng.Visual.NextDouble() * 0.25f,
                MaxLife = warning ? 0.5f : 0.65f,
                Size = (2.5f + (float)Rng.Visual.NextDouble() * 2.5f) * scale,
                Color = warning
                    ? Color.Lerp(new Color(180, 90, 20), new Color(230, 150, 40), (float)Rng.Visual.NextDouble())
                    : Color.Lerp(new Color(120, 8, 12), new Color(200, 24, 28), (float)Rng.Visual.NextDouble()),
                IsEmber = false
            });
        }

        private void DrawDistressWash(RenderContext context, DistressKind kind)
        {
            var bounds = ActualBounds;
            if (bounds.Width <= 0 || bounds.Height <= 0)
            {
                return;
            }

            var warning = kind == DistressKind.Warning;
            var time = Core.TotalTime + _phase;
            var pulse = (warning ? 0.08f : 0.22f) + (warning ? 0.06f : 0.16f) * (0.5f + 0.5f * MathF.Sin(time * (warning ? 5f : 7f)));
            var flicker = (warning ? 0.04f : 0.1f) + (warning ? 0.05f : 0.12f) * (0.5f + 0.5f * MathF.Sin(time * (warning ? 12f : 18f)));
            var pixel = PixelTexture();
            context.Draw(pixel, bounds, (warning ? new Color(200, 130, 30) : new Color(170, 18, 16)) * pulse);
            context.Draw(pixel, bounds, (warning ? new Color(255, 200, 70) : new Color(255, 90, 18)) * flicker);

            var glow = GlowTexture();
            var glowSize = Math.Max(bounds.Width, bounds.Height) + (warning ? 6 : 10);
            context.Draw(glow, new Rectangle(
                bounds.X + bounds.Width / 2 - glowSize / 2,
                bounds.Y + bounds.Height / 2 - glowSize / 2,
                glowSize,
                glowSize), (warning ? new Color(230, 170, 40) : new Color(220, 40, 20)) * ((warning ? 0.12f : 0.28f) + flicker));
        }

        private void DrawParticles(RenderContext context, DistressKind kind)
        {
            if (_particles.Count == 0)
            {
                return;
            }

            var origin = ActualBounds;
            var glow = GlowTexture();
            var pixel = PixelTexture();
            var fadeScale = kind == DistressKind.Warning ? 0.55f : 0.85f;
            foreach (var particle in _particles)
            {
                var fade = Math.Clamp(particle.Life / particle.MaxLife, 0f, 1f);
                var x = origin.X + (int)particle.Position.X;
                var y = origin.Y + (int)particle.Position.Y;
                var size = Math.Max(2, (int)(particle.Size * (particle.IsEmber ? 1.4f : 1f)));
                context.Draw(glow, new Rectangle(x - size / 2, y - size / 2, size, size), particle.Color * (fadeScale * fade));

                if (particle.IsEmber)
                {
                    continue;
                }

                var trail = Math.Max(2, (int)((kind == DistressKind.Warning ? 4 : 6) * fade));
                context.Draw(pixel, new Rectangle(x, y, kind == DistressKind.Warning ? 1 : 2, trail), particle.Color * (0.45f * fade));
            }
        }
    }

    private sealed class DistressParticle
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public float Life;
        public float MaxLife;
        public float Size;
        public Color Color;
        public bool IsEmber;
    }
}
