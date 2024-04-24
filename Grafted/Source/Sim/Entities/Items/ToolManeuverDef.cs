namespace Grafted.Sim.Entities.Items;

public class ToolManeuverDef : Def {
    public List<ToolType>? Tools = null;
    public RangeFloat DamageMultiplier = new(1, 1);
}