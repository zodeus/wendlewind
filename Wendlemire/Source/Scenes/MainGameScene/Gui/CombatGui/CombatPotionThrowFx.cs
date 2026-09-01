namespace Wendlemire.Scenes.MainGameScene.Gui.CombatGui;

internal sealed class CombatPotionThrowFx : Widget
{
    private const float BurstLife = 0.75f;

    public event Action<CombatLogEvent>? Impacted;

    private readonly List<Throw> _throws = [];
    private readonly List<Spark> _sparks = [];
    private readonly List<Shard> _shards = [];
    private readonly List<Halo> _halos = [];
    private SpriteBatch? _spriteBatch;
    private static Texture2D? _glowTexture;

    public CombatPotionThrowFx()
    {
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;
        ClipToBounds = false;
    }

    public bool TryStart(
        CombatLogEvent combatEvent,
        Widget source,
        Vector2 sourceLocal,
        Widget target,
        Vector2 targetLocal,
        Texture2D icon,
        bool thrown)
    {
        var start = source.ToGlobal(sourceLocal);
        var end = target.ToGlobal(targetLocal);
        var delta = end - start;
        var distance = delta.Length();
        var duration = Math.Clamp(0.35f + distance / 1400f, 0.35f, 0.8f);
        var arcHeight = thrown
            ? Math.Clamp(distance * 0.22f, 36f, 90f)
            : Math.Clamp(distance * 0.18f, 18f, 42f);
        var spinTurns = thrown ? 1.25f + Rng.Visual.Next(0, 80) / 100f : 0.12f;
        var spin = MathHelper.TwoPi * spinTurns * (delta.X >= 0 ? 1f : -1f);

        _throws.Add(new Throw
        {
            Event = combatEvent,
            Icon = icon,
            Source = source,
            Target = target,
            SourceLocal = sourceLocal,
            TargetLocal = targetLocal,
            Start = start,
            End = end,
            Duration = duration,
            ArcHeight = arcHeight,
            Spin = spin,
            Size = thrown ? 38f : 32f,
            Tint = thrown ? new Color(255, 230, 160) : Color.White
        });
        return true;
    }

    public void Update(float deltaTime)
    {
        for (var i = _throws.Count - 1; i >= 0; i--)
        {
            var toss = _throws[i];
            toss.Start = toss.Source.ToGlobal(toss.SourceLocal);
            toss.End = toss.Target.ToGlobal(toss.TargetLocal);
            toss.Elapsed += deltaTime;
            if (toss.Elapsed < toss.Duration)
            {
                continue;
            }

            SpawnImpact(toss);
            Impacted?.Invoke(toss.Event);
            _throws.RemoveAt(i);
        }

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
            spark.Position += spark.Velocity * deltaTime;
        }

        for (var i = _shards.Count - 1; i >= 0; i--)
        {
            var shard = _shards[i];
            shard.Life -= deltaTime;
            if (shard.Life <= 0)
            {
                _shards.RemoveAt(i);
                continue;
            }

            shard.Velocity.Y += shard.Gravity * deltaTime;
            shard.Position += shard.Velocity * deltaTime;
            shard.Rotation += shard.Spin * deltaTime;
        }

        for (var i = _halos.Count - 1; i >= 0; i--)
        {
            var halo = _halos[i];
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
        if (_throws.Count == 0 && _sparks.Count == 0 && _shards.Count == 0 && _halos.Count == 0)
        {
            return;
        }

        foreach (var toss in _throws)
        {
            toss.Start = toss.Source.ToGlobal(toss.SourceLocal);
            toss.End = toss.Target.ToGlobal(toss.TargetLocal);
        }

        var uiScale = MeasureUiScale();

        context.Flush();
        _spriteBatch ??= new SpriteBatch(Core.GraphicsDevice);
        var glow = EnsureGlowTexture();

        _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearClamp);
        foreach (var halo in _halos)
        {
            var t = Math.Clamp(1f - halo.Life / halo.MaxLife, 0f, 1f);
            var pulse = halo.Hold
                ? MathF.Pow(1f - t, 1.6f)
                : MathF.Sin(t * MathF.PI);
            var diameter = (halo.StartSize + (halo.EndSize - halo.StartSize) * t) * uiScale;
            DrawHalo(_spriteBatch, glow, halo.Center, diameter, halo.Color * (halo.Brightness * pulse));
        }

        foreach (var spark in _sparks)
        {
            var fade = Math.Clamp(spark.Life / spark.MaxLife, 0f, 1f);
            var size = Math.Max(8, (int)(spark.Size * 3.2f * uiScale));
            _spriteBatch.Draw(glow, new Rectangle(
                (int)spark.Position.X - size / 2,
                (int)spark.Position.Y - size / 2,
                size,
                size), spark.Color * (0.95f * fade));
        }

        foreach (var shard in _shards)
        {
            var fade = Math.Clamp(shard.Life / shard.MaxLife, 0f, 1f);
            var length = shard.Length * uiScale;
            var width = shard.Width * uiScale;
            _spriteBatch.Draw(
                glow,
                shard.Position,
                null,
                shard.Color * (0.95f * fade),
                shard.Rotation,
                new Vector2(glow.Width * 0.5f, glow.Height * 0.5f),
                new Vector2(length / glow.Width, width / glow.Height),
                SpriteEffects.None,
                0f);
        }

        _spriteBatch.End();

