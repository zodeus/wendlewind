namespace Wendlemire.Sim.Entities.Items.Medicinals;

public enum MedicalTriggerType
{
    Immediately,
    AfterSeconds,
    SelfBloodBelow,
    SelfPartsDamaged,
    PartBelowHealth,
    PartSevered,
    HasNecrosis,
    BurningOrAcid,
    HasPoison
}
