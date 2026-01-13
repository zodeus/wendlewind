namespace Grafted.Sim.Entities.Items;

public class FoodProperties {
    public FoodType FoodType;
    public List<BodyEffectRecord> Effects = new();

    public static Color GetNutritionColor(float value)
    {
        return value switch
        {
            >= .3f => new Color(100, 220, 100),   // High nutrition - green
            >= .2f => new Color(220, 220, 100),
            >= .1f => new Color(200, 150, 100),
            >= .05f => new Color(150, 100, 50),
            _ => new Color(80, 80, 80)
        };
    }

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
        return new Color(100, 180, 220); // Positive/neutral effect - blue
    }
}