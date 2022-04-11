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
using Myra.Graphics2D.UI;

namespace Grafted.Sim.Gui.CombatGuis;

public class CombatResultsGui : SimulationGui {
    private readonly CombatEvent _combatEvent;
    private readonly PawnBodyPanel _bodyPanel;
    private readonly PawnInventoryPanel _inventoryPanel;

    public CombatResultsGui(CombatEvent combatEvent) {
        _combatEvent = combatEvent;
        Pawn playerPawn = combatEvent.PlayerPawns.First();

        Label title = new(BaseContent.Styles.Label.Large) {
            GridRow = 0, GridColumn = 0, GridColumnSpan = 2,
            Text = $"Total kills \\c[{CombatSequence.TextColorGreen}]{Core.Sim.World.TotalKills}",
            HorizontalAlignment = HorizontalAlignment.Center
        };

        _bodyPanel = new PawnBodyPanel(playerPawn.Body, BodyPartClickHandler) {
            GridRow = 1, GridColumn = 0,
        };

        _inventoryPanel = new PawnInventoryPanel(
            playerPawn.Inventory,
            entity => {
                Core.Sim.Gui!.MouseAttachment = new MouseAttachment(entity.Icon) {
                    IconSize = new Size(40, 40),
                    Data = entity
                };
            }
        ) {
            Width = 250, GridRow = 1, GridColumn = 1
        };

        Widget progressButton = GenerateProgressButton();
        progressButton.GridRow = 1;
        progressButton.GridColumn = 2;

        Grid grid = new() {
            ShowGridLines = false, HorizontalAlignment = HorizontalAlignment.Center, Padding = new Thickness(50),
            Margin = new Thickness(0, 50, 0, 0), GridLinesColor = Color.Red, RowSpacing = 20,
            DefaultRowProportion = Proportion.Auto, DefaultColumnProportion = Proportion.Auto,
            Widgets = {
                title,
                _inventoryPanel,
                _bodyPanel,
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

            //todo THIS NEEDS TO MOVE
            var playerPawn = _combatEvent.PlayerPawns.First();
            playerPawn.Body.BloodAmount = playerPawn.Body.MaxBlood;

            // GET GLOVEY
            if (Core.Sim.World.TotalKills > 0) {
                var head = playerPawn.Body.AllExternalParts.First(p => p.Type == BodyPartType.Head);
                var bucket = EntityGenerator.CreateEntity<Item>(DefRepository<ItemDef>.GetByMoniker("BucketHelmet")!);
                playerPawn.Equipment.TryEquip(head, bucket);
            }

            if (Core.Sim.World.TotalKills == 2) {
                var hands = playerPawn.Body.AllExternalParts.Where(p => p.Type == BodyPartType.Hand);
                foreach (BodyPart hand in hands) {
                    var glove = EntityGenerator.CreateEntity<Item>(DefRepository<ItemDef>.GetByMoniker("LeatherGlove")!);
                    playerPawn.Equipment.TryEquip(hand, glove);
                }
            }

            playerPawn.Equipment.OnBodyChanged();
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
            //todo END

            button.Click += (_, _) => {
                var newCombatEvent = new CombatEvent();
                //combatEvent.IsInteractive = true;

                newCombatEvent.AddPlayerPawn(playerPawn);
                var pawn = PawnGenerator.CreatePawn(new PawnRequest { Race = DefRepository<RaceDef>.Defs.Where(r => r.Species == Defs.Species.Skeleton).RandomElement() });
                var hand1 = EntityGenerator.CreateEntity<Item>(DefRepository<ItemDef>.GetByMoniker("FleshyHand")!);
                var hand2 = EntityGenerator.CreateEntity<Item>(DefRepository<ItemDef>.GetByMoniker("FleshyHand")!);
                var foot1 = EntityGenerator.CreateEntity<Item>(DefRepository<ItemDef>.GetByMoniker("FleshyFoot")!);
                var foot2 = EntityGenerator.CreateEntity<Item>(DefRepository<ItemDef>.GetByMoniker("FleshyFoot")!);
                pawn.Equipment.TryEquip(pawn.Body.AllParts.Where(p => p.SlotFor(hand1) != null).ToList()[0], hand1);
                pawn.Equipment.TryEquip(pawn.Body.AllParts.Where(p => p.SlotFor(hand2) != null).ToList()[1], hand2);
                pawn.Equipment.TryEquip(pawn.Body.AllParts.Where(p => p.SlotFor(foot1) != null).ToList()[0], foot1);
                pawn.Equipment.TryEquip(pawn.Body.AllParts.Where(p => p.SlotFor(foot2) != null).ToList()[1], foot2);

                ItemDef weaponDef;
                if (Core.Sim.World.TotalKills > 9) {
                    weaponDef = DefRepository<ItemDef>.GetByMoniker("Mace")!;
                }
                else {
                    weaponDef = DefRepository<ItemDef>.GetByMoniker("WoodenStick")!;
                }

                var weapon = EntityGenerator.CreateEntity<Item>(weaponDef);
                pawn.Equipment.TryEquip(pawn.Body.AllParts.First(p => p.SlotFor(weapon) != null), weapon);

                newCombatEvent.AddEnemyPawn(pawn);
                var gui = new CombatGui(newCombatEvent);
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