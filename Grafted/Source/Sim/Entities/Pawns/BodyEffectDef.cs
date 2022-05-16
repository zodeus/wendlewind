using System.Collections.Generic;
using Grafted.Definitions;

namespace Grafted.Sim.Entities.Pawns;

public class BodyEffectDef : Def {
    public List<AffectedStatRecord>? AffectedStats;
}

public class AffectedStatRecord {
    public StatDef Stat = null!;
    public float? Factor = null;
    public float? Offset = null;
}