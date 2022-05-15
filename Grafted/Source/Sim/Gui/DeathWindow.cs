using Grafted.Definitions;
using Grafted.Sim.Entities.Items;
using Grafted.Sim.Entities.Pawns;
using Grafted.Utils;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.Styles;

namespace Grafted.Sim.Gui;

public class DeathWindow : Window {
    public DeathWindow() {
        TextButton button = new(BaseContent.Styles.Button.Large) { Text = "Resurrect" };
        button.Click += (_, _) => {
            Core.Sim.Messages.Push(new Message($"\\c[{UiTextColor.TextColorYellow}]You have been reborn"));
            Pawn tmpPawn = Core.Sim.World.PlayerPawn;
            Core.Sim.World.PlayerPawns.Clear();
            Core.Sim.World.PlayerPawns.Add(WorldGenerator.GeneratePlayerPawn(Defs.PawnConfigs.PlayerPawn));

            foreach ((BodyPart? bodyPart, var slots) in tmpPawn.Equipment.Slots) {
                foreach (EquipmentSlotType slot in slots) {
                    if (slot is EquipmentSlotType.BuiltIn) {
                        continue;
                    }

                    if (tmpPawn.Equipment.UnEquip(bodyPart, slot) is { } item) {
                        Core.Sim.World.PlayerPawn.Equipment.TryEquip(bodyPart, slot, item);
                    }
                }
            }

            Core.Sim.World.PlayerPawn.Body.Effects.TryApplyEffect(new BodyEffect {
                Def = Defs.BodyEffects.DeathToll,
                TicksLeft = SimTime.HoursToTicks(72)
            });

            Core.Sim.ChangeZone(Defs.Zones.VillageOfTheDamned, false);
        };
        TitleGrid.Visible = false;
        Width = Screen.Width - 100;
        Height = Screen.Height - 100;
        Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.Red];
        Content = new VerticalStackPanel {
            Spacing = 15,
            Padding = new Thickness(50), HorizontalAlignment = HorizontalAlignment.Center,
            Widgets = {
                new Label(BaseContent.Styles.Label.Large) { Text = "You died" },
                button
            }
        };
    }
}