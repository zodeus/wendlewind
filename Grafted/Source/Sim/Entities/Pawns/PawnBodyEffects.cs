using System.Collections;
using System.Collections.Generic;
using Grafted.Definitions;
using Grafted.Sim.Persistence;

namespace Grafted.Sim.Entities.Pawns;

public class BodyEffectDef : Def {
    public List<AffectedStatRecord>? AffectedStats;
}

public class AffectedStatRecord {
    public StatDef Stat = null!;
    public float? Factor = null;
    public float? Offset = null;
}

public class BodyEffect : IExposable {
    public BodyEffectDef Def = null!;
    public int TicksLeft;
    public bool IsExpired => TicksLeft < 1;

    public void ModifyIfApplicable(StatDef stat, ref float value) {
        if (Def.AffectedStats == null || IsExpired) {
            return;
        }

        foreach (AffectedStatRecord affectedStat in Def.AffectedStats) {
            if (affectedStat.Stat != stat) { continue; }

            if (affectedStat.Factor != null) {
                value = value + value * affectedStat.Factor.Value;
            }

            if (affectedStat.Offset != null) {
                value += affectedStat.Offset.Value;
            }
        }
    }

    public void Tick() {
        TicksLeft--;
    }

    public void ExposeData() {
        Scribe_Defs.Look(ref Def!, "Def");
        Scribe_Values.Look(ref TicksLeft!, "TicksLeft");
    }
}

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