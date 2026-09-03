namespace Wendlemire.Scenes.MainGameScene.Gui.Widgets;

/// <summary>
/// Prep-style golden hint glow with tiny star sprites around a widget's bounds.
/// </summary>
public sealed class HintSparkleFx
{
    private const string StarAsset = "Fx/HintStar";
    private const float HintSparkInterval = 0.7f;
    private const int HintSparksPerPulse = 2;
    private static readonly Color AvailableHintColor = new(186, 148, 62);
    private const float StarOpacity = 0.48f;
    private static Texture2D? _glowTexture;
    private static Texture2D? _starTexture;

    private readonly List<Spark> _sparks = [];
    private float _hintPhase;
    private float _hintEmit;

    public void Clear()
    {
        _sparks.Clear();
        _hintEmit = 0f;
    }

    public void Update(float deltaTime, Rectangle bounds)
    {
        _hintPhase += deltaTime;
        TickSparks(deltaTime);
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        _hintEmit += deltaTime;
        if (_hintEmit < HintSparkInterval)
        {
            return;
        }

        _hintEmit -= HintSparkInterval;
        for (var i = 0; i < HintSparksPerPulse; i++)
        {
            SpawnHintSpark(bounds);
        }
    }

    public void Draw(RenderContext context, Rectangle bounds)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        var glow = EnsureGlowTexture();
        var pulse = 0.88f + 0.12f * (0.5f + 0.5f * MathF.Sin(_hintPhase * 0.7f));
        var center = new Vector2(bounds.X + bounds.Width * 0.5f, bounds.Y + bounds.Height * 0.5f);
        var cell = Math.Min(bounds.Width, bounds.Height);
        DrawHalo(context, glow, center, cell * (1.2f + 0.06f * pulse), AvailableHintColor * (0.09f * pulse));
        DrawHalo(context, glow, center, cell * (0.78f + 0.04f * pulse), Color.Lerp(AvailableHintColor, Color.White, 0.15f) * (0.04f * pulse));

        var star = EnsureStarTexture();
        foreach (var spark in _sparks)
        {
            var pos = center + spark.Offset;
            var t = Math.Clamp(spark.Life / spark.MaxLife, 0f, 1f);
            var fade = t < 0.2f ? t / 0.2f : t > 0.7f ? (1f - t) / 0.3f : 1f;
            var twinkle = 0.72f + 0.28f * (0.5f + 0.5f * MathF.Sin(spark.Twinkle + (1f - t) * 8f));
            var size = Math.Max(8, (int)(spark.Size * twinkle));
            context.Draw(star, new Rectangle(
                (int)pos.X - size / 2,
                (int)pos.Y - size / 2,
                size,
                size), spark.Color * (fade * StarOpacity));
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

    private void SpawnHintSpark(Rectangle bounds)
    {
        var halfW = bounds.Width * 0.38f;
        var halfH = bounds.Height * 0.38f;
        var edge = Rng.Visual.Next(4);
        var offset = edge switch
        {
            0 => new Vector2(RandomAlong(halfW), -halfH),
            1 => new Vector2(RandomAlong(halfW), halfH),
            2 => new Vector2(-halfW, RandomAlong(halfH)),
            _ => new Vector2(halfW, RandomAlong(halfH))
        };
        var outward = offset.LengthSquared() > 0.01f ? Vector2.Normalize(offset) : new Vector2(0, -1);
        var speed = 1.2f + Rng.Visual.Next(0, 20) / 10f;
        var life = 1.6f + Rng.Visual.Next(0, 60) / 100f;
        _sparks.Add(new Spark
        {
            Offset = offset,
            Velocity = outward * speed + new Vector2(0, -1.4f),
            Life = life,
            MaxLife = life,
            Size = Rng.Visual.Next(9, 13),
            Color = Color.Lerp(AvailableHintColor, Color.White, Rng.Visual.Next(0, 18) / 100f),
            Gravity = -2f,
            Twinkle = Rng.Visual.Next(0, 628) / 100f
        });
    }

    private static float RandomAlong(float half)
    {
        var span = Math.Max(1, (int)half);
        return Rng.Visual.Next(-span, span + 1);
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

    private static Texture2D EnsureStarTexture()
    {
        if (_starTexture != null)
        {
            return _starTexture;
        }

        if (Core.Content.TryLoad<Texture2D>(StarAsset, out var loaded) && loaded != null)
        {
            _starTexture = MakeTintableStar(loaded);
            return _starTexture;
        }

        _starTexture = CreateFallbackStar();
        return _starTexture;
    }

    private static Texture2D MakeTintableStar(Texture2D source)
    {
        var data = new Color[source.Width * source.Height];
        source.GetData(data);
        for (var i = 0; i < data.Length; i++)
        {
            var pixel = data[i];
            if (pixel.A < 12 || (pixel.R + pixel.G + pixel.B) < 24)
            {
                data[i] = Color.Transparent;
                continue;
            }

            var luminance = Math.Clamp((pixel.R * 0.35f + pixel.G * 0.5f + pixel.B * 0.15f) / 255f, 0f, 1f);
            var a = (byte)Math.Clamp(pixel.A * (0.55f + 0.45f * luminance), 0, 255);
            data[i] = new Color(a, a, a, a);
        }

        var texture = new Texture2D(source.GraphicsDevice, source.Width, source.Height);
        texture.SetData(data);
        return texture;
    }

    private static Texture2D CreateFallbackStar()
    {
        const int size = 16;
        var data = new Color[size * size];
        var cx = (size - 1) * 0.5f;
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var dx = MathF.Abs(x - cx);
                var dy = MathF.Abs(y - cx);
                var plus = MathF.Min(dx, dy * 0.22f) + MathF.Min(dy, dx * 0.22f);
                var diag = MathF.Abs(dx - dy);
                var ray = MathF.Min(plus, diag + 1.1f);
                var alpha = MathF.Max(0f, 1f - ray / 1.35f);
                alpha *= alpha;
                var a = (byte)(alpha * 255f);
                data[y * size + x] = new Color(a, a, a, a);
            }
        }

        var texture = new Texture2D(Core.GraphicsDevice, size, size);
        texture.SetData(data);
        return texture;
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

    private sealed class Spark
    {
        public Vector2 Offset;
        public Vector2 Velocity;
        public float Life;
        public float MaxLife;
        public float Size;
        public Color Color;
        public float Gravity;
        public float Twinkle;
    }
}
