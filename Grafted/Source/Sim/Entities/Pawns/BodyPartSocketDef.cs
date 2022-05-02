using System.Collections.Generic;
using Grafted.Definitions;

namespace Grafted.Sim.Entities.Pawns;

public class BodyPartSocketDef : Def {
    public bool IsExternal = false;
    public BodyPartPosition? Position;
    public List<BodyPartType> AllowedBodyPartTypes = new();
}

public enum BodyPartPosition {
    Left,
    Right,
    FrontLeft,
    FrontRight,
    RearLeft,
    RearRight,
    
    //Fingers
    Index,
    Middle,
    Ring,
    Little
}