namespace Wendlewind.Sim.Entities.Pawns;

public class MealPlan : IExposable
{
    public const float NutritionBudget = 1f;

    private List<Item> _items = [];

    public IReadOnlyList<Item> Items => _items;

    public MealPlan()
    {
    }

    public MealPlan(Pawn pawn)
    {
    }

    public float AssignedNutrition
    {
        get
        {
            var total = 0f;
            foreach (var item in _items)
            {
                if (item is { IsDestroyed: false })
                {
                    total += item.GetStatValue(Defs.Stats.NutritionalValue);
                }
            }

            return total;
        }
    }

    public bool TryAdd(Item item)
    {
        Prune();
        if (item.ItemDef.FoodProperties == null || item.IsDestroyed)
        {
            return false;
        }

        if (_items.Contains(item))
        {
            return false;
        }

        var nutrition = item.GetStatValue(Defs.Stats.NutritionalValue);
        if (AssignedNutrition + nutrition > NutritionBudget + 0.001f)
        {
            return false;
        }

        _items.Add(item);
        return true;
    }

    public void Remove(Item item)
    {
        _items.Remove(item);
    }

    public void Prune()
    {
        _items.RemoveAll(i => i == null || i.IsDestroyed || i.StackSize < 1);
    }

    public void ExposeData()
    {
        ScribeCollections.Look(ref _items!, "Items", LookMode.Reference);
        _items ??= [];
    }
}
