namespace Wendlemire.Sim.Entities.Items.Equipment;

[UsedImplicitly(ImplicitUseTargetFlags.WithInheritors)]
public abstract class EquipmentHandler : IExposable, IHasContext, IHasRng
{
    public GameContext Context { get; set; } = null!;
    public IRng Rng { get; set; } = null!;
    public Item Equipment = null!;

    public string Label => Equipment.Label;

    public virtual void Tick(Pawn pawn, BodyPart bodyPart)
    {
    }

    public virtual void ModifyStat(Pawn pawn, StatDef stat, ref float value)
    {
        
    }
    
    public virtual bool OnPreDamageTaken(DamageRequest request, DamageResponse response)
    {
        return false;
    }

    public virtual void PostPawnDamageTakenEffect(BodyPart bodyPart, Pawn pawn, Pawn target, DamageRecord damageRecord)
    {
    }

    public virtual void ExposeData()
    {
        ScribeReferences.Look(ref Equipment!, "Equipment");
    }

    public override string ToString()
    {
        return $"{Equipment.Label} Handler";
    }
}

