namespace Wendlemire.Sim.Entities.Pawns;

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
            var eyeSockets = 0;
            var eyes = 0;
            var parts = _pawn.Body.AllExternalParts;
            for (var i = 0; i < parts.Count; i++)
            {
                var part = parts[i];
                if (part.Type != BodyPartType.Eye)
                {
                    continue;
                }

                eyeSockets++;
                if (part.IsFunctional)
                {
                    eyes++;
                }
            }

            if (eyeSockets == 0)
            {
                return 1;
            }
            if (_pawn.Inventory.Trinkets.Any(t => t.Def == Defs.Items.MechanicalEye))
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
            var lungs = 0;
            var allParts = _pawn.Body.AllParts;
            for (var i = 0; i < allParts.Count; i++)
            {
                var part = allParts[i];
                if (part.Type == BodyPartType.Lung && part.IsFunctional)
                {
                    lungs++;
                }
            }

            var breathing = lungs switch
            {
                2 => 1f,
                1 => .5f,
                _ => .0f
            };

            if (breathing > 0 && _pawn.Body.Effects.Has(Defs.BodyEffects.Lungworted))
            {
                return Math.Max(breathing, LungwortedBreathingFloor);
            }

            return breathing;
        }
    }

    public const float LungwortedBreathingFloor = 0.8f;
    public const float TallowedMobilityFactor = 0.55f;

    public float Mobility
    {
        get
        {
            var mobility = 0f;
            var allParts = _pawn.Body.AllParts;
            for (var i = 0; i < allParts.Count; i++)
            {
                var part = allParts[i];
                if (part.HasMobility)
                {
                    mobility += part.BodyPartDef.MobilityFraction;
                }
            }

            if (_pawn.Body.Effects.Has(Defs.BodyEffects.Tallowed))
            {
                mobility *= TallowedMobilityFactor;
            }

            return mobility;
        }
    }

    public float Circulation => AverageHealth(BodyPartType.Artery);

    public float Digestion => AverageHealth(BodyPartType.Stomach);

    private float AverageHealth(BodyPartType type)
    {
        var total = 0d;
        var count = 0;
        var allParts = _pawn.Body.AllParts;
        for (var i = 0; i < allParts.Count; i++)
        {
            var part = allParts[i];
            if (part.Type != type)
            {
                continue;
            }

            total += part.HealthPercent;
            count++;
        }

        return count == 0 ? 0 : (float)(total / count);
    }

    public void ExposeData()
    {
    }
}