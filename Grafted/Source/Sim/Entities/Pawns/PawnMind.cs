using Grafted.Sim.Persistence;

namespace Grafted.Sim.Entities.Pawns;

public class PawnBrain : IExposable {
    private Pawn _pawn;

    public PawnBrain(Pawn pawn) {
        _pawn = pawn;
    }

    public void Tick() { }

    public void ExposeData() { }
}