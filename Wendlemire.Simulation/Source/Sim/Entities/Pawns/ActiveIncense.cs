namespace Wendlemire.Sim.Entities.Pawns;

public class ActiveIncense : IExposable
{
    public BodyEffectDef Def = null!;
    public int EncountersRemaining;
    public string? SourceMoniker;
    public float AfterSeconds;
    public bool FiredThisEncounter;

    public bool ShouldFire(int tick, int slotIndex) =>
        !FiredThisEncounter && tick >= IncenseProperties.GetIgniteTick(slotIndex);

    public int GetDurationInTicks()
    {
        if (SourceMoniker == null)
        {
            return GameContext.TicksPerSecond * 20;
        }

        var def = DefRepository<ItemDef>.GetByMoniker(SourceMoniker, raiseError: false);
        return def?.IncenseProperties?.GetDurationInTicks() ?? GameContext.TicksPerSecond * 20;
    }

    public void ExposeData()
    {
        ScribeDefs.Look(ref Def!, "Def");
        ScribeValues.Look(ref EncountersRemaining, "EncountersRemaining");
        ScribeValues.Look(ref SourceMoniker, "SourceMoniker");
        ScribeValues.Look(ref AfterSeconds, "AfterSeconds");
        ScribeValues.Look(ref FiredThisEncounter, "FiredThisEncounter");
    }
}
