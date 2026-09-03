namespace Wendlemire.Scenes.MainGameScene.Gui.CombatGui;

internal sealed class CombatIncenseSmokeFx : Widget
{
    private static readonly string[] SmokeAssetPaths =
    [
        "Fx/IncenseSmokePuff",
        "Fx/IncenseSmokeWisp",
        "Fx/IncenseSmokeCloud"
    ];

    private readonly Dictionary<string, Burner> _burners = [];
    private readonly List<Wisp> _wisps = [];
    private readonly List<Halo> _halos = [];
    private SpriteBatch? _spriteBatch;
    private static Texture2D? _glowTexture;
    private static Texture2D[]? _smokePuffs;

    public CombatIncenseSmokeFx()
    {
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;
        ClipToBounds = false;
    }

    public readonly struct BurnSource
    {
        public BurnSource(string id, Widget source, Vector2 sourceLocal, Color tint)
        {
            Id = id;
            Source = source;
            SourceLocal = sourceLocal;
            Tint = tint;
        }

        public string Id { get; }
        public Widget Source { get; }
        public Vector2 SourceLocal { get; }
        public Color Tint { get; }
    }

    public static Color TintFor(ActiveIncense incense)
    {
        return TintFor(incense.SourceMoniker ?? incense.Def?.Moniker);
    }

    public static Color TintFor(string? moniker)
    {
        return moniker switch
        {
            "Clotcedar" => new Color(210, 55, 40),
            "LungwortBraid" => new Color(90, 170, 95),
            "ShadeWood" => new Color(120, 90, 190),
            "WitchWood" => new Color(190, 80, 210),
            "DippedMullinStick" => new Color(240, 185, 55),
            "MullinStick" => new Color(220, 140, 55),
            _ => new Color(200, 130, 60)
        };
    }

    public static Color SmokeColor(Color tint, float shade)
    {
        var deep = new Color(
            (byte)Math.Clamp(tint.R * 0.55f, 0, 255),
            (byte)Math.Clamp(tint.G * 0.45f, 0, 255),
            (byte)Math.Clamp(tint.B * 0.62f, 0, 255));
        var saturated = Color.Lerp(tint, deep, 0.15f);
        return Color.Lerp(deep, saturated, Math.Clamp(shade, 0f, 1f) * 0.7f);
    }

    public static Color EmberColor(Color tint, float shade = 0.5f)
    {
        var hot = Color.Lerp(new Color(255, 150, 50), tint, 0.65f);
        var cool = Color.Lerp(tint, new Color(90, 40, 20), 0.35f);
        return Color.Lerp(cool, hot, Math.Clamp(shade, 0f, 1f));
    }

    public void Sync(IReadOnlyList<BurnSource> active)
    {
        var seen = new HashSet<string>();
        foreach (var source in active)
        {
            seen.Add(source.Id);
            if (!_burners.TryGetValue(source.Id, out var burner))
            {
                burner = new Burner();
                _burners[source.Id] = burner;
                var origin = source.Source.ToGlobal(source.SourceLocal);
                _halos.Add(new Halo
                {
                    Source = source.Source,
                    SourceLocal = source.SourceLocal,
                    Center = origin,
                    Color = Color.Lerp(source.Tint, Color.White, 0.28f),
                    StartSize = 56f,
                    EndSize = 140f,
                    Life = 1.1f,
                    MaxLife = 1.1f
                });
                SpawnBurst(origin, source.Tint, 8, 1.5f, source.Source);
            }

            burner.Source = source.Source;
            burner.SourceLocal = source.SourceLocal;
            burner.Tint = source.Tint;
        }

        foreach (var id in _burners.Keys.Where(key => !seen.Contains(key)).ToList())
        {
            _burners.Remove(id);
        }
    }

    public void TryStart(Widget source, Vector2 sourceLocal, Color tint)
    {
        var origin = source.ToGlobal(sourceLocal);
        _halos.Add(new Halo
        {
            Source = source,
            SourceLocal = sourceLocal,
            Center = origin,
            Color = Color.Lerp(tint, Color.White, 0.4f),
            StartSize = 64f,
            EndSize = 150f,
            Life = 1.2f,
            MaxLife = 1.2f
        });
        SpawnBurst(origin, tint, 14, 1.8f, source);
    }

