﻿namespace Wendlemire.Scenes.MainGameScene.Gui.Widgets.MiscWidgets.Boak;

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
        tabPanel.AddTab("Incense", new BoakItemsDefaultPanel(
                defs.Where(d => d.ItemType == ItemType.Incense)
                    .ToList()
            )
        );
        tabPanel.AddTab("Medicinal", new BoakItemsDefaultPanel(
                defs.Where(d => d.ItemType == ItemType.Medical)
                    .ToList()
            )
        );
        tabPanel.AddTab("Resource", new BoakItemsDefaultPanel(
                defs.Where(d => d.ItemType == ItemType.Resource)
                    .ToList()
            )
        );
        tabPanel.AddTab("Supplies", new BoakItemsDefaultPanel(
                defs.Where(d => d.ItemType == ItemType.Supplies)
                    .ToList()
            )
        );
        tabPanel.AddTab("Food", new BoakItemsDefaultPanel(
                defs.Where(d => d.ItemType == ItemType.Food)
                    .ToList()
            )
        );
        tabPanel.AddTab("Trinkets", new BoakItemsTrinketsPanel(
                defs.Where(d => d.ItemType == ItemType.Trinket)
                    .ToList()
            )
        );
        tabPanel.AddTab("Potion", new BoakItemsTrinketsPanel(
                defs.Where(d => d.ItemType == ItemType.Potion)
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