        _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp);
        foreach (var toss in _throws)
        {
            var t = Math.Clamp(toss.Elapsed / toss.Duration, 0f, 1f);
            var pos = EvaluateArc(toss.Start, toss.End, toss.ArcHeight, t);
            var pop = 1f + 0.55f * MathF.Pow(t, 3f);
            var scale = toss.Size * pop * uiScale / Math.Max(toss.Icon.Width, toss.Icon.Height);
            var rotation = toss.Spin * t;
            _spriteBatch.Draw(
                toss.Icon,
                pos,
                null,
                toss.Tint,
                rotation,
                new Vector2(toss.Icon.Width * 0.5f, toss.Icon.Height * 0.5f),
                scale,
                SpriteEffects.None,
                0f);
        }

        _spriteBatch.End();
    }

    private float MeasureUiScale()
    {
        var a = ToGlobal(Vector2.Zero);
        var b = ToGlobal(new Vector2(100f, 0f));
        return Math.Max(0.01f, (b - a).Length() / 100f);
    }

    private void SpawnImpact(Throw toss)
    {
        var splash = toss.Event.TargetPawnId is { } targetId && targetId != toss.Event.SubjectPawnId
            ? new Color(160, 230, 55)
            : new Color(255, 200, 40);
        var fire = Color.Lerp(splash, new Color(255, 90, 20), 0.45f);
        var core = Color.Lerp(splash, Color.White, 0.65f);

        _halos.Add(new Halo
        {
            Center = toss.End,
            Color = Color.White,
            StartSize = 80f,
            EndSize = 220f,
            Life = 0.16f,
            MaxLife = 0.16f,
            Brightness = 1.15f,
            Hold = true
        });
        _halos.Add(new Halo
        {
            Center = toss.End,
            Color = core,
            StartSize = 50f,
            EndSize = 280f,
            Life = 0.38f,
            MaxLife = 0.38f,
            Brightness = 0.95f
        });
        _halos.Add(new Halo
        {
            Center = toss.End,
            Color = splash,
            StartSize = 70f,
            EndSize = 420f,
            Life = BurstLife,
            MaxLife = BurstLife,
            Brightness = 0.7f
        });
        _halos.Add(new Halo
        {
            Center = toss.End,
            Color = fire,
            StartSize = 40f,
            EndSize = 340f,
            Life = BurstLife * 0.85f,
            MaxLife = BurstLife * 0.85f,
            Brightness = 0.55f
        });

        for (var i = 0; i < 36; i++)
        {
            var angle = MathHelper.ToRadians(Rng.Visual.Next(0, 360));
            var speed = Rng.Visual.Next(90, 260);
            var life = BurstLife * (0.55f + Rng.Visual.Next(0, 45) / 100f);
            _sparks.Add(new Spark
            {
                Position = toss.End + new Vector2(Rng.Visual.Next(-8, 9), Rng.Visual.Next(-8, 9)),
                Velocity = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * speed,
                Life = life,
                MaxLife = life,
                Size = Rng.Visual.Next(8, 18),
                Color = i % 4 == 0 ? Color.White : i % 2 == 0 ? core : splash,
                Gravity = 220f
            });
        }

        for (var i = 0; i < 22; i++)
        {
            var angle = MathHelper.ToRadians(Rng.Visual.Next(0, 360));
            var speed = Rng.Visual.Next(140, 340);
            var length = Rng.Visual.Next(28, 56);
            var life = BurstLife * (0.7f + Rng.Visual.Next(0, 35) / 100f);
            _shards.Add(new Shard
            {
                Position = toss.End,
                Velocity = new Vector2(MathF.Cos(angle), MathF.Sin(angle) - 0.35f) * speed,
                Life = life,
                MaxLife = life,
                Length = length,
                Width = Rng.Visual.Next(8, 16),
                Rotation = angle,
                Spin = (Rng.Visual.Next(0, 2) == 0 ? -1f : 1f) * (8f + Rng.Visual.Next(0, 10)),
                Color = i % 3 == 0 ? Color.White : Color.Lerp(splash, fire, Rng.Visual.Next(0, 80) / 100f),
                Gravity = 380f
            });
        }
    }

    private static Vector2 EvaluateArc(Vector2 start, Vector2 end, float height, float t)
    {
        var along = start + (end - start) * t;
        along.Y -= 4f * height * t * (1f - t);
        return along;
    }

    private static void DrawHalo(SpriteBatch spriteBatch, Texture2D glow, Vector2 center, float diameter, Color color)
    {
        var size = Math.Max(8, (int)diameter);
        spriteBatch.Draw(glow, new Rectangle(
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

    private sealed class Throw
    {
        public CombatLogEvent Event = null!;
        public Texture2D Icon = null!;
        public Widget Source = null!;
        public Widget Target = null!;
        public Vector2 SourceLocal;
        public Vector2 TargetLocal;
        public Vector2 Start;
        public Vector2 End;
        public float Elapsed;
        public float Duration;
        public float ArcHeight;
        public float Spin;
        public float Size;
        public Color Tint;
    }

    private sealed class Spark
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public float Life;
        public float MaxLife;
        public float Size;
        public Color Color;
        public float Gravity;
    }

    private sealed class Shard
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public float Life;
        public float MaxLife;
        public float Length;
        public float Width;
        public float Rotation;
        public float Spin;
        public Color Color;
        public float Gravity;
    }

    private sealed class Halo
    {
        public Vector2 Center;
        public Color Color;
        public float StartSize;
        public float EndSize;
        public float Life;
        public float MaxLife;
        public float Brightness = 0.7f;
        public bool Hold;
    }
}
