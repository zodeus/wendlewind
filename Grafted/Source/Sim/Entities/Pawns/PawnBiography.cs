using System.Collections.Generic;
using Grafted.Sim.Persistence;
using Grafted.Utils;

namespace Grafted.Sim.Entities.Pawns;

public class PawnBiography : IExposable {
    public Pawn Pawn;
    public Gender Gender;
    public string Name;

    public PawnBiography(Pawn pawn) {
        var names = new List<string> {
            "Meat Man", "Broom Boy", "Man with Bucket", "Harmless Human"
        };
        Pawn = pawn;
        Name = pawn.PawnDef.PawnType == PawnType.Player ? "Tillbury" : names.RandomElement();
    }

    public void ExposeData() {
        throw new System.NotImplementedException();
    }
}