using System;
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
    private readonly Dictionary<BodyPart, EquipmentColumn> _panels = new();

    public PawnEquipmentPanel(PawnEquipment equipment, Action<BodyPart, EquipmentSlotType>? clickAction = null) {
        _equipment = equipment;
        foreach ((BodyPart bodyPart, List<EquipmentSlotType> slots) in _equipment.Slots) {
            if (slots.Any() == false) {
                continue;
            }

            EquipmentColumn partPanel = new(bodyPart, slots, clickAction);
            AddChild(partPanel);
            _panels.Add(bodyPart, partPanel);
        }
    }

    public void Update() {
        foreach ((BodyPart? bodyPart, EquipmentColumn? widget) in _panels) {
            widget.Update();
            if (bodyPart.IsSevered) {
                _panels.Remove(bodyPart);
                widget.RemoveFromParent();
            }
        }
    }

    private class EquipmentColumn : VerticalStackPanel {
        private readonly BodyPart _bodyPart;
        private readonly Dictionary<EquipmentSlotType, ImageButton> _slots = new();
        private readonly Image _image;
        private event Action<BodyPart, EquipmentSlotType>? ClickAction;

        public EquipmentColumn(BodyPart bodyPart, List<EquipmentSlotType> slots, Action<BodyPart, EquipmentSlotType>? clickAction = null) {
            _bodyPart = bodyPart;
            ClickAction = clickAction;
            _image = new Image { Background = new ColoredRegion(new TextureRegion(bodyPart.Icon), BodyPartColor.Get(bodyPart)), Width = 32, Height = 32 };
            AddChild(_image);
            foreach (EquipmentSlotType slot in slots) {
                ImageButton slotFrame = new(BaseContent.Styles.Button.Icon) { Width = 32, Height = 32 };
                _slots.Add(slot, slotFrame);
                slotFrame.Click += (_, _) => {
                    ClickAction?.Invoke(bodyPart, slot);
                    if (bodyPart.Equipment[slot] is { } item) {
                        slotFrame.Image = new ColoredRegion(new TextureRegion(item.Icon), GetEquipmentColor(item));
                    }
                };
                AddChild(slotFrame);
            }
        }

        public void Update() {
            foreach ((EquipmentSlotType slot, ImageButton? image) in _slots) {
                if (_bodyPart.Equipment[slot] is { } item) {
                    if (image.Image == null) {
                        image.Image = new ColoredRegion(new TextureRegion(item.Icon), GetEquipmentColor(item));
                    }

                    ((ColoredRegion) image.Image).Color = GetEquipmentColor(item);
                }
                else {
                    image.Image = null;
                }
            }

            ((ColoredRegion) _image.Background).Color = BodyPartColor.Get(_bodyPart);
        }
    }

    private static Color GetEquipmentColor(Item item) {
        if (item.IsDestroyed) {
            return Color.Black;
        }

        return Color.Lerp(Color.Black, Color.White, item.Durability / item.MaxDurability);
    }
}