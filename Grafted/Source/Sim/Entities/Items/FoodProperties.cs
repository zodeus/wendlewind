using System.Collections.Generic;
using Grafted.Sim.Entities.Pawns;

namespace Grafted.Sim.Entities.Items;

public class FoodProperties {
    public FoodType FoodType;
    public List<BodyEffectDef> Effects = new();
}