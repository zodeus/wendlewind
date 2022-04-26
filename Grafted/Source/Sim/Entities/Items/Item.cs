using Grafted.Definitions;
using Grafted.Maths;
using Grafted.Sim.Combat;

namespace Grafted.Sim.Entities.Items;

public class Item : Entity {
    public ItemDef ItemDef => (ItemDef) Def;
    private float _durability;
    public float MaxDurability = 0;
    public float Durability => _durability;
    public int StackSize;
    public override string Label => Def.Label;
    public string LabelWithStackSize => IsStackable ? $"{Def.Label} x{StackSize}" : Def.Label;
    public bool IsStackable => ItemDef.StackLimit > 1;
    public int WeightSingle => Mathf.CeilToInt(this.GetStatValue(Defs.Stats.Weight));
    public int Weight => Mathf.CeilToInt(this.GetStatValue(Defs.Stats.Weight) * StackSize);

    public override void Initialize() {
        MaxDurability = this.GetStatValue(Defs.Stats.MaxDurability);
        _durability = MaxDurability;

        base.Initialize();
    }

    public bool CanBeUsedFor(ToolCategory toolCategory) {
        return ItemDef.ToolCategories.Contains(toolCategory);
    }

    public void ApplyDurabilityLoss(Damage damage) {
        _durability -= Core.Random.Next(1, 5);
        if (_durability <= 0) {
            Destroy();
        }
    }

    public void Repair() {
        _durability = MaxDurability;
    }

    public void ApplyDurabilityLoss(Item? armorHit) {
        if (ItemDef.EquipmentProperties.SlotUsedToEquip == EquipmentSlotType.BuiltIn) {
            return;
        }

        if (armorHit != null) {
            _durability -= Core.Random.Next(1, 10);
        }

        _durability -= Core.Random.Next(1, 2);

        if (_durability <= 0) {
            Destroy();
        }
    }

    public Item SplitStack(int? amountWanted = null) {
        if (amountWanted == null || amountWanted >= StackSize) {
            Container?.Remove(this);
            return this;
        }

        Item item = CreateItemFromSplit(ItemDef, amountWanted.Value);
        StackSize -= amountWanted.Value;
        return item;
    }

    protected virtual Item CreateItemFromSplit(ItemDef def, int amountWanted) {
        return EntityGenerator.CreateEntity<Item>(def, amountWanted);
    }

    /*public bool TryToAbsorbStack(Entity otherEntity) {
        int amountToAbsorb = Math.Min(otherEntity.StackSize, Def.StackLimit - StackSize);
        StackSize += amountToAbsorb;
        otherEntity.StackSize -= amountToAbsorb;
        if (otherEntity.StackSize <= 0) {
            otherEntity.Destroy();
            return true;
        }

        return false;
    }*/
}