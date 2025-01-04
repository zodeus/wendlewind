namespace Grafted.Sim.Entities.Items;

[UsedImplicitly]
public class ResourceCount {
    public ItemDef Item = null!;
    public int Count;
    
    public ResourceCount() { }
    
    public ResourceCount(Item item, int count) {
        Item = item.ItemDef;
        Count = count;
    }

    public ResourceCount(ItemDef def, int count) {
        Item = def;
        Count = count;
    }
}