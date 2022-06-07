using Grafted.Sim.Entities.Pawns;

namespace Grafted.Sim.Combat;

public class PawnTurnData {
    public PawnTurnData(Pawn pawn) {
        Pawn = pawn;
        TotalSequencePoints = pawn.SequencePoints;
        AvailableSequencePoints = TotalSequencePoints;
        StartingBloodLevel = pawn.Body.BloodAmount;
    }

    public Pawn Pawn { get; }
    public int TotalSequencePoints { get; set; }
    public int AvailableSequencePoints { get; set; }
    public float StartingBloodLevel { get; set; }
}