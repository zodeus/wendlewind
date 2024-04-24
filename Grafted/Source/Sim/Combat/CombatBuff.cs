using Grafted.Sim.Entities;

namespace Grafted.Sim.Combat;

public class CombatBuff {
    public readonly EntityDef Def;
    public readonly Pawn Pawn;
    public int Duration;

    public CombatBuff(EntityDef def, Pawn pawn, int duration) {
        Def = def;
        Pawn = pawn;
        Duration = duration;
    }
}