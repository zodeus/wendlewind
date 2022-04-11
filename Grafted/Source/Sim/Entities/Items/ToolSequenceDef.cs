using System.Collections.Generic;
using Grafted.Definitions;
using Grafted.Maths;

namespace Grafted.Sim.Entities.Items;

public class ToolSequenceDef : Def {
    public RangeFloat DamageMultiplier = new(1, 1);
    public int SequencePoints = 0;
    public List<ToolManeuverDef> Maneuvers = new();
    public float VisualWaitTimeMultiplier = 1;
    public int Cooldown = 1;
    
    public int TotalSequencePoints => SequencePoints;
}