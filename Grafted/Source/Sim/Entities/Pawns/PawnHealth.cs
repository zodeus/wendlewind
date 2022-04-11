using System.Linq;
using Grafted.Sim.Persistence;

namespace Grafted.Sim.Entities.Pawns;

public class PawnHealth : IExposable {
    private Pawn _pawn;
    public PawnCapabilities Capabilities;

    public PawnHealth(Pawn pawn) {
        _pawn = pawn;
        Capabilities = new PawnCapabilities(pawn);
    }

    public void ExposeData() { }
    public void Tick() { }
}

public class PawnCapabilities {
    private readonly Pawn _pawn;

    public PawnCapabilities(Pawn pawn) {
        _pawn = pawn;

    }

    public float Sight {
        get {
            int eyes = _pawn.Body.AllExternalParts.Count(p => p.Type == BodyPartType.Eye && p.IsFunctional);
            return eyes switch {
                2 => 1f,
                1 => .6f,
                _ => .05f
            };
        }
    }
}