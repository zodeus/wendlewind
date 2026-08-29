namespace Wendlewind.Sim.Entities.Pawns;

public interface IBodyGenerator
{
    public void Generate(Pawn pawn);

    public static void SetSubstanceOverride(Pawn pawn, SubstanceType substance)
    {
        pawn.Body.AllParts.ForEach(part => part.SetSubstanceOverride(substance));
    }
}