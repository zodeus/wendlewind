namespace Wendlemire.Sim.Entities.Pawns;

[UsedImplicitly]
public class PawnDef : EntityDef
{
    public override EntityType EntityType => EntityType.Pawn;
    public string Species = "undefined";
    public BodyDef Body = null!;
}