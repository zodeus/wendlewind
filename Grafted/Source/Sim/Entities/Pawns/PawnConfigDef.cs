using System;
using System.Collections.Generic;
using Grafted.Definitions;
using Grafted.Sim.Entities.Items;

namespace Grafted.Sim.Entities.Pawns;

public class PawnConfigDef : Def {
    public PawnType PawnType = PawnType.Invalid;
    public string? PawnName = null;
    public Type BodyGeneratorClass = typeof(HumanBodyGenerator);
    public List<ItemDef> EquipmentItems = new();
    public List<ItemDropCount> InventoryItems = new();
}