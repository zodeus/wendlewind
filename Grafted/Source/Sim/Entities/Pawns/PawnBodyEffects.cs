using System.Collections;
using System.Collections.Generic;
using Grafted.Sim.Persistence;

namespace Grafted.Sim.Entities.Pawns;

public class PawnBodyEffects : IEnumerable<BodyEffect>, IExposable {
    private List<BodyEffect> _effects = new();

    public PawnBodyEffects(Pawn pawn) { }

    IEnumerator<BodyEffect> IEnumerable<BodyEffect>.GetEnumerator() {
        return _effects.GetEnumerator();
    }

    public IEnumerator GetEnumerator() {
        return _effects.GetEnumerator();
    }

    public void TryApplyEffect(BodyEffect effect) {
        _effects.Add(effect);
    }

    public void Tick() {
        for (int index = _effects.Count - 1; index >= 0; index--) {
            BodyEffect effect = _effects[index];
            effect.Tick();
            if (effect.IsExpired) {
                _effects.Remove(effect);
            }
        }
    }

    public void ExposeData() {
        Scribe_Collections.Look(ref _effects!, "_effects", LookMode.Deep);
    }
}