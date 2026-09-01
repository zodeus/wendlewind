namespace Wendlemire.Scenes.MainGameScene.Gui.Widgets.PawnRenderer;

internal enum CombatFloaterPattern
{
    Drift,
    Arc,
    Pulse,
    Flicker
}

internal readonly record struct CombatFloaterSample(Vector2 Offset, float Opacity, float Scale);

internal sealed class CombatFloaterStyle
{
    public CombatFloaterPattern Pattern { get; init; }
    public Vector2 Velocity { get; init; }
    public float Gravity { get; init; }

    public static CombatFloaterStyle CreateRandom()
    {
        var patterns = Enum.GetValues<CombatFloaterPattern>();
        var angle = MathHelper.ToRadians(Rng.Visual.Next(-38, 39));
        var speed = Rng.Visual.Next(150, 220);
        var pattern = patterns[Rng.Visual.Next(patterns.Length)];
        return new CombatFloaterStyle
        {
            Pattern = pattern,
            Velocity = new Vector2(MathF.Sin(angle), -MathF.Cos(angle)) * speed,
            Gravity = pattern == CombatFloaterPattern.Arc ? Rng.Visual.Next(90, 160) : 0f
        };
    }

    public CombatFloaterSample Evaluate(float elapsed, float duration)
    {
        var progress = duration <= 0f ? 1f : Math.Clamp(elapsed / duration, 0f, 1f);
        var offset = Velocity * elapsed + new Vector2(0f, 0.5f * Gravity * elapsed * elapsed);
        return Pattern switch
        {
            CombatFloaterPattern.Pulse => new CombatFloaterSample(
                offset,
                1f,
                0.92f + 0.28f * MathF.Sin(progress * MathF.PI)),
            CombatFloaterPattern.Flicker => new CombatFloaterSample(
                offset,
                (int)(elapsed * 12f) % 2 == 0 ? 1f : 0.22f,
                1f),
            _ => new CombatFloaterSample(offset, 1f, 1f)
        };
    }
}

internal static class CombatFloaterDraw
{
    public static void Draw(RenderContext context, DynamicSpriteFont font, string text, Vector2 center, Color color, float opacity, float scale, Vector2? measuredSize = null)
    {
        scale = Math.Max(scale, 0.05f);
        var tint = color * opacity;
        var size = measuredSize ?? font.MeasureString(text);
        var pos = center - size * scale * 0.5f;

        DrawAt(context, font, text, pos + new Vector2(1f, 1f) * scale, Color.Black * opacity * 0.8f, scale);
        DrawAt(context, font, text, pos, tint, scale);
    }

    private static void DrawAt(RenderContext context, DynamicSpriteFont font, string text, Vector2 pos, Color color, float scale)
    {
        if (Math.Abs(scale - 1f) < 0.02f)
        {
            context.DrawString(font, text, pos, color);
            return;
        }

        context.DrawString(font, text, pos, color, new Vector2(scale));
    }
}
