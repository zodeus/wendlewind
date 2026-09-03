namespace Wendlemire.Scenes.MainGameScene.Gui.CombatGui;

internal sealed class CombatMedicalTravelFx : Widget
{
    private readonly List<Travel> _travels = [];
    private readonly List<Halo> _halos = [];
    private SpriteBatch? _spriteBatch;
    private static Texture2D? _glowTexture;

    public CombatMedicalTravelFx()
    {
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;
        ClipToBounds = false;
    }

    public bool TryStart(Widget source, Vector2 sourceLocal, Widget target, Vector2 targetLocal, Color tint)
    {
        if (source.Bounds.Width <= 0 || target.Bounds.Width <= 0)
        {
            return false;
        }

        var start = source.ToGlobal(sourceLocal);
        var end = target.ToGlobal(targetLocal);
        var distance = (end - start).Length();
        _travels.Add(new Travel
        {
            Source = source,
            Target = target,
            SourceLocal = sourceLocal,
            TargetLocal = targetLocal,
            Start = start,
            End = end,
            Duration = Math.Clamp(0.22f + distance / 1800f, 0.22f, 0.5f),
            ArcHeight = Math.Clamp(distance * 0.14f, 12f, 36f),
            Tint = tint
        });
        return true;
    }

    public void Update(float deltaTime)
    {
        for (var i = _travels.Count - 1; i >= 0; i--)
        {
            var travel = _travels[i];
            travel.Start = travel.Source.ToGlobal(travel.SourceLocal);
            travel.End = travel.Target.ToGlobal(travel.TargetLocal);
            travel.Elapsed += deltaTime;
            if (travel.Elapsed < travel.Duration)
            {
                continue;
            }

            SpawnImpact(travel);
            _travels.RemoveAt(i);
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
        if (_travels.Count == 0 && _halos.Count == 0)
        {
            return;
        }

        foreach (var travel in _travels)
        {
            travel.Start = travel.Source.ToGlobal(travel.SourceLocal);
            travel.End = travel.Target.ToGlobal(travel.TargetLocal);
        }

        var uiScale = MeasureUiScale();
        context.Flush();
        _spriteBatch ??= new SpriteBatch(Core.GraphicsDevice);
        var glow = EnsureGlowTexture();

        _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearClamp);
        foreach (var halo in _halos)
        {
            var t = Math.Clamp(1f - halo.Life / halo.MaxLife, 0f, 1f);
            var pulse = MathF.Sin(t * MathF.PI);
            var diameter = (halo.StartSize + (halo.EndSize - halo.StartSize) * t) * uiScale;
            DrawHalo(_spriteBatch, glow, halo.Center, diameter, halo.Color * (halo.Brightness * pulse));
        }

        foreach (var travel in _travels)
        {
            var t = Math.Clamp(travel.Elapsed / travel.Duration, 0f, 1f);
            var pos = EvaluateArc(travel.Start, travel.End, travel.ArcHeight, t);
            var size = (10f + 8f * (1f - t)) * uiScale;
            DrawHalo(_spriteBatch, glow, pos, size * 3.2f, travel.Tint * 0.85f);
            DrawHalo(_spriteBatch, glow, pos, size * 1.4f, Color.Lerp(travel.Tint, Color.White, 0.45f) * 0.9f);
        }

        _spriteBatch.End();
    }

    private float MeasureUiScale()
    {
        var a = ToGlobal(Vector2.Zero);
        var b = ToGlobal(new Vector2(100f, 0f));
        return Math.Max(0.01f, (b - a).Length() / 100f);
    }

    private void SpawnImpact(Travel travel)
    {
        _halos.Add(new Halo
        {
            Center = travel.End,
            Color = Color.Lerp(travel.Tint, Color.White, 0.4f),
            StartSize = 28f,
            EndSize = 90f,
            Life = 0.28f,
            MaxLife = 0.28f,
            Brightness = 0.85f
        });
        _halos.Add(new Halo
        {
            Center = travel.End,
            Color = travel.Tint,
            StartSize = 18f,
            EndSize = 140f,
            Life = 0.4f,
            MaxLife = 0.4f,
            Brightness = 0.45f
        });
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

    private sealed class Travel
    {
        public Widget Source = null!;
        public Widget Target = null!;
        public Vector2 SourceLocal;
        public Vector2 TargetLocal;
        public Vector2 Start;
        public Vector2 End;
        public float Elapsed;
        public float Duration;
        public float ArcHeight;
        public Color Tint;
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
    }
}
