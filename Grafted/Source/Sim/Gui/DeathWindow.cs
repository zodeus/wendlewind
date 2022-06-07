using System.Linq;
using Grafted.Definitions;
using Grafted.Maths;
using Grafted.Sim.Entities;
using Grafted.Sim.Entities.Items;
using Grafted.Sim.Entities.Pawns;
using Grafted.Utils;
using Myra.Graphics2D;
using Myra.Graphics2D.TextureAtlases;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.Styles;

namespace Grafted.Sim.Gui;

public class DeathWindow : Window {
    public DeathWindow() {
        TitleGrid.Visible = false;
        Width = Screen.Width - 100;
        Height = Screen.Height - 100;
        Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.Red];
        Content = new VerticalStackPanel {
            Spacing = 15,
            Padding = new Thickness(50), HorizontalAlignment = HorizontalAlignment.Center,
            Widgets = {
                new Label(BaseContent.Styles.Label.Huge) { Text = "Husk Selection" },
                new Label(BaseContent.Styles.Label.Large) { Text = "Your husk has been destroyed, select a new one" },
                new HorizontalStackPanel {
                    Spacing = 5,
                    Widgets = {
                        new Label(BaseContent.Styles.Label.Large) {
                            TextAlign = TextAlign.Right,
                            Text = $"\\c[{UiTextColor.TextColorGreen}]{Core.Sim.Player.AmountOfItem(Defs.Items.SoulCoin)}",
                            VerticalAlignment = VerticalAlignment.Center
                        },
                        new Image {
                            VerticalAlignment = VerticalAlignment.Center,
                            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Icon.SoulCoin], Width = 32, Height = 32
                        },
                        new Label(BaseContent.Styles.Label.Large) {
                            TextAlign = TextAlign.Right,
                            Text = "Soul Coins available",
                            VerticalAlignment = VerticalAlignment.Center
                        },
                    }
                },
                new HorizontalStackPanel {
                    Spacing = 50,
                    Widgets = {
                        GetBodySelector(Defs.Races.Journeyman),
                        GetBodySelector(Defs.Races.Ghoul)
                    }
                }
            }
        };
    }

    private Widget GetBodySelector(RaceDef raceDef) {
        TextButton button = new(BaseContent.Styles.Button.Large) {
            Text = "Pick",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        button.Click += (_, _) => {
            Core.Sim.Messages.Push(new Message($"\\c[{UiTextColor.TextColorYellow}]You have been reborn as a \\c[{UiTextColor.TextColorGreen}]{raceDef.Label}"));
            Pawn tmpPawn = Core.Sim.PlayerPawn;
            Core.Sim.Player.ResetPawn(WorldGenerator.GeneratePlayerPawn(raceDef, Defs.PawnConfigs.PlayerPawn));

            foreach ((BodyPart? bodyPart, var slots) in tmpPawn.Equipment.Slots) {
                foreach (EquipmentSlotType slot in slots) {
                    if (slot is EquipmentSlotType.BuiltIn) {
                        continue;
                    }

                    if (tmpPawn.Equipment.UnEquip(bodyPart, slot) is { } item) {
                        if (Core.Random.Chance(.92f)) {
                            Core.Sim.Player.Storage.TryAdd(item);
                        }
                    }
                }
            }

            Core.Sim.World.PlayerPawn.Body.Effects.TryApplyEffect(new BodyEffect {
                Def = Defs.BodyEffects.DeathToll,
                TicksLeft = SimTime.HoursToTicks(72)
            });

            Core.Sim.ChangeZone(Defs.Zones.VillageOfTheDamned, false);
        };

        return new VerticalStackPanel {
            Widgets = {
                new Image { Background = new TextureRegion(raceDef.Icon), Width = 128, Height = 128, HorizontalAlignment = HorizontalAlignment.Center },
                new Label(BaseContent.Styles.Label.Medium) {
                    Text = raceDef.Label, HorizontalAlignment = HorizontalAlignment.Center
                },
                new HorizontalStackPanel {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Widgets = {
                        new Label(BaseContent.Styles.Label.Large) {
                            Text = $"\\c[{UiTextColor.TextColorRed}]{raceDef.Species.BaseStats.GetStatValueFromList(Defs.Stats.CurrencyValue)}",
                        },
                        new Image {
                            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Icon.SoulCoin], Width = 32, Height = 32
                        }
                    }
                },
                button
            }
        };
    }
}