namespace Wendlewind.PawnLayout;

/// <summary>
/// Health-based tint for body parts. Matches the combat UI
/// (red when destroyed/low, green when healthy, dark when disabled).
/// </summary>
public static class PawnPartTint
{
    private static readonly Color Destroyed = Color.Red;
    private static readonly Color Disabled = new(50, 50, 50);
    private static readonly Color Low = new(170, 0, 0);
    private static readonly Color High = new(65, 120, 64);

    public static Color Get(BodyPart part)
    {
        if (part.IsDestroyed)
        {
            return Destroyed;
        }

        if (!part.HasMobility || !part.IsFunctional)
        {
            return Disabled;
        }

        return Color.Lerp(Low, High, (float)part.HealthPercent);
    }

    public static Color Get(PawnBody body)
    {
        return Color.Lerp(Low, High, (float)body.HitPoints / (float)body.MaxHitPoints);
    }

    public static Color GetBloodColor(float value)
    {
        return Color.Lerp(Destroyed, High, value);
    }
}
