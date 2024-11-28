using System.Collections;
using Grafted.Scenes.MainGameScene.Gui;

namespace Grafted.Sim.Entities;

public class EntityContainer : IEnumerable<Item>, IExposable
{
    private List<Item> _list = new();

    public EntityContainer()
    {
    }

    public void Tick(int ticks)
    {
        for (int index = _list.Count - 1; index >= 0; index--)
        {
            Entity entity = _list[index];
            entity.Tick(ticks);
            if (entity.IsDestroyed)
            {
                _list.RemoveAt(index);
            }
        }
    }

    public Item this[int i] => _list[i];

    IEnumerator<Item> IEnumerable<Item>.GetEnumerator()
    {
        return GetEnumerator();
    }

    public IEnumerator<Item> GetEnumerator()
    {
        return _list.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void Remove(Item item)
    {
        _list.Remove(item);
        item.Container = null;
    }

    public bool TryAdd(Item item, int amount)
    {
        Item splitItem = item.SplitStack(amount);
        if (TryAdd(splitItem))
        {
            return true;
        }

        TryAdd(splitItem);

        return false;
    }

    public bool TryAdd(Item? item)
    {
        if (item == null)
        {
            Log.Warning("Tried to add null item :(");
            return false;
        }

        //todo there is a bug here where StackSize can/will exceed StackLimit, not doing enough
        if (item.IsStackable)
        {
            for (int i = 0; i < _list.Count; i++)
            {
                if (_list[i].Def != item.Def) continue;
                //todo there is a bug here where StackSize can/will exceed StackLimit, not doing enough
                _list[i].StackSize += item.StackSize;
                item.StackSize = 0;
                item.Container?.Remove(item);
                item.Destroy();
                return true;
            }
        }

        item.Container?.Remove(item);
        item.Container = this;
        _list.Add(item);
        return true;
    }

    public bool Contains(ItemDef def, int amountWanted)
    {
        int amount = 0;
        foreach (Item item in _list)
        {
            if (item.Def == def)
            {
                amount += item.StackSize;
            }
        }

        return amount >= amountWanted;
    }

    public Item? Take(EntityDef def, int amount)
    {
        foreach (Item item in _list)
        {
            if (item.Def == def)
            {
                return Take(item, amount);
            }
        }

        return null;
    }

    public Item? Take(Item item, int amount)
    {
        if (_list.Contains(item) == false)
        {
            Log.Error("ItemContainer doesn't contain " + item);
            return null;
        }

        if (amount > item.StackSize)
        {
            Log.Error("Tried to get " + amount + " of " + item + " while only having " + item.StackSize);
            amount = item.StackSize;
        }

        if (amount == item.StackSize)
        {
            Remove(item);
            return item;
        }

        Item splitItem = item.SplitStack(amount);

        return splitItem;
    }

    public void Clear()
    {
        _list.Clear();
    }

    public int AmountOf(ItemDef? itemDef)
    {
        int amount = 0;
        foreach (Item item in _list)
        {
            if (item.Def == itemDef)
            {
                amount += item.StackSize;
            }
        }

        return amount;
    }

    public void ExposeData()
    {
        ScribeCollections.Look(ref _list!, "Container", LookMode.Deep);
        if (Scribe.State == ScribeState.PostLoadInitialization)
        {
            for (int i = 0; i < _list.Count; i++)
            {
                //if (_container[i] != null) {
                _list[i].Container = this;
                //}
            }
        }
    }
}