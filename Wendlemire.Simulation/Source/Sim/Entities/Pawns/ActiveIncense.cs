namespace Wendlemire.Sim.Entities.Pawns;

public class ActiveIncense : IExposable
{
    public BodyEffectDef Def = null!;
    public int EncountersRemaining;
    public string? SourceMoniker;

    public void ExposeData()
    {
        ScribeDefs.Look(ref Def!, "Def");
        ScribeValues.Look(ref EncountersRemaining, "EncountersRemaining");
        ScribeValues.Look(ref SourceMoniker, "SourceMoniker");
    }
}
