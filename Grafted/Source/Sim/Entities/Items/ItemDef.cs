using Grafted.Scenes.MainGameScene.Gui.Widgets.DefWidgets;
using Grafted.Sim.Entities.Items.Enchantments;
using Grafted.Sim.Entities.Items.Medicinals;
using Grafted.Sim.Entities.Items.Trinkets;

namespace Grafted.Sim.Entities.Items;

public class ItemDef : EntityDef
{
    public override EntityType EntityType => EntityType.Item;
    public override Type DefUiClass => typeof(ItemDefPanel);

    public ItemType ItemType = ItemType.None;
    public int StackLimit = 1;

    public EquipmentProperties? EquipmentProperties;
    public WeaponProperties? WeaponProperties;
    public CraftingProperties? CraftingProperties;
    public FoodProperties? FoodProperties;
    public MedicinalProperties? MedicinalProperties;
    public EnchantmentProperties? EnchantmentProperties;
    public TrinketProperties? TrinketProperties;
    public DisassembleProperties? DisassembleProperties;

    public override void ResolveDependencies()
    {
        base.ResolveDependencies();
        if (WeaponProperties == null || WeaponProperties.WeaponType == WeaponType.None)
        {
            return;
        }

        if (WeaponProperties.WeaponManeuvers.Count != 0)
        {
            Log.Info($"Sequences for Def:{Moniker} have been specified by XML, skipping auto-associations");
            return;
        }

        foreach (var def in DefRepository<WeaponManeuverDef>.Defs.Where(maneuverDef => maneuverDef.Weapons?.Contains(WeaponProperties.WeaponType) == true))
        {
            WeaponProperties.WeaponManeuvers.Add(def);
        }
    }
}