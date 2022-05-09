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
        Entities = new EntityContainer((int) pawn.GetStatValue(Defs.Stats.MaxCarryWeight));
    }

    public void Tick() {
        Entities.Tick();
    }

    public void ExposeData() { }

    public IEnumerator<Item> GetEnumerator() {
        return Entities.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() {
        return GetEnumerator();
    }
}