namespace Wendlewind.Sim.Entities.Pawns;

public class BodyEffect : IExposable {
    public BodyEffectDef Def = null!;
    public int TicksLeft;
    public bool IsExpired => TicksLeft < 1;

    public void ModifyIfApplicable(StatDef stat, ref float value) {
        if (Def.AffectedStats == null || IsExpired) {
            return;
        }

        foreach (var affectedStat in Def.AffectedStats) {
            if (affectedStat.Stat != stat) { continue; }

            if (affectedStat.Factor != null) {
                value += (value * affectedStat.Factor.Value);
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
        ScribeDefs.Look(ref Def!, "Def");
        ScribeValues.Look(ref TicksLeft, "TicksLeft");
    }
}