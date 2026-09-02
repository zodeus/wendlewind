namespace Wendlemire.Scenes.MainGameScene.Gui.Widgets.CombatWidgets;

/// <summary>
/// Rising smoke and embers drawn over an incense loadout slot while it is burning.
/// </summary>
internal sealed class IncenseSlotBurnFx : Widget
{
    private const float SpawnInterval = 0.05f;

    private readonly List<Puff> _puffs = [];
    private float _spawnTimer;
    private float _glowPhase;
    private static Texture2D? _glowTexture;
    private static Texture2D? _pixelTexture;

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
        _glowPhase += deltaTime * 6f;
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

            puff.WobblePhase += deltaTime * puff.WobbleSpeed;
            puff.X += puff.VX * deltaTime + MathF.Sin(puff.WobblePhase) * puff.Wobble * deltaTime;
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
        var pixel = PixelTexture();
        if (Burning)
        {
            var pulse = 0.55f + 0.45f * MathF.Sin(_glowPhase);
            var tipX = bounds.X + bounds.Width / 2;
            var tipY = bounds.Y + (int)(bounds.Height * 0.18f);
            var glowSize = (int)(28 + 10 * pulse);
            context.Draw(glow, new Rectangle(
                tipX - glowSize / 2,
                tipY - glowSize / 2,
                glowSize,
                glowSize), Color.Lerp(Tint, Color.White, 0.25f) * (0.55f * pulse));
        }

        foreach (var puff in _puffs)
        {
            var fade = Math.Clamp(puff.Life / puff.MaxLife, 0f, 1f);
            var size = Math.Max(2, (int)puff.Size);
            var x = bounds.X + (int)puff.X;
            var y = bounds.Y + (int)puff.Y;
            if (puff.IsEmber)
            {
                var flicker = 0.7f + 0.3f * MathF.Sin(puff.WobblePhase * 4f);
                context.Draw(pixel, new Rectangle(x - size / 2, y - size / 2, size, size),
                    puff.Color * (fade * flicker));
                var halo = size + 3;
                context.Draw(glow, new Rectangle(x - halo / 2, y - halo / 2, halo, halo),
                    puff.Color * (0.45f * fade * flicker));
            }
            else
            {
                context.Draw(glow, new Rectangle(x - size / 2, y - size / 2, size, size),
                    puff.Color * (0.7f * fade));
            }
        }
    }

    private void Spawn()
    {
        var width = Math.Max(1, ActualBounds.Width);
        var height = Math.Max(1, ActualBounds.Height);
        var isEmber = Rng.Visual.Next(0, 100) < 30;
        var originX = width * 0.5f + Rng.Visual.Next(-4, 5);
        var originY = height * 0.2f + Rng.Visual.Next(-2, 3);
        if (isEmber)
        {
            _puffs.Add(new Puff
            {
                X = originX,
                Y = originY,
                VX = -12f + Rng.Visual.Next(0, 24),
                VY = -70f - Rng.Visual.Next(0, 50),
                Life = 0.7f + Rng.Visual.Next(0, 40) / 100f,
                MaxLife = 1.1f,
                Size = 2f + Rng.Visual.Next(0, 3),
                Grow = 0f,
                Color = Color.Lerp(new Color(255, 170, 50), Tint, 0.35f),
                IsEmber = true,
                Wobble = 18f + Rng.Visual.Next(0, 20),
                WobbleSpeed = 8f,
                WobblePhase = Rng.Visual.Next(0, 628) / 100f
            });
            return;
        }

        _puffs.Add(new Puff
        {
            X = originX,
            Y = originY,
            VX = -16f + Rng.Visual.Next(0, 32),
            VY = -36f - Rng.Visual.Next(0, 28),
            Life = 1.4f + Rng.Visual.Next(0, 80) / 100f,
            MaxLife = 2.2f,
            Size = 10f + Rng.Visual.Next(0, 14),
            Grow = 12f,
            Color = Color.Lerp(new Color(190, 180, 170), Tint, 0.4f),
            Wobble = 22f + Rng.Visual.Next(0, 24),
            WobbleSpeed = 2.4f,
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
