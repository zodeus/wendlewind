using Grafted.Sim.Persistence;

namespace Grafted.Sim.Entities.Pawns;

public class PawnBiography : IExposable {
    public Gender Gender;
    public string Name = "NoNameNungus";

    public PawnBiography(Pawn pawn) { }

    public void ExposeData() {
        Scribe_Values.Look(ref Name!, "Name");
        Scribe_Values.Look(ref Gender, "Gender");
    }
}