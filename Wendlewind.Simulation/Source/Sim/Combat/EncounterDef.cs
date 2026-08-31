

// ReSharper disable FieldCanBeMadeReadOnly.Global
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable CollectionNeverUpdated.Global
// ReSharper disable UnassignedField.Global

namespace Wendlewind.Sim.Combat;

public class EncounterProperties
{
    public MysteryProperties? MysteryProperties = null;
    public List<EncounterEnemyRecord> Enemies = new();
    public List<LootBoxDef> PotentialLootBoxes = new();
    public int? MaxBoxes;
    public bool IsBoss;
    public bool SkipLoot;
}

public class EncounterEnemyRecord
{
    public PawnDef PawnDef = null!;
    public string PawnName = "undefined";
    public PawnLoadoutDef Loadout = null!;
    public float SpawnWeight = 1;
    public float BodySizeFactor = 1;
    public List<ItemDef> EquipmentItems = new();
    public List<ItemDropCount> InventoryItems = new();
    public BodyModificationRecord BodyModifications = new();
    public List<SkillValueRecord> Skills = new();
    public List<BodyEffectDef> Effects = new();
}
