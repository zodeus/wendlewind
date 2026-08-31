namespace Wendlewind.Sim.Arena;

public enum ShopCategory
{
    Starter,
    Weapons,
    Armor,
    Potions,
    Enchantments,
    Trinkets,
    Cloaks,
    Incense,
    Medicine,
    Food,
    Ammo
}

public static class ShopCategoryLabels
{
    public static string Label(this ShopCategory category) => category switch
    {
        ShopCategory.Starter => "Starter",
        ShopCategory.Weapons => "Weapons",
        ShopCategory.Armor => "Armor",
        ShopCategory.Potions => "Potions",
        ShopCategory.Enchantments => "Enchantments",
        ShopCategory.Trinkets => "Trinkets",
        ShopCategory.Cloaks => "Cloaks",
        ShopCategory.Incense => "Incense",
        ShopCategory.Medicine => "Medicine",
        ShopCategory.Food => "Food",
        ShopCategory.Ammo => "Ammo",
        _ => category.ToString()
    };
}
