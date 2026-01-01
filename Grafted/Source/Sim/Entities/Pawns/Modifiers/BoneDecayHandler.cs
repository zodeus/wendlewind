namespace Grafted.Sim.Entities.Pawns.Modifiers;

[UsedImplicitly]
public class BoneDecayHandler : BodyPartModifier
{
    public double CurrentDamage = 0.3;
    public const double BaseDamage = 0.3;
    public const double EntropyRate = 0.85;
    public const double MinimumDamage = 0.025;
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
        if (part.Substance != SubstanceType.Bone && part.Substance != SubstanceType.Chitin)
        {
            return false;
        }

        part.TryAddModifier(this);

        return true;
    }
}