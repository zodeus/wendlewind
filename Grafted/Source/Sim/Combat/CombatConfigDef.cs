using System.Collections.Generic;
using Grafted.Definitions;
using Grafted.Maths;
using Grafted.Sim.Entities;
using Grafted.Sim.Entities.Items;
using Grafted.Sim.Entities.Pawns;
using Grafted.Sim.Zones;

namespace Grafted.Sim.Combat;

public class CombatConfigDef : Def {
    public List<CombatConfigEnemyRecord> Enemies = new();
    public RangeFloat SpawnRange; // as a percentage of travel distance
    public ZoneDef Zone = null!;
    public bool IsBoss;
}

public class CombatConfigEnemyRecord {
    public RaceDef Race = null!;
    public PawnConfigDef Config = null!;
    public float SpawnWeight = 1;
    public string PawnName = null;
    public List<ItemDef> EquipmentItems = new();
    public List<ItemDropCount> InventoryItems = new();
    public BodyModificationRecord BodyModifications = new();
    public TimeOfDay SpawnDuring = TimeOfDay.AllDay;
    public List<SkillValueRecord> Skills = new();
}