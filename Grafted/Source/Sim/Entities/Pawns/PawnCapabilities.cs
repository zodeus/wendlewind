using System.Linq;

namespace Grafted.Sim.Entities.Pawns;

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