    public void Update(float deltaTime)
    {
        foreach (var burner in _burners.Values)
        {
            if (burner.Source.ActualBounds.Width <= 0)
            {
                continue;
            }

            burner.SpawnTimer -= deltaTime;
            burner.HaloTimer -= deltaTime;
            var origin = burner.Source.ToGlobal(burner.SourceLocal);
            if (burner.HaloTimer <= 0)
            {
                burner.HaloTimer = 0.9f;
                _halos.Add(new Halo
                {
                    Source = burner.Source,
                    SourceLocal = burner.SourceLocal,
                    Center = origin,
                    Color = Color.Lerp(burner.Tint, Color.White, 0.22f),
                    StartSize = 50f,
                    EndSize = 130f,
                    Life = 1.3f,
                    MaxLife = 1.3f
                });
            }

            if (burner.SpawnTimer > 0)
            {
                continue;
            }

            burner.SpawnTimer = 0.075f;
            SpawnWisp(origin, burner.Tint, ember: false, spread: burner.Source);
            if (Rng.Visual.Next(0, 100) < 18)
            {
                SpawnWisp(origin, burner.Tint, ember: true, spread: burner.Source);
            }
        }

        for (var i = _wisps.Count - 1; i >= 0; i--)
        {
            var wisp = _wisps[i];
            wisp.Life -= deltaTime;
            if (wisp.Life <= 0)
            {
                _wisps.RemoveAt(i);
                continue;
            }

            var age = 1f - wisp.Life / wisp.MaxLife;
            wisp.WobblePhase += deltaTime * wisp.WobbleSpeed;
            wisp.Velocity.X *= 1f - 0.4f * deltaTime;
            wisp.Velocity.Y -= 10f * deltaTime;
            wisp.Position += wisp.Velocity * deltaTime;
            wisp.Position.X += MathF.Sin(wisp.WobblePhase) * wisp.Wobble * (0.35f + age) * deltaTime;
            wisp.Size += wisp.Grow * deltaTime;
            wisp.Rotation += wisp.Spin * deltaTime;
        }

        for (var i = _halos.Count - 1; i >= 0; i--)
        {
            var halo = _halos[i];
            halo.Center = halo.Source.ToGlobal(halo.SourceLocal);
            halo.Life -= deltaTime;
            if (halo.Life <= 0)
            {
                _halos.RemoveAt(i);
            }
        }
    }

    public override Widget? HitTest(Point p) => null;

    public override void InternalRender(RenderContext context)
    {
        if (_wisps.Count == 0 && _halos.Count == 0)
        {
            return;
        }

        context.Flush();
        _spriteBatch ??= new SpriteBatch(Core.GraphicsDevice);
        var glow = EnsureGlowTexture();
        var puffs = SmokePuffs();
        var uiScale = MeasureUiScale();

        _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.PointClamp);
        foreach (var wisp in _wisps)
        {
            if (wisp.IsEmber)
            {
                continue;
            }

            var t = Math.Clamp(wisp.Life / wisp.MaxLife, 0f, 1f);
            var fade = Math.Clamp((1f - t) / 0.18f, 0f, 1f) * t;
            var texture = puffs[wisp.Frame % puffs.Length];
            var size = Math.Max(28, wisp.Size * uiScale);
            var origin = new Vector2(texture.Width * 0.5f, texture.Height * 0.5f);
            var scale = size / texture.Width;
            var flip = wisp.Flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            _spriteBatch.Draw(
                texture,
                wisp.Position,
                null,
                wisp.Color * (0.336f * fade),
                wisp.Rotation,
                origin,
                scale,
                flip,
                0f);
        }

        _spriteBatch.End();

