using Grafted.Definitions;

namespace Grafted.Sim.Entities.Items;

public class Item : Entity {
    public ItemDef ItemDef => (ItemDef) Def;
    public float Durability = 0;
    public int StackSize;
    public override string Label => IsStackable ? $"{Def.Label} x{StackSize}" : Def.Label;
    public bool IsStackable => ItemDef.StackLimit > 1;

    public override void Initialize() {
        Durability = this.GetStatValue(Defs.Stats.Durability);
        base.Initialize();
    }

    public bool CanBeUsedFor(ToolCategory toolCategory) {
        return ItemDef.ToolCategories.Contains(toolCategory);
    }
}