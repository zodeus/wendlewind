using Grafted.Sim.Entities.Pawns;

namespace Grafted.Sim.Combat;

public class PawnTurnData {
    public PawnTurnData(Pawn pawn, int sequencePoints) {
        Pawn = pawn;
        TotalSequencePoints = sequencePoints;
        AvailableSequencePoints = sequencePoints;
    }

    public Pawn Pawn { get; }
    public int TotalSequencePoints { get; }
    public int AvailableSequencePoints { get; set; }
    public bool WantsToRetreat { get; set; }
}