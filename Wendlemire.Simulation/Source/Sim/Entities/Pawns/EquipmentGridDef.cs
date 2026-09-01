namespace Wendlemire.Sim.Entities.Pawns;

public class EquipmentGridCell
{
    public string PartKey = "";
    public EquipmentSlotType Slot;
    public int Col;
    public int Row;
}

public class EquipmentGridDef : Def
{
    public BodyDef Body = null!;
    public int Columns = 8;
    public int Rows = 10;
    public List<EquipmentGridCell> Cells = new();

    public static EquipmentGridDef? ForBody(BodyDef body)
    {
        if (body.EquipmentGrid != null)
        {
            return body.EquipmentGrid;
        }

        return DefRepository<EquipmentGridDef>.Defs.FirstOrDefault(def => def.Body == body);
    }
}
