namespace Wendlemire.Sim.Entities.Pawns;

public class MealPlan : IExposable
{
    public const int MaxSlots = 4;

    private List<Item> _items = [];

    public IReadOnlyList<Item> Items => _items;

    public MealPlan()
    {
    }

    public MealPlan(Pawn pawn)
    {
    }

    public bool CanFit(Item item)
    {
        if (item.ItemDef.FoodProperties == null || item.IsDestroyed || item.StackSize < 1)
        {
            return false;
        }

        if (_items.Count >= MaxSlots)
        {
            return false;
        }

        var slotted = 0;
        for (var i = 0; i < _items.Count; i++)
        {
            if (_items[i] == item)
            {
                slotted++;
            }
        }

        return slotted < item.StackSize;
    }

    public bool TryAdd(Item item)
    {
        Prune();
        if (!CanFit(item))
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

    public void RemoveAt(int index)
    {
        if (index < 0 || index >= _items.Count)
        {
            return;
        }

        _items.RemoveAt(index);
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
