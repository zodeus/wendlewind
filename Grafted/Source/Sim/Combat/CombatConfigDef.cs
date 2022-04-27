using System.Collections.Generic;
using Grafted.Definitions;
using Grafted.Maths;
using Grafted.Sim.Entities;
using Grafted.Sim.Entities.Items;
using Grafted.Sim.Entities.Pawns;

namespace Grafted.Sim.Combat;

public class CombatConfigDef : Def {
    public List<CombatConfigEnemyRecord> Enemies = new();
    public float DistanceToEnd = 0;
}

public class CombatConfigEnemyRecord {
    public RaceDef Race = null!;
    public PawnConfigDef Config = null!;
    public float SpawnWeight = 1;
    public string PawnName = null;
    public List<ItemDef> EquipmentItems = new();
    public List<ItemDropCount> InventoryItems = new();
    public BodyModificationRecord BodyModifications = new();
}