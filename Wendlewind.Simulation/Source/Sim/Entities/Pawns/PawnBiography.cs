namespace Wendlewind.Sim.Entities.Pawns;

public class PawnBiography : IExposable {
    public Gender Gender;
    public string Name = "NoNameNungus";

    public PawnBiography(Pawn pawn) { }

    public void ExposeData() {
        ScribeValues.Look(ref Name!, "Name");
        ScribeValues.Look(ref Gender, "Gender");
    }
}