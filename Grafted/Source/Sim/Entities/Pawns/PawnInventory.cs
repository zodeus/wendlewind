using System.Collections;

namespace Grafted.Sim.Entities.Pawns;

public class PawnInventory : IExposable, IEnumerable<Item> {
    public Pawn Pawn;

    public EntityContainer Entities;

    public PawnInventory(Pawn pawn) {
        Pawn = pawn;
        Entities = new EntityContainer();
    }

    public IEnumerator<Item> GetEnumerator() {
        return Entities.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() {
        return GetEnumerator();
    }

    public void ExposeData() {
        ScribeDeep.Look(ref Entities!, "Entities");
    }
}