using System;
using System.Collections.Generic;
using System.Linq;
using Grafted.Definitions;
using Grafted.Maths;
using Grafted.Sim.Combat;
using Grafted.Sim.Entities;
using Grafted.Sim.Entities.Items;
using Grafted.Sim.Entities.Pawns;
using Grafted.Sim.Gui.EntityWidgets.PawnWidgets;
using Grafted.UI;
using Grafted.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Myra.Graphics2D;
using Myra.Graphics2D.TextureAtlases;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.Styles;

namespace Grafted.Sim.Gui.CombatGuis;

public class CombatResultsGui : SimulationGui {
    private readonly CombatEvent _combatEvent;
    private readonly PawnBodyPanel _bodyPanel;
    private readonly PawnInventoryPanel _inventoryPanel;
    private readonly PawnEquipmentPanel _equipmentPanel;

    public CombatResultsGui(CombatEvent combatEvent) {
        _combatEvent = combatEvent;
        UpdateWorldStuff();
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

        _equipmentPanel = new PawnEquipmentPanel(playerPawn.Equipment) {
            Width = 250, GridRow = 1, GridColumn = 2
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

    private void UpdateWorldStuff() {
        Pawn playerPawn = _combatEvent.PlayerPawns.First();
        playerPawn.Body.BloodAmount = playerPawn.Body.MaxBlood;

        // GET GLOVEY
        if (Core.Sim.World.TotalKills > 8) {
            BodyPart head = playerPawn.Body.AllExternalParts.First(p => p.Type == BodyPartType.Head);
            Item bucket = EntityGenerator.CreateEntity<Item>(DefRepository<ItemDef>.GetByMoniker("BucketHelmet")!);
            playerPawn.Equipment.TryEquip(head, bucket);
        }

        if (Core.Sim.World.TotalKills == 2) {
            var hand = playerPawn.Body.AllExternalParts.Where(p => p.Type == BodyPartType.Hand).ToList()[0];
            ItemDef gloveDef = DefRepository<ItemDef>.GetByMoniker("LeatherGlove")!;
            Item glove = EntityGenerator.CreateEntity<Item>(gloveDef);
            playerPawn.Equipment.TryEquip(hand, glove);
            
            Item mendersMist = EntityGenerator.CreateEntity<Item>(DefRepository<ItemDef>.GetByMoniker("MendersMist")!);
            mendersMist.StackSize = 2;
            playerPawn.Inventory.Items.TryAdd(mendersMist);
        }

        if (Core.Sim.World.TotalKills == 6) {
            var tmp = playerPawn.Body.AllExternalParts.Where(p => p.Type == BodyPartType.Hand).ToList();
            if (tmp.Count > 1) {
                var hand = playerPawn.Body.AllExternalParts.Where(p => p.Type == BodyPartType.Hand).ToList()[1];
                ItemDef gloveDef = DefRepository<ItemDef>.GetByMoniker("LeatherGlove")!;
                Item glove = EntityGenerator.CreateEntity<Item>(gloveDef);
                playerPawn.Equipment.TryEquip(hand, glove);
            }
        } 
        
        if (Core.Sim.World.TotalKills == 10) {
            var tmp = playerPawn.Body.AllExternalParts.Where(p => p.Type == BodyPartType.Hand).ToList();
            if (tmp.Count > 1) {
                var hand = playerPawn.Body.AllExternalParts.Where(p => p.Type == BodyPartType.Hand).ToList()[1];
                ItemDef weaponDef = DefRepository<ItemDef>.GetByMoniker("Mace")!;
                Item mace = EntityGenerator.CreateEntity<Item>(weaponDef);
                playerPawn.Equipment.TryEquip(hand, mace);
            }
        }

        foreach (BodyPart part in playerPawn.Body.AllParts) {
            if (part.HealthPercent >= .97) { continue; }

            if (part.Type == BodyPartType.Skin) {
                part.HitPoints += part.MaxHitPoints * Core.Random.NextFloat(0.10f, 0.25f);
                continue;
            }

            if (part.IsDestroyed) {
                /*if (Core.Random.Chance(.04f)) {
                    part.HitPoints = 1;
                }*/

                continue;
            }

            part.HitPoints += part.MaxHitPoints * Core.Random.NextFloat(0.03f, 0.08f);
        }
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
            item.StackSize--;
            if (item.StackSize == 0) {
                item.Destroy();
                MouseAttachment.Detach();
            }

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

            float mistJuice = 150;

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
                    if (internalPart.Type is BodyPartType.Bone or BodyPartType.Skin) {
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
                Pawn playerPawn = _combatEvent.PlayerPawns.First();
                CombatEvent newCombatEvent = new();
                //combatEvent.IsInteractive = true;
                newCombatEvent.AddPlayerPawn(playerPawn);
                Pawn pawn = PawnGenerator.CreatePawn(new PawnRequest { Race = DefRepository<RaceDef>.Defs.Where(r => r.Species == Defs.Species.Skeleton).RandomElement() });
                ItemDef weaponDef;
                if (Core.Sim.World.TotalKills > 8) {
                    weaponDef = DefRepository<ItemDef>.GetByMoniker("Mace")!;
                }
                else {
                    weaponDef = DefRepository<ItemDef>.GetByMoniker("WoodenStick")!;
                }

                Item weapon = EntityGenerator.CreateEntity<Item>(weaponDef);
                pawn.Equipment.TryEquip(pawn.Body.AllParts.First(p => p.SlotFor(weapon) != null), weapon);

                newCombatEvent.AddEnemyPawn(pawn);
                CombatGui gui = new(newCombatEvent);
                Core.Sim.Gui = gui;
                newCombatEvent.StartAsCoroutine();
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