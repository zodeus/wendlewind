namespace Wendlemire.Sim.Entities.Pawns;

[UsedImplicitly(ImplicitUseTargetFlags.WithInheritors)]
public interface IBodyGenerator
{
    public void Generate(Pawn pawn);

    public static void SetSubstanceOverride(Pawn pawn, SubstanceType substance)
    {
        pawn.Body.AllParts.ForEach(part => part.SetSubstanceOverride(substance));
    }
}