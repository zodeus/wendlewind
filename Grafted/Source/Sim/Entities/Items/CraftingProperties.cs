namespace Grafted.Sim.Entities.Items;

public class CraftingProperties
{
    public int AmountProduced = 1;
    public List<ItemDef>? RequiredTrinkets = null;
    public List<ResourceCount> ResourceRequirements = new();

    public bool CanCraft(Pawn pawn) => CanCraft(pawn, 1);

    public bool CanCraft(Pawn pawn, int times)
    {
        if (RequiredTrinkets != null && RequiredTrinkets.Except(pawn.Inventory.Trinkets.Select(t => t.Def)).Any())
        {
            return false;
        }

        foreach (var resource in ResourceRequirements)
        {
            if (pawn.Inventory.AmountOf(resource.Item) < resource.Count * times)
            {
                return false;
            }
        }

        return true;
    }
}