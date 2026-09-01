namespace Wendlemire.Sim.Entities.Items.Enchantments;

public class EnchantmentProperties
{
    public List<EquipmentType> ValidEquipmentTypes = [];
    public List<BodyPartModifierRecord> BodyPartModifiers = new();
    [UsedImplicitly] public Type? HandlerClass;
    public bool ScaleChance;
    public bool ScaleDuration;
    public bool ScalePower;

    public BodyPartModifierRecord ScaleRecord(BodyPartModifierRecord record, float magic)
    {
        return record.ScaledBy(magic, ScaleChance, ScaleDuration, ScalePower);
    }
}