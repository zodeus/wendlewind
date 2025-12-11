namespace Grafted.Sim.Entities.Items.Equipment;

public abstract class EquipmentHandler : IExposable
{
    public Item Equipment = null!;

    public string Label => Equipment.Label;

    public virtual void Tick()
    {
    }

    public virtual void TickForPawn(Pawn pawn, BodyPart bodyPart)
    {
    }
    
    public virtual bool OnPreDamageTaken(DamageRequest request, DamageResponse response)
    {
        return false;
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

