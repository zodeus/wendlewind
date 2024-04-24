namespace Grafted.Sim.Entities.Items;

[UsedImplicitly]
public class ResourceCount {
    public MaterialType? MaterialType;
    public ItemDef? Resource;
    public int Count;

    public ResourceCount() { }

    public ResourceCount(Item item, int count) {
        Resource = item.ItemDef;
        Count = count;
    }

    public ResourceCount(ItemDef def, int count) {
        Resource = def;
        Count = count;
    }

    private ResourceCount(ItemDef? def, MaterialType? materialType, int count) {
        Resource = def;
        MaterialType = materialType;
        Count = count;
    }

    public ResourceCount Copy(int? count = null) {
        return new ResourceCount(Resource, MaterialType, count ?? Count);
    }
}