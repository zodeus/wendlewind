namespace Wendlewind.Sim.Entities.Pawns.Modifiers;

[UsedImplicitly]
public class BoneDecayHandler : BodyPartModifier
{
    public BoneDecayHandler(IRng rng)
    {
        Rng = rng;
    }

    public double CurrentDamage = 0.3;
    public const double BaseDamage = 0.3;
    public const double EntropyRate = 0.85;
    public const double MinimumDamage = 0.025;
    public override List<SubstanceType> AllowedSubstances => [SubstanceType.Bone, SubstanceType.Chitin];

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

    public override void ExposeData()
    {
        base.ExposeData();
    }

    public override void MergeWith(BodyPartModifier modifier)
    {
        CurrentDamage = BaseDamage;
        base.MergeWith(modifier);
    }

    public override bool ApplyToPart(BodyPart part)
    {
        if (AllowedSubstances.Contains(part.Substance) == false)
        {
            return false;
        }

        part.TryAddModifier(this);

        return true;
    }

    public override InfoPanelData GetInfoData() => new InfoPanelData
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