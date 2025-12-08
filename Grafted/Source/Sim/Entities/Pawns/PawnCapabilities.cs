namespace Grafted.Sim.Entities.Pawns;

public class PawnCapabilities : IExposable
{
    private readonly Pawn _pawn;

    public PawnCapabilities(Pawn pawn)
    {
        _pawn = pawn;
    }

    public float Sight
    {
        get
        {
            // if pawn has no eye sockets return 1
            if (_pawn.Body.AllExternalParts.Count(p => p.Type == BodyPartType.Eye) == 0)
            {
                return 1;
            }

            var eyes = _pawn.Body.AllExternalParts.Count(p => p.Type == BodyPartType.Eye && p.IsFunctional);
            if (_pawn.Inventory.Trinkets.Any(t => t.Def == Defs.Items.ThirdEye))
            {
                eyes += 1;
            }

            if (eyes >= 2)
            {
                return 1;
            }

            if (eyes >= 1)
            {
                return .6f;
            }

            return 0.05f;
        }
    }

    public float Breathing
    {
        get
        {
            if (_pawn.Body.RequiresLungs == false) return 1;
            var lungs = _pawn.Body.AllParts.Count(p => p.BodyPartDef.BodyPartType == BodyPartType.Lung && p.IsFunctional);
            return lungs switch
            {
                2 => 1f,
                1 => .5f,
                _ => .0f
            };
        }
    }

    public float Mobility => _pawn.Body.AllParts.Sum(p => p.HasMobility ? p.BodyPartDef.MobilityFraction : 0f);

    public void ExposeData()
    {
    }
}