namespace Wendlewind.Sim.Entities.Pawns;

public class BodyPartSocketDef : Def {
    public bool IsExternal = false;
    public BodyPartPosition? Position;
    public List<BodyPartType> AllowedBodyPartTypes = new();
}

public enum BodyPartPosition
{
    Left,
    Right,
    FrontLeft,
    FrontRight,
    MiddleLeft,
    MiddleRight,
    RearLeft,
    RearRight,

    //Fingers
    Index,
    Middle,
    Ring,
    Little,
    M1,
    M2,
    M3,
    M4,
    M5,
    M6,
    M7,
    M8,
    M9,
    M10
}