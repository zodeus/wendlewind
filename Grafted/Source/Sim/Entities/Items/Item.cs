using System.Collections;
using Grafted.Sim.Entities.Items.Medicinals;

namespace Grafted.Sim.Entities.Items;

public class Item : Entity, IExposable
{
    public ItemDef ItemDef => (ItemDef)Def;
    private float _durability;
    public float MaxDurability = 0;
    public float Durability => _durability;
    public int StackSize = 1;
    public override string Label => Def.Label;
    public string LabelWithStackSize => IsStackable ? $"{Def.Label} x{StackSize}" : Def.Label;
    public bool IsStackable => ItemDef.StackLimit > 1;
    public MedicinalHandler? MedicinalHandler => ItemDef.MedicinalProperties?.Handler;
    public bool CanBeDestroyed => ItemDef.ItemType == ItemType.Equipment;

    public ItemEnchantments? Enchantments;

    public override void Initialize()
    {
        if (ItemDef.EquipmentProperties.MaxEnchantments > 0)
        {
            Enchantments = new ItemEnchantments
            {
                MaxEnchantments = ItemDef.EquipmentProperties.MaxEnchantments
            };
            Enchantments.Initialize();
        }

        MaxDurability = this.GetStatValue(Defs.Stats.MaxDurability);
        _durability = MaxDurability;

        base.Initialize();
    }

    public void ApplyDurabilityLoss(Damage damage)
    {
        _durability -= Core.Random.Next(1, 5);
        if (_durability <= 0)
        {
            Destroy();
        }
    }

    public void Repair()
    {
        _durability = MaxDurability;
    }

    public void ApplyDurabilityLoss(Item? armorHit)
    {
        if (ItemDef.EquipmentProperties.SlotUsedToEquip == EquipmentSlotType.BuiltIn)
        {
            return;
        }

        if (armorHit != null)
        {
            _durability -= Core.Random.Next(1, 10);
        }

        _durability -= Core.Random.Next(1, 2);

        if (_durability <= 0)
        {
            Destroy();
        }
    }

    public Item SplitStack(int? amountWanted = null)
    {
        if (amountWanted == null || amountWanted >= StackSize)
        {
            EjectFromContainer();
            return this;
        }

        var item = CreateItemFromSplit(ItemDef, amountWanted.Value);
        StackSize -= amountWanted.Value;
        return item;
    }

    protected virtual Item CreateItemFromSplit(ItemDef def, int amountWanted)
    {
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

    public override void ExposeData()
    {
        ScribeValues.Look(ref _durability!, "Durability");
        ScribeValues.Look(ref MaxDurability!, "MaxDurability");
        ScribeValues.Look(ref StackSize!, "StackSize");
        ScribeDeep.Look(ref Enchantments!, "Enchantments");
        base.ExposeData();
    }
}

public class ItemEnchantments : IEnumerable<Item>, IExposable
{
    public int MaxEnchantments;

    private Dictionary<int, Item?> _enchantments = null!;

    public void ExposeData()
    {
        ScribeValues.Look(ref MaxEnchantments!, "MaxEnchantments");
        ScribeCollections.Look(ref _enchantments!, "Enchantments", LookMode.Value, LookMode.Deep);
    }

    public Item? TryGetAtSlot(int position)
    {
        return _enchantments.Count > position ? _enchantments[position] : null;
    }

    public void TryAdd(Item enchantment, int position = 0)
    {
        _enchantments[position] = enchantment;
    }

    public void Initialize()
    {
        _enchantments = new Dictionary<int, Item?>();
        for (var i = 0; i < MaxEnchantments; i++)
        {
            _enchantments[i] = null;
        }
    }

    public IEnumerator<Item> GetEnumerator()
    {
        return _enchantments.Values.Where(i => i != null).GetEnumerator()!;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}