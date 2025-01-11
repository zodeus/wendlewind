using Grafted.Sim.Entities;
using Grafted.Sim.LootBoxes;

namespace Grafted.Sim.Combat;

public class CombatConfigDef : Def {
    public List<CombatConfigEnemyRecord> Enemies = new();
    public List<LootBoxDef> PotentialLootBoxes = new();
    public BiomeDef Biome = null!;
    public bool IsBoss;
}

public class CombatConfigEnemyRecord {
    public RaceDef Race = null!;
    public PawnConfigDef Config = null!;
    public float SpawnWeight = 1;
    public string PawnName = null;
    public float BodySizeFactor = 1;
    public List<ItemDef> EquipmentItems = new();
    public List<ItemDropCount> InventoryItems = new();
    public BodyModificationRecord BodyModifications = new();
    public List<SkillValueRecord> Skills = new();
    public List<BodyEffectDef> Effects = new();
}