        _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearClamp);
        foreach (var halo in _halos)
        {
            var t = Math.Clamp(1f - halo.Life / halo.MaxLife, 0f, 1f);
            var pulse = MathF.Sin(t * MathF.PI);
            var diameter = (halo.StartSize + (halo.EndSize - halo.StartSize) * t) * uiScale;
            var size = Math.Max(8, (int)diameter);
            _spriteBatch.Draw(glow, new Rectangle(
                (int)halo.Center.X - size / 2,
                (int)halo.Center.Y - size / 2,
                size,
                size), halo.Color * (0.16f * pulse));
        }

        foreach (var wisp in _wisps)
        {
            if (!wisp.IsEmber)
            {
                continue;
            }

            var fade = Math.Clamp(wisp.Life / wisp.MaxLife, 0f, 1f);
            var flicker = 0.7f + 0.3f * MathF.Sin(wisp.WobblePhase * 5f);
            var size = Math.Max(4, (int)(wisp.Size * uiScale));
            _spriteBatch.Draw(glow, new Rectangle(
                (int)wisp.Position.X - size / 2,
                (int)wisp.Position.Y - size / 2,
                size,
                size), wisp.Color * (0.95f * fade * flicker));
        }

        _spriteBatch.End();
    }

    private void SpawnBurst(Vector2 origin, Color tint, int count, float life, Widget? spread = null)
    {
        for (var i = 0; i < count; i++)
        {
            SpawnWisp(origin, tint, ember: i % 5 == 0, life, spread);
        }
    }

    private void SpawnWisp(Vector2 origin, Color tint, bool ember, float? life = null, Widget? spread = null)
    {
        var offset = HazeOffset(spread);
        if (ember)
        {
            _wisps.Add(new Wisp
            {
                Position = origin + offset * 0.35f,
                Velocity = new Vector2(-18f + Rng.Visual.Next(0, 36), -28f - Rng.Visual.Next(0, 18)),
                Life = life ?? 0.55f + Rng.Visual.Next(0, 30) / 100f,
                MaxLife = life ?? 0.85f,
                Size = 6f + Rng.Visual.Next(0, 6),
                Color = EmberColor(tint, Rng.Visual.Next(0, 100) / 100f),
                IsEmber = true,
                Wobble = 12f + Rng.Visual.Next(0, 14),
                WobbleSpeed = 6f,
                WobblePhase = Rng.Visual.Next(0, 628) / 100f
            });
            return;
        }

        _wisps.Add(new Wisp
        {
            Position = origin + offset,
            Velocity = new Vector2(-22f + Rng.Visual.Next(0, 44), -14f - Rng.Visual.Next(0, 18)),
            Life = life ?? 2.0f + Rng.Visual.Next(0, 80) / 100f,
            MaxLife = life ?? 2.8f,
            Size = 78f + Rng.Visual.Next(0, 34),
            Grow = 12f,
            Color = SmokeColor(tint, Rng.Visual.Next(0, 100) / 100f),
            Wobble = 28f + Rng.Visual.Next(0, 24),
            WobbleSpeed = 1.1f + Rng.Visual.Next(0, 10) / 10f,
            WobblePhase = Rng.Visual.Next(0, 628) / 100f,
            Frame = Rng.Visual.Next(0, SmokeAssetPaths.Length),
            Rotation = Rng.Visual.Next(-20, 21) / 100f,
            Spin = (Rng.Visual.Next(0, 2) == 0 ? -1f : 1f) * (0.08f + Rng.Visual.Next(0, 12) / 100f),
            Flip = Rng.Visual.Next(0, 2) == 0
        });
    }

    private static Vector2 HazeOffset(Widget? spread)
    {
        var width = spread?.ActualBounds.Width ?? 80;
        var height = spread?.ActualBounds.Height ?? 80;
        var side = Rng.Visual.Next(0, 2) == 0 ? -1f : 1f;
        return new Vector2(
            side * (width * (0.14f + Rng.Visual.Next(0, 22) / 100f)),
            Rng.Visual.Next((int)(-height * 0.32f), (int)(height * 0.32f) + 1));
    }

    private float MeasureUiScale()
    {
        var a = ToGlobal(Vector2.Zero);
        var b = ToGlobal(new Vector2(100f, 0f));
        return Math.Max(0.01f, (b - a).Length() / 100f);
    }

    public static Texture2D[] SmokePuffs()
    {
        if (_smokePuffs != null)
        {
            return _smokePuffs;
        }

        var loaded = new List<Texture2D>();
        foreach (var path in SmokeAssetPaths)
        {
            if (Core.Content.TryLoad<Texture2D>(path, out var texture) && texture != null)
            {
                loaded.Add(MakeTintablePuff(texture));
            }
        }

        _smokePuffs = loaded.Count > 0 ? loaded.ToArray() : [EnsureGlowTexture()];
        return _smokePuffs;
    }

    private static Texture2D MakeTintablePuff(Texture2D source)
    {
        var data = new Color[source.Width * source.Height];
        source.GetData(data);
        for (var i = 0; i < data.Length; i++)
        {
            var pixel = data[i];
            var luminance = (pixel.R + pixel.G + pixel.B) / 3f;
            if (pixel.A < 12 || luminance < 22)
            {
                data[i] = Color.Transparent;
                continue;
            }

            var alpha = (byte)Math.Clamp(MathF.Max(luminance, pixel.A * 0.7f) * 1.2f, 48, 255);
            data[i] = new Color((byte)255, (byte)255, (byte)255, alpha);
        }

        var punched = new Texture2D(source.GraphicsDevice, source.Width, source.Height);
        punched.SetData(data);
        return punched;
    }

    public static Texture2D EnsureGlowTexture()
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
                alpha = alpha * alpha;
                var a = (byte)(alpha * 255f);
                data[y * size + x] = new Color(a, a, a, a);
            }
        }

        _glowTexture = new Texture2D(Core.GraphicsDevice, size, size);
        _glowTexture.SetData(data);
        return _glowTexture;
    }

    private sealed class Burner
    {
        public Widget Source = null!;
        public Vector2 SourceLocal;
        public Color Tint;
        public float SpawnTimer;
        public float HaloTimer;
    }

    private sealed class Wisp
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public float Life;
        public float MaxLife;
        public float Size;
        public float Grow;
        public Color Color;
        public bool IsEmber;
        public float Wobble;
        public float WobbleSpeed;
        public float WobblePhase;
        public int Frame;
        public float Rotation;
        public float Spin;
        public bool Flip;
    }

    private sealed class Halo
    {
        public Widget Source = null!;
        public Vector2 SourceLocal;
        public Vector2 Center;
        public Color Color;
        public float StartSize;
        public float EndSize;
        public float Life;
        public float MaxLife;
    }
}
