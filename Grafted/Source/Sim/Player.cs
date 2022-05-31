using Grafted.Sim.Entities.Pawns;
using Grafted.Sim.Persistence;

namespace Grafted.Sim;

public class Player : IExposable {
    public Pawn Pawn = null!;

    public string Label => "You"; //Pawn.Label;

    public void Tick() {
        Pawn.Tick();
    }

    public void ExposeData() {
        Scribe_Deep.Look(ref Pawn!, "Pawn");
    }
}