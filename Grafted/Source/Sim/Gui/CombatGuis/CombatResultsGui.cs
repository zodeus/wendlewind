using System;
using System.Linq;
using Grafted.Definitions;
using Grafted.Maths;
using Grafted.Sim.Combat;
using Grafted.Sim.Entities.Items;
using Grafted.Sim.Entities.Pawns;
using Grafted.Sim.Gui.EntityWidgets.PawnWidgets;
using Grafted.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;

namespace Grafted.Sim.Gui.CombatGuis;

public class CombatResultsGui : SimulationGui {
    private readonly CombatEvent _combatEvent;
    private readonly PawnBodyPanel _bodyPanel;
    private readonly PawnInventoryPanel _inventoryPanel;
    private readonly PawnEquipmentPanel _equipmentPanel;

    public CombatResultsGui(CombatEvent combatEvent) {
        _combatEvent = combatEvent;
        Pawn playerPawn = combatEvent.PlayerPawns.First();

        Label title = new(BaseContent.Styles.Label.Large) {
            GridRow = 0, GridColumn = 0, GridColumnSpan = 2,
            Text = $"Total kills \\c[{CombatSequence.TextColorGreen}]{Core.Sim.World.TotalKills}",
            HorizontalAlignment = HorizontalAlignment.Center
        };

        _bodyPanel = new PawnBodyPanel(playerPawn.Body, BodyPartClickHandler) {
            GridRow = 1, GridColumn = 0
        };

        _inventoryPanel = new PawnInventoryPanel(
            playerPawn.Inventory,
            entity => {
                Core.Sim.Gui!.MouseAttachment = new MouseAttachment(entity.Icon, updateAction: attachment => {
                    if (Input.RightMouseButtonPressed) attachment.Detach();
                }) {
                    IconSize = new Size(40, 40),
                    Data = entity,
                };
            }
        ) {
            Width = 300, GridRow = 1, GridColumn = 1
        };

        _equipmentPanel = new PawnEquipmentPanel(playerPawn.Equipment, (part, type) => {
            if (Core.Sim.Gui!.MouseAttachment?.Data is Item item) {
                if (item.ItemDef.EquipmentProperties.SlotUsedToEquip == type) {
                    playerPawn.Inventory.Items.Remove(item);
                    Item? unEquippedItem = playerPawn.Equipment.TryEquip(part, item);
                    Core.Sim.Gui!.MouseAttachment.Detach();
                    if (unEquippedItem != null) {
                        playerPawn.Inventory.Items.TryAdd(unEquippedItem);
                    }
                }

                return;
            }

            if (part.Equipment[type] != null) {
                Core.Sim.Gui!.ViewEntity(part.Equipment[type]!);
            }
        }) {
            GridRow = 1, GridColumn = 2
        };

        Widget progressButton = GenerateProgressButton();
        progressButton.GridRow = 2;
        progressButton.GridColumn = 2;

        Grid grid = new() {
            ShowGridLines = false, HorizontalAlignment = HorizontalAlignment.Center, Padding = new Thickness(50),
            Margin = new Thickness(0, 50, 0, 0), GridLinesColor = Color.Red, RowSpacing = 20,
            DefaultRowProportion = Proportion.Auto, DefaultColumnProportion = Proportion.Auto,
            Widgets = {
                title,
                _equipmentPanel,
                _bodyPanel,
                _inventoryPanel,
                progressButton
            }
        };

        Desktop = new Desktop { Root = grid, HasExternalTextInput = true };
        //todo fairly certain there is an issue here, deregister this event when gui's change?
        Core.Instance.Window.TextInput += (_, a) => {
            Desktop.OnChar(a.Character);
        };
    }

    private void BodyPartClickHandler(BodyPartSocket socket) {
        if (Input.RightMouseButtonPressed) {
            return;
        }

        if (MouseAttachment == null) {
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

            float mistJuice = 200;

            float UpdateHealth(BodyPart bodyPart) {
                float currentHealth = bodyPart.HitPoints;
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
        TextButton button;
        if (_combatEvent.PlayerPawns.First().IsDead) {
            button = new TextButton(BaseContent.Styles.Button.Large) {
                Text = "You dead son",
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Bottom,
            };
            button.Click += (_, _) => ((GameScene) Core.Scene.ActiveScene).QuickPlay();
        }
        else {
            button = new TextButton(BaseContent.Styles.Button.Large) {
                Text = "Carry on",
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Bottom,
            };
            button.Click += (_, _) => {
                Core.Sim.Gui = new CombatGui(Core.Sim.World.NextCombat());
            };
        }

        return button;
    }

    public override void Render(SpriteBatch spriteBatch) {
        MouseAttachment?.Update();
        _bodyPanel.Update();
        _inventoryPanel.Update();
        _equipmentPanel.Update();
        Desktop.Render();
        spriteBatch.Begin(
            SpriteSortMode.Deferred,
            BlendState.NonPremultiplied,
            SamplerState.PointClamp,
            DepthStencilState.None,
            RasterizerState.CullNone
        );
        MouseAttachment?.Render(spriteBatch);
        spriteBatch.End();
    }
}