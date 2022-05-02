using System.Collections.Generic;

namespace Grafted.Sim.Entities.Items;

public class CraftingProperties {
    public bool CanCraft = false;
    public int MinutesToMake;
    //public int Trinkets = 0;
    public int AmountProduced = 1;
    public int MinTier = 1;
    public List<ItemDef>? RequiredTools = null;
    public List<ResourceCount> ResourceRequirements = new();
}