using System.Collections.Generic;
using Grafted.Definitions;

namespace Grafted.Sim.Entities.Pawns;

public class BodyPartSocketDef : Def {
    public bool IsExternal = false;
    public List<BodyPartType> AllowedBodyPartTypes = new();
}