using Grafted.Sim.Entities;
using Grafted.Sim.LootBoxes;

namespace Grafted.Scenes.MainGameScene.Gui.Widgets.MiscWidgets.Boak;

internal sealed class BoakItemsPanel : Panel
{
    public BoakItemsPanel(IReadOnlyList<ItemDef> defs, IReadOnlyList<WeaponManeuverDef> weaponManeuverDefs)
    {
        TabPanel tabPanel = new()
        {
            ButtonStyle = BaseContent.Styles.Button.Normal
        };
        tabPanel.AddTab("Weapons", new BoakItemsWeaponPanel(
                defs.Where(d => d.ItemType == ItemType.Equipment && d.EquipmentProperties?.EquipmentType == EquipmentType.Weapon)
                    .ToList(), weaponManeuverDefs
            )
        );
        tabPanel.AddTab("Armor", new BoakItemsArmorPanel(
                defs.Where(d => d.ItemType == ItemType.Equipment && d.EquipmentProperties?.EquipmentType == EquipmentType.Armor)
                    .ToList()
            )
        );
        tabPanel.AddTab("Medicinal", new BoakItemsMedicalPanel(
                defs.Where(d => d.ItemType == ItemType.Medical)
                    .ToList()
            )
        );
        tabPanel.AddTab("Trinkets", new BoakItemsTrinketsPanel(
                defs.Where(d => d.ItemType == ItemType.Trinket)
                    .ToList()
            )
        );
        tabPanel.AddTab("Enchantments", new BoakItemsEnchantmentsPanel(
                defs.Where(d => d.ItemType == ItemType.Enchantment)
                    .ToList()
            )
        );
        tabPanel.AddTab("Maneuvers", new BoakItemsManeuversPanel(weaponManeuverDefs));
        Widgets.Add(tabPanel);
    }
}