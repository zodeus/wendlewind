using Grafted.Sim.Entities.Pawns.Bodies;

namespace Grafted.Sim.Entities.Pawns;

public class PawnConfigDef : Def {
    public PawnType PawnType = PawnType.Invalid;
    public string? PawnName = null;
    public List<ItemDef> EquipmentItems = new();
    public List<ItemDropCount> InventoryItems = new();
}