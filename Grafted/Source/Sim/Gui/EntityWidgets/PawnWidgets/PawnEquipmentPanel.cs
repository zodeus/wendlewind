using System;
using System.Collections.Generic;
using System.Linq;
using Grafted.Sim.Entities.Items;
using Grafted.Sim.Entities.Pawns;
using Microsoft.Xna.Framework;
using Myra.Graphics2D;
using Myra.Graphics2D.TextureAtlases;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.Styles;

namespace Grafted.Sim.Gui.EntityWidgets.PawnWidgets;

public class PawnEquipmentPanel : HorizontalStackPanel {
    private readonly PawnEquipment _equipment;
    private readonly Dictionary<BodyPart, EquipmentColumn> _panels = new();
    private static readonly Color DestroyedEquipmentColor = new(255, 0, 0, 15);

    public PawnEquipmentPanel(PawnEquipment equipment, Action<BodyPart, EquipmentSlotType>? clickAction = null) {
        _equipment = equipment;
        Spacing = 2;
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
        private Dictionary<ItemDef, ColoredRegion> _iconCache = new();
        private IImage _potionSlotIcon;

        public EquipmentColumn(BodyPart bodyPart, List<EquipmentSlotType> slots, Action<BodyPart, EquipmentSlotType>? clickAction = null) {
            _bodyPart = bodyPart;
            ClickAction = clickAction;
            Spacing = 2;
            _potionSlotIcon = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Icon.PotionSlot];
            _image = new Image { Background = new ColoredRegion(new TextureRegion(bodyPart.Icon), BodyPartColor.Get(bodyPart)), Width = 32, Height = 32 };
            _image.TouchDown += (_, _) => Core.Sim.Gui!.ViewEntity(bodyPart);
            AddChild(_image);
            foreach (EquipmentSlotType slot in slots) {
                ImageButton slotFrame = new(BaseContent.Styles.Button.Icon) { Width = 32, Height = 32 };
                _slots.Add(slot, slotFrame);
                slotFrame.Click += (_, _) => ClickAction?.Invoke(bodyPart, slot);
                AddChild(slotFrame);
            }
        }

        public void Update() {
            foreach ((EquipmentSlotType slot, ImageButton? image) in _slots) {
                if (_bodyPart.Equipment[slot] is { } item && item.IsDestroyed == false) {
                    if (_iconCache.ContainsKey(item.ItemDef) == false) {
                        _iconCache[item.ItemDef] = new ColoredRegion(new TextureRegion(item.Icon), Color.White);
                    }

                    image.Image = _iconCache[item.ItemDef];
                    ((ColoredRegion) image.Image).Color = GetEquipmentColor(item, _bodyPart);
                }
                else {
                    image.Image = slot is EquipmentSlotType.PotionSlot1 or EquipmentSlotType.PotionSlot2 ? _potionSlotIcon : null;
                }
            }

            ((ColoredRegion) _image.Background).Color = BodyPartColor.Get(_bodyPart);
        }
    }

    private static Color GetEquipmentColor(Item item, BodyPart bodyPart) {
        if (item.IsDestroyed) {
            return DestroyedEquipmentColor;
        }

        if (item.ItemDef.EquipmentProperties.EquipmentType == EquipmentType.Tool && bodyPart.HasMobility == false) {
            return Color.Red;
        }

        return Color.Lerp(DestroyedEquipmentColor, Color.White, item.Durability / item.MaxDurability);
    }
}