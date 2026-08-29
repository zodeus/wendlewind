namespace Wendlewind.Sim.Entities.Pawns;

public class BodyPartLayoutCell
{
    public string PartKey = "";
    public float PosX;
    public float PosY;
    public int RenderOrder;
    public float ScaleMultiplier = 1f;
    public float Rotation;
    public bool FlipH;
    public bool FlipV;
    public bool HasEquipmentAttachment;
    public float EquipOffsetX;
    public float EquipOffsetY;
    public float EquipRotation;
    public float EquipScale = 1f;
    public bool EquipFlipH;
    public bool EquipRenderWeapons = true;
    public bool EquipRenderArmor = true;
}

public class BodyPartLayoutDef : Def
{
    public BodyDef Body = null!;
    public int NativeSize = 512;
    public List<BodyPartLayoutCell> Cells = new();

    public static BodyPartLayoutDef? ForBody(BodyDef body)
    {
        if (body.BodyPartLayout != null)
        {
            return body.BodyPartLayout;
        }

        return DefRepository<BodyPartLayoutDef>.Defs.FirstOrDefault(def => def.Body == body);
    }
}
