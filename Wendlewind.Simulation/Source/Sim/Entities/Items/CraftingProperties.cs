namespace Wendlewind.Sim.Entities.Items;

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

    public bool Craft(Pawn pawn, ItemDef itemDef, int times = 1)
    {
        List<Item> resourcesTaken = [];
        foreach (var resource in ResourceRequirements)
        {
            var scaledResource = new ResourceCount { Item = resource.Item, Count = resource.Count * times };
            var resourceToUse = pawn.Inventory.Take(scaledResource);

            if (resourceToUse == null)
            {
                foreach (var resourceTaken in resourcesTaken)
                {
                    pawn.Inventory.TryAdd(resourceTaken);
                }

                return false;
            }

            resourcesTaken.Add(resourceToUse);
            if (resourceToUse.StackSize < scaledResource.Count)
            {
                return false;
            }
        }

        foreach (var resourceTaken in resourcesTaken)
        {
            resourceTaken.Destroy();
        }

        var totalAmountCrafted = AmountProduced * times;
        pawn.Inventory.TryAdd(pawn.Context.Factory.CreateEntity<Item>(itemDef, totalAmountCrafted));

        // Notify achievement system
        pawn.Context.Achievements.OnItemCrafted(pawn, itemDef, totalAmountCrafted);

        return true;
    }
}