namespace Wendlemire.Sim.LootBoxes;

public enum LootBoxCategory
{
    Weapons,
    Armor,
    Food,
    Supplies,
    Trinkets,
    Medicinal,
    Enchantments,
    Resources,
    Potions,
}

public enum LootBoxCollectionType
{
    Random,
    All
}

public class LootBoxTrapProperties
{
    public string? TrapLabel;
}

public class LootBoxItem
{
    public ItemDef ItemDef = null!;
    public RangeInt Amount = new(1, 1);
    public float Weight = 1;
    public float ChanceToDrop = 1;
}

public class LootBoxDef : Def
{
    public Type? UiClass;
    public LootBoxCategory Category;
    public RangeInt CollectionLimit;
    public LootBoxTrapProperties? TrapProperties;
    public List<LootBoxItem> Items = [];
    public string? TexturePath;
}
