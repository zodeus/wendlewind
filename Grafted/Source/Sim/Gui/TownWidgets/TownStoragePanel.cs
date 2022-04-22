using Grafted.Definitions;
using Grafted.Maths;
using Grafted.Sim.Entities;
using Grafted.Sim.Entities.Items;
using Grafted.Sim.Entities.Pawns;
using Grafted.Sim.Gui.EntityWidgets;
using Grafted.Sim.Gui.EntityWidgets.PawnWidgets;
using Grafted.Sim.Gui.MiscWidgets;
using Grafted.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.Styles;

namespace Grafted.Sim.Gui.TownWidgets;

public class TownStoragePanel : Panel, IUpdatable {
    private readonly PawnInventoryPanel _inventoryPanel;
    private readonly PawnEquipmentPanel _equipmentPanel;
    private readonly EntityListPanel _storagePanel;

    public TownStoragePanel(Town town, Pawn playerPawn) {
        _inventoryPanel = new PawnInventoryPanel(
            playerPawn,
            entity => {
                if (Core.Sim.Gui!.MouseAttachment == null && Input.IsKeyDown(Keys.LeftShift)) {
                    if (entity is not Item item || item.Def.Moniker == "Cauterize") {
                        return;
                    }

                    playerPawn.Inventory.Items.Remove(item);
                    item.Destroy();
                    return;
                }

                Core.Sim.Gui!.MouseAttachment = new MouseAttachment(entity.Icon, updateAction: attachment => {
                    if (Input.RightMouseButtonPressed) attachment.Detach();
                }) {
                    IconSize = new Size(40, 40),
                    Data = entity,
                };
            }, entity => {
                town.Storage.TryTransfer((Item) entity);
            }
        ) {
            Visible = !playerPawn.IsDead,
            MinHeight = 700,
            Width = 300, GridRow = 1, GridColumn = 1
        };

        _equipmentPanel = new PawnEquipmentPanel(playerPawn.Equipment, (part, slot) => {
            // UnEquip
            if (Core.Sim.Gui!.MouseAttachment == null && Input.RightMouseButtonReleased && slot != EquipmentSlotType.BuiltIn) {
                Item? unEquippedItem = playerPawn.Equipment.UnEquip(part, slot);
                if (unEquippedItem != null) {
                    playerPawn.Inventory.Items.TryAdd(unEquippedItem);
                }
            }

            if (Core.Sim.Gui!.MouseAttachment?.Data is Item item) {
                if (item.Def == Defs.Items.RepairKit) {
                    if (playerPawn.Equipment.GetBySlot(part, slot) is { } equipmentItem && equipmentItem.Durability < equipmentItem.MaxDurability) {
                        equipmentItem.Repair();
                        item.StackSize--;
                        if (item.StackSize == 0) {
                            item.Destroy();
                            Core.Sim.Gui!.MouseAttachment.Detach();
                        }
                    }

                    return;
                }

                // Try Equip
                if (item.ItemDef.EquipmentProperties.SlotUsedToEquip == slot || (item.ItemDef.ItemType == ItemType.Potion && slot is EquipmentSlotType.PotionSlot1 or EquipmentSlotType.PotionSlot2)) {
                    Item? unEquippedItem = null;
                    if (item.ItemDef.ItemType == ItemType.Potion) {
                        //todo implement splitting
                        Item potion;
                        if (item.StackSize > 1) {
                            item.StackSize--;
                            potion = EntityGenerator.CreateEntity<Item>(item.ItemDef, 1);
                        }
                        else {
                            potion = item;
                            playerPawn.Inventory.Items.Remove(item);
                        }

                        unEquippedItem = playerPawn.Equipment.TryEquip(part, slot, potion);
                    }
                    else {
                        playerPawn.Inventory.Items.Remove(item);
                        unEquippedItem = playerPawn.Equipment.TryEquip(part, slot, item);
                    }

                    Core.Sim.Gui!.MouseAttachment.Detach();
                    if (unEquippedItem != null) {
                        playerPawn.Inventory.Items.TryAdd(unEquippedItem);
                    }
                }

                return;
            }

            if (part.Equipment[slot] != null) {
                Core.Sim.Gui!.ViewEntity(part.Equipment[slot]!);
            }
        });

        _storagePanel = new EntityListPanel(town.Storage, null, e => {
            if (playerPawn.Inventory.HasCapacityFor((Item) e)) {
                playerPawn.Inventory.Items.TryTransfer((Item) e);
            }
            else {
                Core.Sim.Gui.PushScreenMessage(new ScreenMessageData {
                    Color = Color.Red, Duration = 2, Text = "Cannot carry, exceeds weight limit"
                });
            }
        }) {
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrame],
            Padding = new Thickness(15),
            VerticalAlignment = VerticalAlignment.Center,
        };


        VerticalStackPanel rightColumn = new() { Visible = !playerPawn.IsDead, GridRow = 1, GridColumn = 2 };
        rightColumn.AddChild(_equipmentPanel);
        rightColumn.AddChild(new HorizontalSeparator() { Margin = new Thickness(0, 50, 0, 20) });
        rightColumn.AddChild(new HorizontalStackPanel {
            Padding = new Thickness(15),
            VerticalAlignment = VerticalAlignment.Center,
            Widgets = { new Label(BaseContent.Styles.Label.Large) { Text = "Storage" } }
        });
        rightColumn.AddChild(_storagePanel);
        Grid grid = new() {
            ShowGridLines = false, HorizontalAlignment = HorizontalAlignment.Center, Padding = new Thickness(50),
            Margin = new Thickness(0, 50, 0, 0), GridLinesColor = Color.Red, RowSpacing = 20,
            DefaultRowProportion = Proportion.Auto, DefaultColumnProportion = Proportion.Auto,
            Widgets = {
                rightColumn,
                _inventoryPanel,
            }
        };
        AddChild(grid);
    }

    public void Update() {
        _storagePanel.Update();
        _equipmentPanel.Update();
        _inventoryPanel.Update();
    }
}