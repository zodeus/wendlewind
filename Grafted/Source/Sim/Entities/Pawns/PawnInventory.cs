using System.Collections;

namespace Grafted.Sim.Entities.Pawns;

public class PawnInventory : IExposable, IEnumerable<Item>
{
    public Pawn Pawn;

    public EntityContainer Entities;
    public Item this[int i] => (Item)Entities[i];

    public List<Item> Trinkets => Entities.AsItems().Where(i => i.ItemDef.ItemType == ItemType.Trinket).ToList();
    public List<Item> Resources => Entities.AsItems().Where(i => i.ItemDef.ItemType == ItemType.Resource).ToList();

    public PawnInventory(Pawn pawn)
    {
        Pawn = pawn;
        Entities = new EntityContainer();
    }

    public bool TryAdd(Entity? entity)
    {
        return Entities.TryAdd(entity);
    }

    public int AmountOf(ItemDef def) => Entities.AmountOf(def);

    public IEnumerator<Item> GetEnumerator()
    {
        return Entities.AsEnumerable().AsItems().GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void ExposeData()
    {
        ScribeDeep.Look(ref Entities!, "Entities");
    }

    public void Tick()
    {
        Entities.Tick();
    }
}