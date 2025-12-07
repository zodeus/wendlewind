namespace Grafted.Sim.Entities.Items;

public class CraftingProperties
{
    public int AmountProduced = 1;
    public List<ItemDef>? RequiredTrinkets = null;
    public List<ResourceCount> ResourceRequirements = new();

    public bool CanCraft(Pawn pawn)
    {
        if (RequiredTrinkets != null && RequiredTrinkets.Except(pawn.Inventory.Trinkets.Select(t => t.Def)).Any())
        {
            return false;
        }

        foreach (var resource in ResourceRequirements)
        {
            if (pawn.Inventory.Contains(resource) == false)
            {
                return false;
            }
        }

        return true;
    }
}