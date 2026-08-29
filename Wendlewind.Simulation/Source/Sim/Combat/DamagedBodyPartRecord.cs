namespace Wendlewind.Sim.Combat;

public class DamagedBodyPartRecord
{
    public readonly BodyPart BodyPart;
    public double DamageApplied;
    public string Label => BodyPart.Label;
    public List<BodyPartModifierDef> AppliedModifiers = new();
    public BodyPartType PartType => BodyPart.Type;
    public bool WasDestroyed;
    public bool StoppedFunctioning;
    public bool WasSevered;
    public bool IsVital => BodyPart.IsVital;

    public DamagedBodyPartRecord(BodyPart bodyPart, double damageApplied = 0)
    {
        BodyPart = bodyPart;
        DamageApplied = damageApplied;
    }
}