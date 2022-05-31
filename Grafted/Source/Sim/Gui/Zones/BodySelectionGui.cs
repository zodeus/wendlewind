using System.Linq;
using Grafted.Definitions;
using Grafted.Sim.Entities;
using Grafted.Sim.Entities.Items;
using Grafted.Sim.Entities.Pawns;
using Grafted.Sim.Entities.Pawns.BodyGenerators;
using Grafted.Sim.Zones;
using Grafted.Utils;
using Microsoft.Xna.Framework;
using Myra.Graphics2D;
using Myra.Graphics2D.TextureAtlases;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.Styles;

namespace Grafted.Sim.Gui.Zones;

public class MeatMarketGui : ZoneGui {
    private class BodyPartPortrait : VerticalStackPanel {
        private readonly BodyPartDef _bodyPartDef;

        public BodyPartPortrait(BodyPartDef bodyPartDef, float totalCoins) {
            _bodyPartDef = bodyPartDef;
            Spacing = 10;

            var bodyPart = new BodyPartSocket(Defs.BodyPartSockets.HandSocket);
            if (bodyPartDef == Defs.BodyParts.HumanHand) {
                HumanBodyGenerator.MakeHandForSocket(bodyPart);
            }
            else {
                GhoulBodyGenerator.MakeHandForSocket(bodyPart);
            }

            int value = (int) bodyPartDef.BaseStats.GetStatValueFromList(Defs.Stats.CurrencyValue)!.Value;
            TextButton purchaseButton = new(BaseContent.Styles.Button.Normal) {
                HorizontalAlignment = HorizontalAlignment.Center,
                Text = $"\\c[{UiTextColor.TextColorGolden}]Purchase",
                Enabled = value <= totalCoins
            };
            purchaseButton.Click += (_, _) => {
                Pawn player = Core.Sim.World.PlayerPawn;
                Window window = new();
                TextButton leftHand = new(BaseContent.Styles.Button.Large) { Text = "Left Hand" };
                leftHand.Click += (_, _) => {
                    BodyPartSocket handSocket = player.Body.AllExternalParts.First(p => p.Type == BodyPartType.Hand && p.Position == BodyPartPosition.Left).Socket!;
                    AttachHand(player, bodyPartDef, handSocket);
                    player.Inventory.Entities.Take(Defs.Items.SoulCoin, value)!.Destroy();
                    window.Close();
                };
                TextButton rightHand = new(BaseContent.Styles.Button.Large) { Text = "Right Hand" };
                rightHand.Click += (_, _) => {
                    BodyPartSocket handSocket = player.Body.AllExternalParts.First(p => p.Type == BodyPartType.Hand && p.Position == BodyPartPosition.Right).Socket!;
                    AttachHand(player, bodyPartDef, handSocket);
                    player.Inventory.Entities.Take(Defs.Items.SoulCoin, value)!.Destroy();
                    window.Close();
                };
                window.Content = new VerticalStackPanel {
                    Spacing = 20, Padding = new Thickness(20),
                    Widgets = {
                        new Label(BaseContent.Styles.Label.Medium) { Text = "Select a hand to replace" },
                        new HorizontalStackPanel { Spacing = 50, Widgets = { leftHand, rightHand } }
                    }
                };
                window.ShowModal(Desktop, (Screen.Center - new Vector2(200, 300)).ToPoint());
            };

            AddChild(new ImageButton() {
                Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.IconFrame], Width = 100, Height = 100,
                Padding = new Thickness(8), HorizontalAlignment = HorizontalAlignment.Center,
                Image = new TextureRegion(bodyPartDef.Icon)
            });
            AddChild(new Label(BaseContent.Styles.Label.Medium) {
                Text = bodyPartDef.Label, HorizontalAlignment = HorizontalAlignment.Center, Wrap = true
            });
            AddChild(new Label() {
                Text = bodyPartDef.Description, HorizontalAlignment = HorizontalAlignment.Center, Wrap = true, Width = 200
            });
            AddChild(new Label() {
                Text = $"Hit Points: {bodyPart.AttachedPart!.GetStatValue(Defs.Stats.MaxHitPoints)}", HorizontalAlignment = HorizontalAlignment.Center, Wrap = true, Width = 200
            });
            AddChild(new Label() {
                Text = $"Sequence Points: {bodyPart.AttachedPart!.SequencePoints}", HorizontalAlignment = HorizontalAlignment.Center, Wrap = true, Width = 200
            });
            AddChild(new HorizontalStackPanel {
                Padding = new Thickness(5, 3, 5, 3),
                VerticalAlignment = VerticalAlignment.Center,
                Width = 80, HorizontalAlignment = HorizontalAlignment.Center,
                Widgets = {
                    new Label(BaseContent.Styles.Label.Medium) {
                        TextAlign = TextAlign.Right,
                        Text = value.ToString(),
                        Width = 40,
                        VerticalAlignment = VerticalAlignment.Center
                    },
                    new Image {
                        VerticalAlignment = VerticalAlignment.Center,
                        Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Icon.SoulCoin], Width = 24, Height = 24
                    }
                },
                Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.IconFrame]
            });
            AddChild(purchaseButton);
        }

        private void AttachHand(Pawn player, BodyPartDef bodyPartDef, BodyPartSocket handSocket) {
            Item? weapon = null;
            Item? armor = null;
            if (handSocket.AttachedPart != null) {
                weapon = player.Equipment.UnEquip(handSocket.AttachedPart, EquipmentSlotType.HandWeapon);
                armor = player.Equipment.UnEquip(handSocket.AttachedPart, EquipmentSlotType.HandArmor);
            }

            handSocket.AttachedPart?.Severe();
            if (bodyPartDef == Defs.BodyParts.HumanHand) {
                HumanBodyGenerator.MakeHandForSocket(handSocket);
            }
            else {
                GhoulBodyGenerator.MakeHandForSocket(handSocket);
            }

            player.Body.BodyPartsDirty = true;

            if (weapon != null) {
                player.Equipment.TryEquip(handSocket.AttachedPart!, weapon);
            }

            if (armor != null) {
                player.Equipment.TryEquip(handSocket.AttachedPart!, armor);
            }

            Core.Sim.Gui!.PushScreenMessage(new ScreenMessageData {
                Color = Color.Goldenrod, Duration = 8, Text = $"                       THE MEAT FORGER WORKS\n" +
                                                              $"Your hand is severed and the {bodyPartDef.Label} has been sown in its place"
            });
        }
    }

    public override void Initialize(Zone zone) {
        TextButton travelHomeButton = new(BaseContent.Styles.Button.Large) {
            HorizontalAlignment = HorizontalAlignment.Center,
            Text = $"Travel Home",
            GridRow = 1, GridColumnSpan = 2
        };
        travelHomeButton.Click += (_, _) => {
            Core.Sim.ChangeZone(Defs.Zones.VillageOfTheDamned);
        };
        float totalCoins = Core.Sim.World.PlayerPawn.Inventory.Entities.AmountOf(Defs.Items.SoulCoin);
        Desktop = new Desktop {
            Root = new VerticalStackPanel {
                Margin = new Thickness(0, 50, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Widgets = {
                    new Label { Text = "Meat Market", Font = BaseContent.Fonts.Fancy.VeryLarge, HorizontalAlignment = HorizontalAlignment.Center },
                    new Grid {
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.Red], Width = 500,
                        RowsProportions = {
                            Proportion.Auto, Proportion.Fill, Proportion.Auto
                        },
                        RowSpacing = 50,
                        Padding = new Thickness(30),
                        Widgets = {
                            new BodyPartPortrait(Defs.BodyParts.HumanHand, totalCoins),
                            new BodyPartPortrait(Defs.BodyParts.GhoulHand, totalCoins) { GridColumn = 1 },
                            travelHomeButton,
                            new HorizontalStackPanel {
                                GridRow = 2, GridColumnSpan = 2,
                                HorizontalAlignment = HorizontalAlignment.Right,
                                Widgets = {
                                    new Label(BaseContent.Styles.Label.Large) {
                                        TextAlign = TextAlign.Right,
                                        Text = "Available: " + totalCoins,
                                        Width = 40,
                                        VerticalAlignment = VerticalAlignment.Center
                                    },
                                    new Image {
                                        VerticalAlignment = VerticalAlignment.Center,
                                        Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Icon.SoulCoin], Width = 40, Height = 40
                                    }
                                }
                            }
                        }
                    }
                }
            },
            HasExternalTextInput = true
        };
    }
}