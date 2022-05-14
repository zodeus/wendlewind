using System.Collections.Generic;
using System.Linq;
using Grafted.Maths;
using Grafted.Sim.Entities.Items;
using Grafted.Sim.Gui.Widgets.EntityWidgets;
using Grafted.UI;
using Microsoft.Xna.Framework;
using Myra.Graphics2D.TextureAtlases;
using Myra.Graphics2D.UI;

namespace Grafted.Sim.Gui.Widgets.TownWidgets.HouseWidgets.FoodWidgets;

public class MeatRackPanel : VerticalStackPanel {
    private readonly TownStructureHouse _house;
    private readonly List<MeatRackSlotPanel> _slots = new();

    public MeatRackPanel(TownStructureHouse house) {
        _house = house;
        Spacing = 5;
        base.Visible = false;
        AddChild(new HorizontalSeparator());
        AddChild(new Label(BaseContent.Styles.Label.Medium) { Text = "Meat Rack" });
        AddChild(new Label { Text = $"Takes \\c[{UiTextColor.TextColorBlue}]16 hours \\c[{UiTextColor.TextColorDefault}]to dry,\nrequires heat from the fire" });
        HorizontalStackPanel panel = new() { Spacing = 5 };
        foreach ((int key, Item? item) in _house.MeatRack) {
            MeatRackSlotPanel meatSlot = new(_house, key);
            _slots.Add(meatSlot);
            panel.AddChild(meatSlot);
        }

        AddChild(panel);
    }



    public void Update() {
        if (Visible == false && _house.HasMeatRack) {
            Visible = true;
        }

        foreach (MeatRackSlotPanel slot in _slots) {
            slot.Update();
        }
    }

    private class MeatRackSlotPanel : VerticalStackPanel {
        private readonly TownStructureHouse _house;
        private readonly int _slot;
        private readonly ImageButton _button;
        private readonly Label _label;
        private Item? _currentItem;

        public MeatRackSlotPanel(TownStructureHouse house, int slot) {
            _house = house;
            _slot = slot;
            Spacing = 5;
            _button = new ImageButton(BaseContent.Styles.Button.Icon) { Width = 32, Height = 32 };
            _button.Click += (_, _) => {
                if (_house.MeatRack[_slot]?.ItemDef.FoodProperties?.FoodType == FoodType.DriedMeat) {
                    _house.Storage.TryAdd(_house.MeatRack[_slot]);
                    _house.MeatRack[_slot] = null;
                    _currentItem = null;
                    return;
                }

                var items = _house.Storage.Where(i => i.ItemDef.FoodProperties?.FoodType == FoodType.RawMeat)
                    .Concat(Core.Sim.World.PlayerPawn.Inventory.Where(i => i.ItemDef.FoodProperties?.FoodType == FoodType.RawMeat));
                new EntitySelector(items, entity => {
                    Item item = (Item) entity;
                    _house.AddMeatToDryingRack(item.SplitStack(1), slot);
                }).Show(Core.Sim.Gui!.Desktop, Input.MousePosition.ToPoint() + new Point(-20, -20));
            };
            _label = new Label();
            AddChild(_button);
            AddChild(_label);

        }

        public void Update() {
            if (_house.MeatRack[_slot] != null && _currentItem != _house.MeatRack[_slot]) {
                _currentItem = _house.MeatRack[_slot];
                _button.Image = new TextureRegion(_currentItem!.Icon);
            }
            else if (_currentItem == null && _button.Image != null) {
                _button.Image = null;
            }

            if (_button.Enabled && _currentItem?.ItemDef.FoodProperties?.FoodType == FoodType.RawMeat) {
                _button.Enabled = false;
            }

            if (_button.Enabled == false && _currentItem?.ItemDef.FoodProperties?.FoodType == FoodType.DriedMeat) {
                _button.Enabled = true;
            }

            if (_house.MeatTicks[_slot] > 0) {
                int hoursLeft = 16 - Mathf.FloorToInt(_house.MeatTicks[_slot] / 60f);
                _label.Text = $"\\c[{UiTextColor.TextColorBlue}]{hoursLeft}/h";
            }
            else {
                _label.Text = "";
            }
        }
    }
}