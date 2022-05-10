using System;
using System.Collections.Generic;
using System.Linq;
using Grafted.Definitions;
using Grafted.Sim.Entities;
using Grafted.Sim.Entities.Items;
using Grafted.Sim.Entities.Pawns;
using Grafted.UI;
using Microsoft.Xna.Framework;
using Myra.Graphics2D;
using Myra.Graphics2D.TextureAtlases;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.Styles;

namespace Grafted.Sim.Gui.EntityWidgets.PawnWidgets;

public class PawnEquipmentPanel : HorizontalStackPanel {
    private readonly Pawn _pawn;
    private readonly Dictionary<BodyPart, EquipmentColumn> _panels = new();
    private static readonly Color DestroyedEquipmentColor = new(255, 0, 0, 15);

    public PawnEquipmentPanel(Pawn pawn, Action<BodyPart, EquipmentSlotType>? clickAction = null) {
        _pawn = pawn;
        Spacing = 2;
        foreach ((BodyPart bodyPart, List<EquipmentSlotType> slots) in pawn.Equipment.Slots) {
            if (slots.Any() == false) {
                continue;
            }

            EquipmentColumn partPanel = new(bodyPart, slots, (part, type) => {
                if (clickAction != null) {
                    clickAction.Invoke(part, type);
                    return;
                }

                HandleClick(part, type);
            });
            AddChild(partPanel);
            _panels.Add(bodyPart, partPanel);
        }
    }

    private void HandleClick(BodyPart part, EquipmentSlotType slot) {
        // UnEquip
        if (Core.Sim.Gui!.MouseAttachment == null && Input.RightMouseButtonReleased && slot != EquipmentSlotType.BuiltIn) {
            Item? unEquippedItem = _pawn.Equipment.UnEquip(part, slot);
            if (unEquippedItem != null) {
                if (_pawn.Inventory.Entities.TryAdd(unEquippedItem) == false) {
                    //return item, failed to place in inventory
                    _pawn.Equipment.TryEquip(part, slot, unEquippedItem);
                }
            }

            return;
        }

        if (Core.Sim.Gui!.MouseAttachment?.Data is Item item) {
            if (item.Def == Defs.Items.RepairKit) {
                if (_pawn.Equipment.GetBySlot(part, slot) is { } equipmentItem && equipmentItem.Durability < equipmentItem.MaxDurability) {
                    equipmentItem.Repair();
                    item.StackSize--;
                    if (item.StackSize == 0) {
                        item.Destroy();
                        Core.Sim.Gui!.MouseAttachment.Detach();
                    }
                }

                return;
            }

            // Try Equip
            if (item.ItemDef.EquipmentProperties.SlotUsedToEquip == slot || (item.ItemDef.ItemType == ItemType.Potion && slot is EquipmentSlotType.PotionSlot1 or EquipmentSlotType.PotionSlot2)) {
                EntityContainer transferringContainer = item.Container!;
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
                        transferringContainer.Remove(item);
                    }

                    unEquippedItem = _pawn.Equipment.TryEquip(part, slot, potion);
                }
                else {
                    transferringContainer.Remove(item);
                    unEquippedItem = _pawn.Equipment.TryEquip(part, slot, item);
                }

                Core.Sim.Gui!.MouseAttachment.Detach();
                if (unEquippedItem != null) {
                    transferringContainer.TryAdd(unEquippedItem);
                }
            }

            return;
        }

        if (part.Equipment[slot] != null) {
            Core.Sim.Gui!.ViewEntity(part.Equipment[slot]!);
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
        private IImage _bagSlotIcon;

        public EquipmentColumn(BodyPart bodyPart, List<EquipmentSlotType> slots, Action<BodyPart, EquipmentSlotType>? clickAction = null) {
            _bodyPart = bodyPart;
            ClickAction = clickAction;
            Spacing = 2;
            _potionSlotIcon = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Icon.PotionSlot];
            _bagSlotIcon = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Icon.BagSlot];
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
                if (_bodyPart.Equipment[slot] is { IsDestroyed: false } item) {
                    if (_iconCache.ContainsKey(item.ItemDef) == false) {
                        _iconCache[item.ItemDef] = new ColoredRegion(new TextureRegion(item.Icon), Color.White);
                    }

                    image.Image = _iconCache[item.ItemDef];
                    ((ColoredRegion) image.Image).Color = GetEquipmentColor(item, _bodyPart);
                }
                else {
                    if (slot is EquipmentSlotType.PotionSlot1 or EquipmentSlotType.PotionSlot2) {
                        image.Image = _potionSlotIcon;
                    }else if (slot is EquipmentSlotType.Bag) {
                        image.Image = _bagSlotIcon;
                    }
                    else {
                        image.Image = null;
                    }
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