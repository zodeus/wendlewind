using Wendlemire.Scenes.MainGameScene.Gui.CombatGui;

namespace Wendlemire.Scenes.MainGameScene.Gui.Widgets.CombatWidgets;

/// <summary>
/// Rising smoke drawn over an incense loadout slot while it is burning.
/// </summary>
internal sealed class IncenseSlotBurnFx : Widget
{
    private const float SpawnInterval = 0.11f;

    private readonly List<Puff> _puffs = [];
    private float _spawnTimer;
    private float _glowPhase;
    private static Texture2D? _glowTexture;

    public bool Burning;
    public Color Tint = new(255, 180, 80);

    public IncenseSlotBurnFx()
    {
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;
        ClipToBounds = false;
    }

    public void Update()
    {
        Tick(1f / 60f);
    }

    public void Tick(float deltaTime)
    {
        _glowPhase += deltaTime * 4f;
        if (Burning)
        {
            _spawnTimer -= deltaTime;
            while (_spawnTimer <= 0)
            {
                _spawnTimer += SpawnInterval;
                Spawn();
            }
        }

        for (var i = _puffs.Count - 1; i >= 0; i--)
        {
            var puff = _puffs[i];
            puff.Life -= deltaTime;
            if (puff.Life <= 0)
            {
                _puffs.RemoveAt(i);
                continue;
            }

            var age = 1f - puff.Life / puff.MaxLife;
            puff.WobblePhase += deltaTime * puff.WobbleSpeed;
            puff.VX *= 1f - 0.45f * deltaTime;
            puff.VY += 22f * deltaTime;
            puff.X += puff.VX * deltaTime + MathF.Sin(puff.WobblePhase) * puff.Wobble * (0.35f + age) * deltaTime;
            puff.Y += puff.VY * deltaTime;
            puff.Size += puff.Grow * deltaTime;
        }
    }

    public override Widget? HitTest(Point p) => null;

    public override void InternalRender(RenderContext context)
    {
        var bounds = ActualBounds;
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        var glow = GlowTexture();
        if (Burning)
        {
            var pulse = 0.7f + 0.3f * MathF.Sin(_glowPhase);
            var tipX = bounds.X + (int)(bounds.Width * 0.58f);
            var tipY = bounds.Y + (int)(bounds.Height * 0.38f);
            var ember = CombatIncenseSmokeFx.EmberColor(Tint);
            var glowSize = (int)(10 + 4 * pulse);
            context.Draw(glow, new Rectangle(
                tipX - glowSize / 2,
                tipY - glowSize / 2,
                glowSize,
                glowSize), ember * (0.4f * pulse));
        }

        foreach (var puff in _puffs)
        {
            var t = Math.Clamp(puff.Life / puff.MaxLife, 0f, 1f);
            var fadeIn = Math.Clamp((1f - t) / 0.18f, 0f, 1f);
            var fade = fadeIn * t;
            var size = Math.Max(3, (int)puff.Size);
            var x = bounds.X + (int)puff.X;
            var y = bounds.Y + (int)puff.Y;
            if (puff.IsEmber)
            {
                var flicker = 0.65f + 0.35f * MathF.Sin(puff.WobblePhase * 4f);
                context.Draw(glow, new Rectangle(x - size / 2, y - size / 2, size, size),
                    puff.Color * (0.7f * fade * flicker));
                continue;
            }

            var halo = (int)(size * 1.45f);
            context.Draw(glow, new Rectangle(x - halo / 2, y - halo / 2, halo, halo),
                puff.Color * (0.22f * fade));
            context.Draw(glow, new Rectangle(x - size / 2, y - size / 2, size, size),
                puff.Color * (0.42f * fade));
        }
    }

    private void Spawn()
    {
        var width = Math.Max(1, ActualBounds.Width);
        var height = Math.Max(1, ActualBounds.Height);
        var isEmber = Rng.Visual.Next(0, 100) < 18;
        var originX = width * 0.55f + Rng.Visual.Next(-6, 7);
        var originY = height * 0.4f + Rng.Visual.Next(-4, 5);
        if (isEmber)
        {
            _puffs.Add(new Puff
            {
                X = originX,
                Y = originY,
                VX = -10f + Rng.Visual.Next(0, 20),
                VY = -28f - Rng.Visual.Next(0, 18),
                Life = 0.45f + Rng.Visual.Next(0, 25) / 100f,
                MaxLife = 0.7f,
                Size = 2f + Rng.Visual.Next(0, 3),
                Grow = -1f,
                Color = CombatIncenseSmokeFx.EmberColor(Tint, Rng.Visual.Next(0, 100) / 100f),
                IsEmber = true,
                Wobble = 10f + Rng.Visual.Next(0, 12),
                WobbleSpeed = 6f,
                WobblePhase = Rng.Visual.Next(0, 628) / 100f
            });
            return;
        }

        var shade = Rng.Visual.Next(0, 100) / 100f;
        _puffs.Add(new Puff
        {
            X = originX,
            Y = originY,
            VX = -14f + Rng.Visual.Next(0, 28),
            VY = -16f - Rng.Visual.Next(0, 14),
            Life = 1.8f + Rng.Visual.Next(0, 90) / 100f,
            MaxLife = 2.7f,
            Size = 12f + Rng.Visual.Next(0, 10),
            Grow = 6f + Rng.Visual.Next(0, 6),
            Color = CombatIncenseSmokeFx.SmokeColor(Tint, shade),
            Wobble = 14f + Rng.Visual.Next(0, 18),
            WobbleSpeed = 1.4f + Rng.Visual.Next(0, 12) / 10f,
            WobblePhase = Rng.Visual.Next(0, 628) / 100f
        });
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

    private sealed class Puff
    {
        public float X;
        public float Y;
        public float VX;
        public float VY;
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
}
