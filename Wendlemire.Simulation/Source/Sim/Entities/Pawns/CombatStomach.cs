namespace Wendlemire.Sim.Entities.Pawns;

public class IngestedFood : IExposable
{
    public ItemDef Def = null!;

    public void ExposeData()
    {
        ScribeDefs.Look(ref Def!, "Def");
    }
}

public class CombatStomach : IExposable
{
    private List<IngestedFood> _items = [];

    public IReadOnlyList<IngestedFood> Items => _items;

    public CombatStomach()
    {
    }

    public CombatStomach(Pawn pawn)
    {
    }

    public bool TryAdd(ItemDef def)
    {
        if (def?.FoodProperties == null || _items.Count >= MealPlan.MaxSlots)
        {
            return false;
        }

        _items.Add(new IngestedFood { Def = def });
        return true;
    }

    public bool TryRemoveAt(int index)
    {
        if (index < 0 || index >= _items.Count)
        {
            return false;
        }

        _items.RemoveAt(index);
        return true;
    }

    public void Clear()
    {
        _items.Clear();
    }

    public void ExposeData()
    {
        ScribeCollections.Look(ref _items!, "Items", LookMode.Deep);
        _items ??= [];
    }
}
