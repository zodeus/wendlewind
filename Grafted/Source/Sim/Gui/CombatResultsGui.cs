using System;
using System.Linq;
using Grafted.Definitions;
using Grafted.Maths;
using Grafted.Sim.Combat;
using Grafted.Sim.Entities;
using Grafted.Sim.Entities.Items;
using Grafted.Sim.Entities.Pawns;
using Grafted.Sim.Gui.EntityWidgets;
using Grafted.Sim.Gui.EntityWidgets.PawnWidgets;
using Grafted.Sim.Gui.MiscWidgets;
using Grafted.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.Styles;
using HorizontalAlignment = Myra.Graphics2D.UI.HorizontalAlignment;
using Keys = Microsoft.Xna.Framework.Input.Keys;
using Label = Myra.Graphics2D.UI.Label;

namespace Grafted.Sim.Gui;

public class CombatResultsGui : BaseGui {
    private readonly CombatEvent _combatEvent;
    private readonly PawnBodyPanel _bodyPanel;
    private readonly PawnInventoryPanel _inventoryPanel;
    private readonly PawnEquipmentPanel _equipmentPanel;
    private readonly EntityListPanel _lootPanel;

    public CombatResultsGui(CombatEvent combatEvent) {
        _combatEvent = combatEvent;
        Pawn playerPawn = combatEvent.PlayerPawns[0];
        Label title = new(BaseContent.Styles.Label.Large) {
            GridRow = 0, GridColumn = 0, GridColumnSpan = 2,
            Text = $"Total kills \\c[{CombatSequence.TextColorGreen}]{Core.Sim.World.TotalKills}",
            HorizontalAlignment = HorizontalAlignment.Center
        };

        _bodyPanel = new PawnBodyPanel(playerPawn.Body, BodyPartClickHandler) {
            GridRow = 1, GridColumn = 0
        };

        _inventoryPanel = new PawnInventoryPanel(
            playerPawn,
            entity => {
                if (MouseAttachment == null && Input.IsKeyDown(Keys.LeftShift)) {
                    if (entity is not Item item || item.Def.Moniker == "Cauterize") {
                        return;
                    }

                    playerPawn.Inventory.Items.Remove(item);
                    item.Destroy();
                    return;
                }

                MouseAttachment = new MouseAttachment(entity.Icon, updateAction: attachment => {
                    if (Input.RightMouseButtonPressed) attachment.Detach();
                }) {
                    IconSize = new Size(40, 40),
                    Data = entity,
                };
            }, entity => {
                _combatEvent.Loot.TryTransfer((Item) entity);
            }
        ) {
            Visible = !playerPawn.IsDead,
            MinHeight = 700,
            Width = 300, GridRow = 1, GridColumn = 1
        };

        _equipmentPanel = new PawnEquipmentPanel(playerPawn.Equipment, (part, slot) => {
            // UnEquip
            if (MouseAttachment == null && Input.RightMouseButtonReleased && slot != EquipmentSlotType.BuiltIn) {
                Item? unEquippedItem = playerPawn.Equipment.UnEquip(part, slot);
                if (unEquippedItem != null) {
                    playerPawn.Inventory.Items.TryAdd(unEquippedItem);
                }
            }

            if (MouseAttachment?.Data is Item item) {
                if (item.Def == Defs.Items.RepairKit) {
                    if (playerPawn.Equipment.GetBySlot(part, slot) is { } equipmentItem && equipmentItem.Durability < equipmentItem.MaxDurability) {
                        equipmentItem.Repair();
                        item.StackSize--;
                        if (item.StackSize == 0) {
                            item.Destroy();
                            MouseAttachment.Detach();
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

                    MouseAttachment.Detach();
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

        _lootPanel = new EntityListPanel(combatEvent.Loot, null, e => {
            if (playerPawn.Inventory.HasCapacityFor((Item) e)) {
                playerPawn.Inventory.Items.TryTransfer((Item) e);
            }
            else {
                Core.Sim.Gui.PushScreenMessage(new ScreenMessageData {
                    Color = Color.Red, Duration = 2, Text = "Cannot carry, exceeds weight limit"
                });
            }
        }) {
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.DeepGold],
            Padding = new Thickness(15),
            VerticalAlignment = VerticalAlignment.Center,
        };

        Widget progressButton = GenerateProgressButton();
        progressButton.GridRow = 2;
        progressButton.GridColumn = 2;

        VerticalStackPanel rightColumn = new() { Visible = !playerPawn.IsDead, GridRow = 1, GridColumn = 2 };
        rightColumn.AddChild(_equipmentPanel);
        rightColumn.AddChild(new HorizontalSeparator() { Margin = new Thickness(0, 50, 0, 20) });
        rightColumn.AddChild(new HorizontalStackPanel {
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.DeepGold],
            Padding = new Thickness(15),
            VerticalAlignment = VerticalAlignment.Center,
            Widgets = { new Label(BaseContent.Styles.Label.Large) { Text = "Loot" } }
        });
        rightColumn.AddChild(_lootPanel);
        Grid grid = new() {
            ShowGridLines = false, HorizontalAlignment = HorizontalAlignment.Center, Padding = new Thickness(50),
            Margin = new Thickness(0, 50, 0, 0), GridLinesColor = Color.Red, RowSpacing = 20,
            DefaultRowProportion = Proportion.Auto, DefaultColumnProportion = Proportion.Auto,
            Widgets = {
                title,
                rightColumn,
                _bodyPanel,
                _inventoryPanel,
                progressButton
            }
        };

        Desktop = new Desktop { Root = grid, HasExternalTextInput = true };
    }

    private Widget DeathsButton() {
        ImageButton image = new(BaseContent.Styles.Button.Large) {
            Image = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Icon.Skull], Width = 32, Height = 32,
            Padding = new Thickness(10)
        };
        image.TouchDown += (_, _) => {
            new PawnDeathRecordsWindow(Core.Sim.World.DeathRecords).Show(Desktop);
        };
        return image;
    }

    private void BodyPartClickHandler(BodyPartSocket socket) {
        if (Input.RightMouseButtonReleased) {
            return;
        }

        if (MouseAttachment == null) {
            if (socket.AttachedPart != null) {
                ViewEntity(socket.AttachedPart);
            }

            return;
        }

        if (MouseAttachment.Data is Item item == false) {
            return;
        }

        //Handle Cauterize
        if (socket.AttachedPart == null && socket.IsSealed == false && item.Def == Defs.Items.Cauterize) {
            socket.IsSealed = true;
            //MouseAttachment.Detach();
            return;
        }

        if (socket.AttachedPart is not { } part) {
            return;
        }

        //Handle MendersMist
        if (item.Def == Defs.Items.MendersMist) {
            item.StackSize--;
            if (item.StackSize == 0) {
                item.Destroy();
                MouseAttachment.Detach();
            }

            int mistJuice = 200;

            int UpdateHealth(BodyPart bodyPart) {
                int currentHealth = bodyPart.HitPoints;
                bodyPart.HitPoints += Math.Min(bodyPart.MaxHitPoints - bodyPart.HitPoints, mistJuice);
                return bodyPart.HitPoints - currentHealth;
            }

            void DoMisting(BodyPart bodyPart) {
                if (mistJuice <= 0) {
                    return;
                }

                mistJuice -= UpdateHealth(bodyPart);
                foreach (BodyPart internalPart in bodyPart.InternalParts) {
                    if (internalPart.IsBone || internalPart.Type is BodyPartType.Skin) {
                        mistJuice -= UpdateHealth(internalPart);
                    }
                }

                foreach (BodyPart externalPart in bodyPart.ExternalParts) {
                    DoMisting(externalPart);
                }
            }

            DoMisting(socket.AttachedPart);
        }

        //Handle MedKit
        if (item.Def == Defs.Items.MedKit) {
            if (part.HealthPercent >= 1) {
                return;
            }

            item.StackSize--;
            if (item.StackSize == 0) {
                item.Destroy();
                MouseAttachment.Detach();
            }

            socket.AttachedPart.HitPoints = socket.AttachedPart.MaxHitPoints;
            foreach (BodyPart internalPart in socket.AttachedPart.InternalParts) {
                internalPart.HitPoints = internalPart.MaxHitPoints;
            }
        }

        //Handle ArterialThreads
        if (item.Def == Defs.Items.ArterialThreads) {
            bool wasConsumed = false;
            foreach (BodyPart internalPart in socket.AttachedPart.InternalParts) {
                if (internalPart.Type == BodyPartType.Artery && internalPart.HealthPercent < 1) {
                    wasConsumed = true;
                    internalPart.HitPoints = internalPart.MaxHitPoints;
                }
            }

            if (wasConsumed) {
                item.StackSize--;
                if (item.StackSize == 0) {
                    item.Destroy();
                    MouseAttachment.Detach();
                }
            }
        }
    }

    private Widget GenerateProgressButton() {
        HorizontalStackPanel buttons = new() {
            Spacing = 5,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Bottom
        };
        if (_combatEvent.PlayerPawns.First().IsDead) {
            TextButton button = new(BaseContent.Styles.Button.Large) {
                Text = "You dead son"
            };
            button.Click += (_, _) => ((GameScene) Core.Scene.ActiveScene!).QuickPlay();
            buttons.AddChild(button);
        }
        else {
            TextButton continueButton = new(BaseContent.Styles.Button.Large) {
                Text = "Carry on"
            };
            continueButton.Click += (_, _) => {
                if (Core.Sim.World.CurrentZone.Def == Defs.Zones.Intro) {
                    HandleIntro();
                    return;
                }

                Core.Sim.Gui = new CombatGui(Core.Sim.World.NextCombat());
            };
            buttons.AddChild(continueButton);
            if (Core.Sim.World.CurrentZone.Def != Defs.Zones.Intro) {
                TextButton goHome = new(BaseContent.Styles.Button.Large) { Text = "Go Home" };
                goHome.Click += (_, _) => {
                    Core.Sim.World.MoveToZone(Defs.Zones.VillageOfTheDamned);
                    Core.Sim.Gui = new TownGui(Core.Sim.World.CurrentZone.Town!);
                };
                buttons.AddChild(goHome);
            }

        }

        return new HorizontalStackPanel { Spacing = 10, Widgets = { DeathsButton(), buttons } };
    }

    private void HandleIntro() {
        if (Core.Sim.World.TotalKills < 15) {
            Core.Sim.Gui = new CombatGui(Core.Sim.World.NextCombat());
            return;
        }

        Core.Sim.Gui = new DialogueGui(Core.Sim.World.NextDialogue());
    }

    public override void Render(SpriteBatch spriteBatch, float deltaTime) {
        _bodyPanel.Update();
        _inventoryPanel.Update();
        _equipmentPanel.Update();
        _lootPanel.Update();
        base.Render(spriteBatch, deltaTime);
    }
}