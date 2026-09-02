namespace Wendlemire.Scenes.MainGameScene.Gui.CombatGui;

internal sealed class CombatIncenseSmokeFx : Widget
{
    private readonly Dictionary<string, Burner> _burners = [];
    private readonly List<Wisp> _wisps = [];
    private readonly List<Halo> _halos = [];
    private SpriteBatch? _spriteBatch;
    private static Texture2D? _glowTexture;

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
            "Clotcedar" => new Color(230, 70, 45),
            "LungwortBraid" => new Color(150, 220, 140),
            "ShadeWood" => new Color(150, 130, 200),
            "WitchWood" => new Color(210, 130, 230),
            "DippedMullinStick" => new Color(255, 210, 80),
            _ => new Color(255, 170, 70)
        };
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
                    Color = Color.Lerp(source.Tint, Color.White, 0.4f),
                    StartSize = 36f,
                    EndSize = 140f,
                    Life = 1.1f,
                    MaxLife = 1.1f
                });
                SpawnBurst(origin, source.Tint, 14, 1.3f);
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
            StartSize = 48f,
            EndSize = 180f,
            Life = 1.2f,
            MaxLife = 1.2f
        });
        SpawnBurst(origin, tint, 22, 1.4f);
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
            if (burner.SpawnTimer > 0)
            {
                continue;
            }

            burner.SpawnTimer = 0.055f;
            var origin = burner.Source.ToGlobal(burner.SourceLocal);
            SpawnWisp(origin, burner.Tint, ember: false);
            if (Rng.Visual.Next(0, 100) < 40)
            {
                SpawnWisp(origin, burner.Tint, ember: true);
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

            wisp.WobblePhase += deltaTime * wisp.WobbleSpeed;
            wisp.Position += wisp.Velocity * deltaTime;
            wisp.Position.X += MathF.Sin(wisp.WobblePhase) * wisp.Wobble * deltaTime;
            wisp.Size += wisp.Grow * deltaTime;
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
        var uiScale = MeasureUiScale();

        _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.LinearClamp);
        foreach (var wisp in _wisps)
        {
            if (wisp.IsEmber)
            {
                continue;
            }

            var fade = Math.Clamp(wisp.Life / wisp.MaxLife, 0f, 1f);
            var size = Math.Max(10, (int)(wisp.Size * uiScale));
            _spriteBatch.Draw(glow, new Rectangle(
                (int)wisp.Position.X - size / 2,
                (int)wisp.Position.Y - size / 2,
                size,
                size), wisp.Color * (0.55f * fade));
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
                size), halo.Color * (0.7f * pulse));
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

    private void SpawnBurst(Vector2 origin, Color tint, int count, float life)
    {
        for (var i = 0; i < count; i++)
        {
            SpawnWisp(origin, tint, ember: i % 3 == 0, life);
        }
    }

    private void SpawnWisp(Vector2 origin, Color tint, bool ember, float? life = null)
    {
        if (ember)
        {
            _wisps.Add(new Wisp
            {
                Position = origin + new Vector2(Rng.Visual.Next(-8, 8), Rng.Visual.Next(-6, 6)),
                Velocity = new Vector2(-20f + Rng.Visual.Next(0, 40), -80f - Rng.Visual.Next(0, 50)),
                Life = life ?? 0.8f + Rng.Visual.Next(0, 40) / 100f,
                MaxLife = life ?? 1.2f,
                Size = 6f + Rng.Visual.Next(0, 8),
                Color = Color.Lerp(new Color(255, 170, 50), tint, 0.35f),
                IsEmber = true,
                Wobble = 16f + Rng.Visual.Next(0, 18),
                WobbleSpeed = 9f,
                WobblePhase = Rng.Visual.Next(0, 628) / 100f
            });
            return;
        }

        _wisps.Add(new Wisp
        {
            Position = origin + new Vector2(Rng.Visual.Next(-10, 10), Rng.Visual.Next(-8, 8)),
            Velocity = new Vector2(-22f + Rng.Visual.Next(0, 44), -42f - Rng.Visual.Next(0, 36)),
            Life = life ?? 1.6f + Rng.Visual.Next(0, 70) / 100f,
            MaxLife = life ?? 2.3f,
            Size = 22f + Rng.Visual.Next(0, 20),
            Grow = 14f,
            Color = Color.Lerp(new Color(200, 190, 180), tint, 0.35f),
            Wobble = 24f + Rng.Visual.Next(0, 22),
            WobbleSpeed = 2.6f,
            WobblePhase = Rng.Visual.Next(0, 628) / 100f
        });
    }

    private float MeasureUiScale()
    {
        var a = ToGlobal(Vector2.Zero);
        var b = ToGlobal(new Vector2(100f, 0f));
        return Math.Max(0.01f, (b - a).Length() / 100f);
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

    private sealed class Burner
    {
        public Widget Source = null!;
        public Vector2 SourceLocal;
        public Color Tint;
        public float SpawnTimer;
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
