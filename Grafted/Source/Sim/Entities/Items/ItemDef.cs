using Grafted.Scenes.MainGameScene.Gui.Widgets.DefWidgets;
using Grafted.Sim.Entities.Items.Medicinals;

namespace Grafted.Sim.Entities.Items;

public class ItemDef : EntityDef
{
    public override EntityType EntityType => EntityType.Item;
    public override Type DefUiClass => typeof(ItemDefPanel);

    public ItemType ItemType = ItemType.None;
    public ToolType ToolType = ToolType.None;
    public List<ToolCategory> ToolCategories = new();
    public int StackLimit = 1;
    public EquipmentProperties EquipmentProperties = new();
    public WeaponProperties WeaponProperties = new();
    public List<ToolManeuverDef> ToolManeuvers = new();

    public CraftingProperties CraftingProperties = new();
    public FoodProperties? FoodProperties;
    public MedicinalProperties? MedicinalProperties;

    public override void ResolveDependencies()
    {
        base.ResolveDependencies();
        if (ToolType == ToolType.None)
        {
            return;
        }

        if (ToolManeuvers.Any())
        {
            Log.Info($"Sequences for Def:{Moniker} have been specified by XML, skipping auto-associations");
            return;
        }

        foreach (var def in DefRepository<ToolManeuverDef>.Defs.Where(maneuverDef => maneuverDef.Tools?.Contains(ToolType) == true))
        {
            ToolManeuvers.Add(def);
        }
    }
}