using System.Collections.Generic;

namespace Grafted.Sim.Entities.Items;

public class FoodProperties {
    public FoodType FoodType;
    public List<BodyEffectRecord> Effects = new();
}