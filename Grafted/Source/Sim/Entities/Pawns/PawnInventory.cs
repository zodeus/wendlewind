using System.Collections;

namespace Grafted.Sim.Entities.Pawns;

public class PawnInventory : IExposable, IEnumerable<Item>
{
    public Pawn Pawn;

    private EntityContainer _entities;
    
    public event Action<Item>? ItemAdded
    {
        add => _entities.ItemAdded += value;
        remove => _entities.ItemAdded -= value;
    }
    
    public event Action<Item>? ItemRemoved
    {
        add => _entities.ItemRemoved += value;
        remove => _entities.ItemRemoved -= value;
    }
    
    public event Action<Item>? ItemStackSizeChanged
    {
        add => _entities.ItemStackSizeChanged += value;
        remove => _entities.ItemStackSizeChanged -= value;
    }
    
    public Item this[int i] => (Item)_entities[i];

    public List<Item> Trinkets => _entities.AsItems().Where(i => i.ItemDef.ItemType == ItemType.Trinket).ToList();
    public List<Item> Resources => _entities.AsItems().Where(i => i.ItemDef.ItemType == ItemType.Resource).ToList();

    public PawnInventory(Pawn pawn)
    {
        Pawn = pawn;
        _entities = new EntityContainer();
    }

    public bool TryAdd(Entity? entity)
    {
        return _entities.TryAdd(entity);
    }

    public int AmountOf(ItemDef def) => _entities.AmountOf(def);
    
    public void Remove(Entity entity) => _entities.Remove(entity);
    
    public bool Contains(ResourceCount resource) => _entities.Contains(resource);
    
    public bool Contains(ItemDef def) => _entities.Contains(def, 1);
    
    public Item? Take(EntityDef def, int amount) => _entities.Take(def, amount);
    
    /// <summary>
    /// Exposes the underlying container for widget binding (events + iteration).
    /// Prefer using proxy methods for direct manipulation.
    /// </summary>
    //public EntityContainer AsEntityContainer => _entities;

    public IEnumerator<Item> GetEnumerator()
    {
        return _entities.AsEnumerable().AsItems().GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void ExposeData()
    {
        ScribeDeep.Look(ref _entities!, "Entities");
    }

    public void Tick()
    {
        _entities.Tick();
    }
    
    public Item? Take(ResourceCount resource)
    {
        return _entities.Take(resource.Item, resource.Count);
    }
}