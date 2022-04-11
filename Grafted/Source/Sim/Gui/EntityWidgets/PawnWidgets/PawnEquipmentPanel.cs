using System.Collections.Generic;
using System.Linq;
using Grafted.Sim.Entities.Items;
using Grafted.Sim.Entities.Pawns;
using Microsoft.Xna.Framework;
using Myra.Graphics2D.TextureAtlases;
using Myra.Graphics2D.UI;

namespace Grafted.Sim.Gui.EntityWidgets.PawnWidgets;

public class PawnEquipmentPanel : HorizontalStackPanel {
    private readonly PawnEquipment _equipment;
    private readonly Dictionary<BodyPart, Widget> _panels = new();

    public PawnEquipmentPanel(PawnEquipment equipment) {
        _equipment = equipment;
        foreach ((BodyPart bodyPart, List<EquipmentSlotType> slots) in _equipment.Slots) {
            if (slots.Any() == false) {
                continue;
            }

            VerticalStackPanel partPanel = new();
            Image image = new() { Background = new ColoredRegion(new TextureRegion(bodyPart.Icon), Color.White), Width = 32, Height = 32 };
            partPanel.AddChild(image);
            foreach (EquipmentSlotType slot in slots) {
                new Label() {
                    Text = slot.ToString(),
                };
                var slotFrame = new ImageButton(BaseContent.Styles.Button.Icon) { Width = 32, Height = 32 };
                partPanel.AddChild(slotFrame);
                if (bodyPart.Equipment[slot] is { } item) {
                    slotFrame.Click += (_, _) => {
                        Core.Sim.Gui!.ViewEntity(item);
                    };
                    slotFrame.Image = new TextureRegion(item.Icon);
                }
            }

            AddChild(partPanel);
            _panels.Add(bodyPart, partPanel);
        }
    }

    public void Update() {
        foreach ((BodyPart? bodyPart, Widget? widget) in _panels) {
            if (bodyPart.IsSevered) {
                _panels.Remove(bodyPart);
                widget.RemoveFromParent();
            }
        }
    }
}