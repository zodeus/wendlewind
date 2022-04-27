using System.Collections.Generic;
using Grafted.Definitions;
using Grafted.Maths;

namespace Grafted.Sim.Entities.Items;

public class ToolManeuverDef : Def {
    public List<ToolType>? Tools = null;
    public RangeFloat DamageMultiplier = new RangeFloat(1, 1);
}