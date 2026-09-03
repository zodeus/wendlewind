namespace Wendlemire.Sim.Entities.Pawns.Modifiers;

[UsedImplicitly]
public class BoneDecayHandler : BodyPartModifier
{
    public BoneDecayHandler(IRng rng)
    {
        Rng = rng;
    }

    public double CurrentDamage = 0.12;
    public const double BaseDamage = 0.12;
    public const double EntropyRate = 0.75;
    public const double MinimumDamage = 0.025;
    public override List<SubstanceType> AllowedSubstances => [SubstanceType.Bone, SubstanceType.Chitin];

    public override void Initialize()
    {
        CurrentDamage = BaseDamage * Power;
    }

    public override void Tick()
    {
        Ticks++;
        BodyPart.HitPoints -= CurrentDamage;
        if (Ticks % 30 == 0)
        {

            CurrentDamage = Math.Max(CurrentDamage * EntropyRate, MinimumDamage);
        }

        if (BodyPart.IsDestroyed)
        {
            IsExpired = true;
        }

        CheckIfLostVitalPart();
    }

    public override void MergeWith(BodyPartModifier modifier)
    {
        CurrentDamage = BaseDamage * Power;
        base.MergeWith(modifier);
    }

    public override bool ApplyToPart(BodyPart part)
    {
        if (!AllowedSubstances.Contains(part.Substance))
        {
            return false;
        }

        if (!IsBoneExposed(part))
        {
            return false;
        }

        part.TryAddModifier(this);

        return true;
    }

    private static bool IsBoneExposed(BodyPart part)
    {
        var host = part;
        while (host is { IsExternal: false })
        {
            host = host.Socket?.ParentPart;
            if (host == null)
            {
                return true;
            }
        }

        return host.Skin == null || host.Skin.IsDestroyed;
    }

    public override InfoPanelData GetInfoData() => new()
    {
        Damage = CurrentDamage,
        DamageColor = new Color(200, 200, 180),
        Lines =
        [
            new("Damage decreases over time", new Color(180, 180, 160)),
            new("Expires when part destroyed", new Color(150, 150, 130))
        ]
    };
}