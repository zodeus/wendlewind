namespace Wendlemire.Sim.Entities.Pawns;

public class BodyEffectDef : Def
{
    public string? TexturePath;
    public List<AffectedStatRecord>? AffectedStats;
    public string? Notes;
}

public class BodyStanceDef : Def
{
    public string? TexturePath;
    public List<AffectedStatRecord>? AffectedStats;
}
