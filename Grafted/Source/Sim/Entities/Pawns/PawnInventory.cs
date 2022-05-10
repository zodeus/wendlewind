using System.Collections;
using System.Collections.Generic;
using Grafted.Definitions;
using Grafted.Sim.Entities.Items;
using Grafted.Sim.Persistence;

namespace Grafted.Sim.Entities.Pawns;

public class PawnInventory : IExposable, IEnumerable<Item> {
    public Pawn Pawn;

    public EntityContainer Entities;

    public PawnInventory(Pawn pawn) {
        Pawn = pawn;
        Entities = new EntityContainer(pawn.MaxCarryWeight);
    }

    public void Tick() {
        if (Pawn.MaxCarryWeight != Entities.MaxWeight) {
            Entities.UpdateMaxWeight(Pawn.MaxCarryWeight);
        }

        Entities.Tick();
    }

    public IEnumerator<Item> GetEnumerator() {
        return Entities.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() {
        return GetEnumerator();
    }

    public void ExposeData() {
        Scribe_Deep.Look(ref Entities!, "Entities");
    }
}