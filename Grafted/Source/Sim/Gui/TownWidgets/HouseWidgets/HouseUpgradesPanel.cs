using Grafted.Definitions;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.Styles;

namespace Grafted.Sim.Gui.TownWidgets.HouseWidgets;

public class HouseUpgradesPanel : VerticalStackPanel {
    private readonly MeatRackUpgradePanel _meatRackUpgrade;

    public HouseUpgradesPanel(TownStructureHouse house) {
        Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrame];
        Padding = new Thickness(20);
        Spacing = 10;
        AddChild(new Label(BaseContent.Styles.Label.Large) { Text = "Upgrades & Repairs" });

        // Meat Rack
        _meatRackUpgrade = new MeatRackUpgradePanel(house);
        if (house.HasMeatRack == false) {
            AddChild(new HorizontalSeparator());
            AddChild(_meatRackUpgrade);
        }

        // Mead Barrel
        AddChild(new HorizontalSeparator());
        AddChild(new MeadBarrelPanel(house));
        // 4
        AddChild(new HorizontalSeparator());
        AddChild(new Label() { Text = $"\\c[{UiTextColor.TextColorGolden}]Fortify the house" });
        AddChild(new Label() { Text = $" " });
        AddChild(new Label() { Text = $"Requires \\c[{UiTextColor.TextColorGreen}]0/80 \\c[{UiTextColor.TextColorItem}]wood boards" });
        AddChild(new Label() { Text = $"Takes \\c[{UiTextColor.TextColorBlue}]6 hours \\c[{UiTextColor.TextColorDefault}]to install some boards" });
        AddChild(new TextButton(BaseContent.Styles.Button.Small) { Text = "Install some boards", Enabled = false });
    }

    public void Update() {
        _meatRackUpgrade.Update();
    }

    private class MeadBarrelPanel : VerticalStackPanel {
        public MeadBarrelPanel(TownStructureHouse house) {
            Spacing = 5;
            AddChild(new Label() { Text = $"\\c[{UiTextColor.TextColorGolden}]Mead Barrel" });
            AddChild(new Label() { Text = $"Allows for the brewing of fine meads" });
            AddChild(new Label() { Text = $"Requires \\c[{UiTextColor.TextColorGreen}]0/20 \\c[{UiTextColor.TextColorItem}]wooden boards" });
            AddChild(new Label() { Text = $"Takes \\c[{UiTextColor.TextColorBlue}]4 hours \\c[{UiTextColor.TextColorDefault}] to assemble" });
            AddChild(new TextButton(BaseContent.Styles.Button.Small) { Text = "Assemble the barrel", Enabled = false });
        }
    }

    private class MeatRackUpgradePanel : VerticalStackPanel {
        private readonly TownStructureHouse _house;
        private readonly Label _label;
        private readonly TextButton _button;

        public MeatRackUpgradePanel(TownStructureHouse house) {
            _house = house;
            Spacing = 5;
            _label = new Label() { Text = $"Requires \\c[{UiTextColor.TextColorGreen}]0/2 \\c[{UiTextColor.TextColorItem}]wood logs" };
            _button = new TextButton(BaseContent.Styles.Button.Small) { Text = "Build", Enabled = false };
            _button.Click += (_, _) => {
                house.HasMeatRack = true;
                Core.Sim.World.PlayerPawns[0].Body.ApplyEnergyLoss(0.35f);
                Core.Sim.World.ProgressTime(SimTime.HoursToSeconds(5));
                _house.TakeItem(Defs.Items.WoodLog, 2)!.Destroy();
                Core.Sim.Messages.Push(new Message($"Built a \\c[{UiTextColor.TextColorItem}]Meat Rack"));
                RemoveFromParent();
            };
            AddChild(new Label() { Text = $"\\c[{UiTextColor.TextColorGolden}]Meat Rack" });
            AddChild(new Label() { Text = $"Dried meat, an adventurers ration" });
            AddChild(_label);
            AddChild(new Label() { Text = $"Takes \\c[{UiTextColor.TextColorBlue}]5 hours \\c[{UiTextColor.TextColorDefault}]to build" });
            AddChild(_button);
        }

        public void Update() {
            int availableLogs = _house.AmountOfItem(Defs.Items.WoodLog);
            string color = availableLogs >= 2 ? UiTextColor.TextColorGreen : UiTextColor.TextColorRed;
            _label.Text = $"Requires \\c[{color}]{availableLogs}/2 \\c[{UiTextColor.TextColorItem}]wood logs";
            _button.Enabled = availableLogs >= 2;
        }
    }
}