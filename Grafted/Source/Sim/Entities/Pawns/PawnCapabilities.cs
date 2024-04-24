namespace Grafted.Sim.Entities.Pawns;

public class PawnCapabilities : IExposable {
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

    public float Breathing {
        get {
            int lungs = _pawn.Body.AllParts.Count(p => p.BodyPartDef.BodyPartType == BodyPartType.Lung && p.IsFunctional);
            return lungs switch {
                2 => 1f,
                1 => .5f,
                _ => .0f
            };
        }
    }

    public void ExposeData() { }
}