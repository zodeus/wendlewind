using Grafted.Maths;
using Grafted.Sim.Persistence;

namespace Grafted.Sim.Entities.Pawns;

public class PawnMind : IExposable {
    private Pawn _pawn;
    private float _sanity = 1f;
    private float _power = 0f;
    private float _focus = .1f;

    public float Sanity {
        get => _sanity;
        set => _sanity = Mathf.Clamp(value, 0f, 1);
    }

    public float Power {
        get => _power;
        set => _power = Mathf.Clamp(value, 0f, 1);
    }

    public float Focus {
        get => _focus;
        set => _focus = Mathf.Clamp(value, 0f, 1);
    }


    public PawnMind(Pawn pawn) {
        _pawn = pawn;
    }

    public void Tick() {
        if (_pawn.IsFamished) {
            Sanity -= 0.001f;
            Power -= 0.001f;
            Focus -= 0.001f;
        }

        if (SimTime.TicksToHours(_pawn.Body.TicksSinceLastRest) > 22) {
            Sanity -= 0.001f;
            Power -= 0.001f;
            Focus -= 0.001f;
        }

        if (_pawn.Body.IsWarm && _pawn.IsHungry == false && _pawn.IsExhausted == false) {
            Sanity += 0.0005f;
        }
    }

    public void ExposeData() {
        Scribe_Values.Look(ref _sanity, "Sanity");
        Scribe_Values.Look(ref _power, "Power");
        Scribe_Values.Look(ref _focus, "Focus");
    }
}