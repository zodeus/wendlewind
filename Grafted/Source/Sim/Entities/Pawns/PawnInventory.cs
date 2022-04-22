using System.Collections;
using System.Collections.Generic;
using Grafted.Definitions;
using Grafted.Sim.Entities.Items;
using Grafted.Sim.Persistence;

namespace Grafted.Sim.Entities.Pawns;

public class PawnInventory : IExposable, IEnumerable<Item> {
    public Pawn Pawn;

    public ItemContainer Items;

    public PawnInventory(Pawn pawn) {
        Pawn = pawn;
        Items = new ItemContainer((int) pawn.GetStatValue(Defs.Stats.MaxCarryWeight));
    }

    public void Tick() {
        Items.Tick();
    }

    public void ExposeData() { }

    public IEnumerator<Item> GetEnumerator() {
        return Items.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() {
        return GetEnumerator();
    }
}