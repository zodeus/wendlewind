using System.Collections.Generic;
using Grafted.Sim.Entities.Pawns;

namespace Grafted.Sim.Entities.Items;

public class FoodProperties {
    public FoodType FoodType;
    public List<BodyEffectRecord> Effects = new();
}

public class BodyEffectRecord {
    public BodyEffectDef Def = null!;
    public int DurationInMinutes = -1;
}