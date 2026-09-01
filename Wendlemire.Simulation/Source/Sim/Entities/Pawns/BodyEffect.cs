namespace Wendlemire.Sim.Entities.Pawns;

public class BodyEffect : IExposable {
    public BodyEffectDef Def = null!;
    public int TicksLeft;
    public bool LastsWholeEncounter;
    public float Power = 1f;
    public bool IsExpired => !LastsWholeEncounter && TicksLeft < 1;

    public void ModifyIfApplicable(StatDef stat, ref float value) {
        if (Def.AffectedStats == null || IsExpired) {
            return;
        }

        foreach (var affectedStat in Def.AffectedStats) {
            if (affectedStat.Stat != stat) { continue; }

            if (affectedStat.Factor != null) {
                value += (value * affectedStat.Factor.Value * Power);
            }

            if (affectedStat.Offset != null) {
                value += affectedStat.Offset.Value * Power;
            }
        }
    }

    public void Tick() {
        if (!LastsWholeEncounter)
        {
            TicksLeft--;
        }
    }

    public void ExposeData() {
        ScribeDefs.Look(ref Def!, "Def");
        ScribeValues.Look(ref TicksLeft, "TicksLeft");
        ScribeValues.Look(ref LastsWholeEncounter, "LastsWholeEncounter");
        ScribeValues.Look(ref Power, "Power", 1f);
    }
}