namespace Wendlemire.Sim.Cosmetics;

public class CosmeticDef : Def
{
    public CosmeticCategory Category = CosmeticCategory.NamePlate;
    public int Price;
    public bool DefaultOwned;
    public Color TextColor = new(203, 184, 150);
    public string FrameAtlasKey = "panel-frame-medium";
    public Color FrameTint = Color.White;
}
