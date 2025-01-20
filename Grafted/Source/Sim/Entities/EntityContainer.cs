using System.Collections;
using Grafted.Scenes.MainGameScene.Gui;

namespace Grafted.Sim.Entities;

public partial class EntityContainer : IEnumerable<Entity>, IExposable
{
    private List<Entity> _list = new();

    public void Tick()
    {
        for (var index = _list.Count - 1; index >= 0; index--)
        {
            var entity = _list[index];
            entity.Tick();
            if (entity.IsDestroyed)
            {
                _list.RemoveAt(index);
            }
        }
    }

    public Entity this[int i] => _list[i];

    public bool TryAdd(Entity? entity)
    {
        if (entity == null)
        {
            Log.Warning("Tried to add null entity");
            return false;
        }

        if (entity is Item item)
        {
            return AddItem(item);
        }

        entity.EjectFromContainer();
        entity.EjectedFromContainer += OnContainerEject;
        entity.Destroyed += OnEntityDestroyed;
        _list.Add(entity);

        return true;
    }

    private void OnEntityDestroyed(Entity entity)
    {
        Remove(entity);
    }

    private void OnContainerEject(Entity entity)
    {
        Remove(entity);
    }

    public void Remove(Entity entity)
    {
        entity.EjectedFromContainer -= OnContainerEject;
        entity.Destroyed -= OnEntityDestroyed;
        _list.Remove(entity);

        if (entity is Item item)
        {
            ItemRemoved?.Invoke(item);
        }
    }

    public void Clear()
    {
        for (var i = _list.Count - 1; i >= 0; i--)
        {
            Remove(_list[i]);
        }
    }

    IEnumerator<Entity> IEnumerable<Entity>.GetEnumerator()
    {
        return GetEnumerator();
    }

    public IEnumerator<Entity> GetEnumerator()
    {
        return _list.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void ExposeData()
    {
        ScribeCollections.Look(ref _list!, "Container", LookMode.Deep);
        if (Scribe.State == ScribeState.PostLoadInitialization)
        {
            foreach (var entity in _list)
            {
                entity.EjectedFromContainer += OnContainerEject;
                entity.Destroyed += OnEntityDestroyed;
            }
        }
    }
}

public partial class EntityContainer
{
    public event Action<Item>? ItemAdded;
    public event Action<Item>? ItemRemoved;
    public event Action<Item>? ItemStackSizeChanged;

    private bool AddItem(Item item)
    {
        if (item.IsStackable)
        {
            foreach (var mergeEntity in _list)
            {
                if (mergeEntity is not Item mergeItem || mergeItem.Def != item.Def) continue;
                mergeItem.StackSize += item.StackSize; // todo currently no overfill handling
                item.StackSize = 0;
                item.EjectFromContainer();
                item.Destroy();

                ItemStackSizeChanged?.Invoke(mergeItem);
                return true;
            }
        }

        item.EjectFromContainer();
        item.EjectedFromContainer += OnContainerEject;
        item.Destroyed += OnEntityDestroyed;

        _list.Add(item);
        ItemAdded?.Invoke(item);
        return true;
    }

    public bool TryAdd(Item item, int amount)
    {
        var splitItem = item.SplitStack(amount);
        if (TryAdd(splitItem))
        {
            return true;
        }

        TryAdd(splitItem);

        return false;
    }

    public bool Contains(ItemDef def, int amountWanted)
    {
        var amount = 0;
        foreach (var entity in _list)
        {
            if (entity is Item item && item.Def == def)
            {
                amount += item.StackSize;
            }
        }

        return amount >= amountWanted;
    }

    public bool Contains(ResourceCount resourceCount)
    {
        return Contains(resourceCount.Item, resourceCount.Count);
    }

    public Item? Take(EntityDef def, int amount)
    {
        foreach (var entity in _list)
        {
            if (entity is Item item && item.Def == def)
            {
                return Take(item, amount);
            }
        }

        return null;
    }

    // ReSharper disable Unity.PerformanceAnalysis
    public Item? Take(Item item, int amount)
    {
        if (_list.Contains(item) == false)
        {
            Log.Error("EntityContainer doesn't contain " + item);
            return null;
        }

        if (amount > item.StackSize)
        {
            Log.Warning("Tried to get " + amount + " of " + item + " while only having " + item.StackSize);
            amount = item.StackSize;
        }

        if (amount == item.StackSize)
        {
            Remove(item);
            return item;
        }

        var splitItem = item.SplitStack(amount);
        ItemStackSizeChanged?.Invoke(item);

        return splitItem;
    }

    public Item? Take(ResourceCount resource)
    {
        return Take(resource.Item, resource.Count);
    }

    // ReSharper disable Unity.PerformanceAnalysis
    public int AmountOf(ItemDef? itemDef)
    {
        if (itemDef == null)
        {
            Log.Warning("Called EntityContainer.AmountOf with null itemDef");
            return 0;
        }

        var amount = 0;
        foreach (var entity in _list)
        {
            if (entity is Item item && item.Def == itemDef)
            {
                amount += item.StackSize;
            }
        }

        return amount;
    }
}

public static class EntityContainerExtensions
{
    public static IEnumerable<Item> AsItems(this IEnumerable<Entity> entities)
    {
        return entities.Where(e => e is Item).Cast<Item>();
    }
}