namespace Wendlewind.Sim.Entities.Items;

public class IncenseProperties
{
    public BodyEffectRecord Effect = null!;

    public static Color GetEffectColor(BodyEffectDef def)
    {
        if (def.AffectedStats != null)
        {
            foreach (var stat in def.AffectedStats)
            {
                if (stat.Offset < 0 || stat.Factor < 1f)
                    return new Color(220, 100, 100); // Negative effect - red
            }
        }
        return new Color(255, 200, 120); // Positive/neutral effect - warm glow
    }
}
