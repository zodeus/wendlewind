using System.Collections;

namespace Grafted.Sim.Entities.Pawns;

public class PawnInventory : IExposable, IEnumerable<Item>
{
    public Pawn Pawn;

    public EntityContainer Entities;
    public Item this[int i] => (Item)Entities[i];

    public PawnInventory(Pawn pawn)
    {
        Pawn = pawn;
        Entities = new EntityContainer();
    }

    public bool TryAdd(Entity? entity)
    {
        return Entities.TryAdd(entity);
    }

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

    public void Tick(int ticks)
    {
        Entities.Tick();
    }
}