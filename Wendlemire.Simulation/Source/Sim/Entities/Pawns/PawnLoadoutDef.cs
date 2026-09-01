﻿namespace Wendlemire.Sim.Entities.Pawns;

public class PawnLoadoutDef : Def
{
    public PawnDef PawnDef = null!;
    public PawnType PawnType = PawnType.Invalid;
    public List<ItemDef> EquipmentItems = new();
    public List<ItemDropCount> InventoryItems = new();
}