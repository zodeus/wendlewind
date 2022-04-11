using Grafted.Sim.Persistence;

namespace Grafted.Sim.Entities.Pawns;

public class PawnBrain : IExposable {
    private Pawn _pawn;

    public PawnCombatSettings CombatSettings;
    public PawnBrain(Pawn pawn) {
        _pawn = pawn;
        
        CombatSettings = new PawnCombatSettings();
    }

    public void Tick() { }

    public void ExposeData() { }
}