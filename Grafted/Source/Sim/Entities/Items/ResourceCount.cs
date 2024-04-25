namespace Grafted.Sim.Entities.Items;

[UsedImplicitly]
public class ResourceCount {
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
}