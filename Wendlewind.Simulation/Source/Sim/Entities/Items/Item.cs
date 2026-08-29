using Wendlewind.Sim.Entities.Items.Weapons;

namespace Wendlewind.Sim.Entities.Items;

public class Item : Entity, IExposable
{
    public ItemDef ItemDef => (ItemDef)Def;
    private float _durability;
    public float MaxDurability;
    public float Durability => _durability;
    
    private int _stackSize = 1;
    public int StackSize
    {
        get => _stackSize;
        set
        {
            if (_stackSize == value) return;
            _stackSize = value;
            StackSizeChanged?.Invoke(this);
        }
    }
    public event Action<Item>? StackSizeChanged;
    
    public EnchantmentHandler? EnchantmentHandler;
    public TrinketHandler? TrinketHandler;
    public EquipmentHandler? EquipmentHandler;
    public PotionHandler? PotionHandler;
    public WeaponHandler? WeaponHandler;
    public override string Label => Def.Label;
    public string LabelWithStackSize => IsStackable ? $"{Def.Label} x{StackSize}" : Def.Label;
    public bool IsStackable => ItemDef.StackLimit > 1;
    public MedicinalHandler? MedicinalHandler => ItemDef.MedicinalProperties?.CreateHandler(Context.Factory);

    public bool CanBeDestroyed => ItemDef.ItemType == ItemType.Equipment;
    public bool UseInCombat = true;
    public PotionTrigger? PotionTrigger;

    public ItemEnchantments? Enchantments;

    public override void Initialize()
    {
        if (ItemDef.EquipmentProperties?.MaxEnchantments > 0)
        {
            Enchantments = new ItemEnchantments
            {
                MaxEnchantments = ItemDef.EquipmentProperties.MaxEnchantments
            };
            Enchantments.Initialize();
        }

        if (ItemDef.EnchantmentProperties?.HandlerClass != null)
        {
            EnchantmentHandler = Context.Factory.Create<EnchantmentHandler>(ItemDef.EnchantmentProperties.HandlerClass);
            EnchantmentHandler.Enchantment = this;
        }

        if (ItemDef.TrinketProperties?.HandlerClass != null)
        {
            TrinketHandler = Context.Factory.Create<TrinketHandler>(ItemDef.TrinketProperties.HandlerClass);
            TrinketHandler.Trinket = this;
        }

        if (ItemDef.EquipmentProperties?.HandlerClass != null)
        {
            EquipmentHandler = Context.Factory.Create<EquipmentHandler>(ItemDef.EquipmentProperties.HandlerClass);
            EquipmentHandler.Equipment = this;
        }

        if (ItemDef.PotionProperties?.HandlerClass != null)
        {
            PotionHandler = ItemDef.PotionProperties.CreateHandler(Context.Factory);
            if (PotionHandler != null)
            {
                PotionHandler.Potion = this;
            }

            PotionTrigger ??= ItemDef.PotionProperties.DefaultTrigger?.Clone();
        }

        if (ItemDef.WeaponProperties?.HandlerClass != null)
        {
            WeaponHandler = ItemDef.WeaponProperties.CreateHandler(Context.Factory);
            if (WeaponHandler != null)
            {
                WeaponHandler.Weapon = this;
            }
        }

        MaxDurability = this.GetStatValue(Defs.Stats.MaxDurability);
        _durability = MaxDurability;

        base.Initialize();
    }

    public void ApplyDurabilityLoss(float durabilityLoss)
    {
        _durability -= durabilityLoss;
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
        if (ItemDef.EquipmentProperties?.SlotUsedToEquip == EquipmentSlotType.BuiltIn)
        {
            return;
        }

        if (armorHit != null)
        {
            _durability -= Context.Rng.Next(1, 10);
        }

        _durability -= Context.Rng.Next(1, 2);

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
        return Context.Factory.CreateEntity<Item>(def, amountWanted);
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

    public override void Tick()
    {
        EnchantmentHandler?.Tick();
        TrinketHandler?.Tick();
        WeaponHandler?.Tick();
        base.Tick();
    }

    public override void ExposeData()
    {
        ScribeValues.Look(ref _durability, "Durability");
        ScribeValues.Look(ref MaxDurability, "MaxDurability");
        ScribeValues.Look(ref _stackSize, "StackSize");
        ScribeValues.Look(ref UseInCombat, "UseInCombat");
        ScribeDeep.Look(ref PotionTrigger!, "PotionTrigger");
        ScribeDeep.Look(ref Enchantments!, "Enchantments");
        ScribeDeep.Look(ref EnchantmentHandler!, "EnchantmentHandler");
        ScribeDeep.Look(ref TrinketHandler!, "TrinketHandler");
        ScribeDeep.Look(ref EquipmentHandler!, "EquipmentHandler");
        ScribeDeep.Look(ref PotionHandler!, "PotionHandler");
        ScribeDeep.Look(ref WeaponHandler!, "WeaponHandler");
        base.ExposeData();
    }
}