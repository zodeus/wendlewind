namespace Wendlemire.Sim.Entities.Pawns;

public class BodyPartDef : EntityDef {
    public override EntityType EntityType => EntityType.BodyPart;
    public BodyPartType BodyPartType = BodyPartType.Undefined;
    public float BloodAmount = 0;
    public float HitWeight = 0;
    public bool IsVital = false;
    public bool IsOrgan = false;
    public SubstanceType Substance = SubstanceType.Undefined;
    public float MobilityFraction = 0;
    public List<BodyPartSocketDef> Sockets = new();
    public List<EquipmentSlotType>? EquipmentSlots = null;
    public AdaptiveBodyPartProperties? AdaptiveProperties;
    public string? WhiteIconTexturePath;
}
