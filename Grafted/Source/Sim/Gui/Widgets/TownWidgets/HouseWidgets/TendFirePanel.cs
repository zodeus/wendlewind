using Grafted.Definitions;
using Grafted.Sim.Entities.Items;
using Myra.Graphics2D;
using Myra.Graphics2D.TextureAtlases;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.Styles;

namespace Grafted.Sim.Gui.Widgets.TownWidgets.HouseWidgets;

public class TendFirePanel : HorizontalStackPanel {
    private readonly TownStructureHouse _house;
    private readonly TextButton _add1Wood;
    private readonly TextButton _add5Wood;
    private readonly Label _woodInFireLabel;
    private readonly Label _fireStatusLabel;
    private readonly TextButton _startFire;
    private readonly TextButton _add10Wood;

    public TendFirePanel(TownStructureHouse house) {
        _house = house;
        Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrame];
        Padding = new Thickness(20);
        Spacing = 15;

        _woodInFireLabel = new Label {
            VerticalAlignment = VerticalAlignment.Center, Padding = new Thickness(8),
            Width = 20,
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.IconFrame]
        };
        _add1Wood = new TextButton(BaseContent.Styles.Button.Normal) {
            Text = " 1x Firewood", VerticalAlignment = VerticalAlignment.Center
        };
        _add1Wood.Click += (_, _) => {
            Item wood = _house.Storage.Take(Defs.Items.Firewood, 1)
                        ?? Core.Sim.World.PlayerPawns[0].Inventory.Entities.Take(Defs.Items.Firewood, 1)!;
            _house.AddWoodToFire(wood);
        };
        _add5Wood = new TextButton(BaseContent.Styles.Button.Normal) {
            Text = " 5x Firewood", VerticalAlignment = VerticalAlignment.Center
        };
        _add5Wood.Click += (_, _) => {
            Item wood = _house.Storage.Take(Defs.Items.Firewood, 5)
                        ?? Core.Sim.World.PlayerPawns[0].Inventory.Entities.Take(Defs.Items.Firewood, 5)!;
            _house.AddWoodToFire(wood);
        };
        _add10Wood = new TextButton(BaseContent.Styles.Button.Normal) {
            Text = "10x Firewood", VerticalAlignment = VerticalAlignment.Center
        };
        _add10Wood.Click += (_, _) => {
            Item wood = _house.Storage.Take(Defs.Items.Firewood, 10)
                        ?? Core.Sim.World.PlayerPawns[0].Inventory.Entities.Take(Defs.Items.Firewood, 10)!;
            _house.AddWoodToFire(wood);
        };
        _startFire = new TextButton(BaseContent.Styles.Button.Normal) {
            Text = "Start Fire"
        };
        _startFire.Click += (_, _) => {
            _house.StartFire();
        };
        _fireStatusLabel = new Label(BaseContent.Styles.Label.Medium);
        VerticalStackPanel leftPanel = new() { Spacing = 15 };
        leftPanel.AddChild(_fireStatusLabel);
        leftPanel.AddChild(new Label { Text = $"Logs burn at \\c[{UiTextColor.TextColorBlue}]1/hour" });
        leftPanel.AddChild(new Label { Text = "Wood in fireplace" });
        leftPanel.AddChild(new HorizontalStackPanel {
            Spacing = 10,
            Widgets = {
                new Image {
                    Background = new TextureRegion(Defs.Items.Firewood.Icon),
                    Width = 40, Height = 40, VerticalAlignment = VerticalAlignment.Center
                },
                _woodInFireLabel
            }
        });
        leftPanel.AddChild(_startFire);

        VerticalStackPanel rightPanel = new() {
            Spacing = 15,
            VerticalAlignment = VerticalAlignment.Center,
            Widgets = {
                new Label(BaseContent.Styles.Label.Medium) { Text = "Add Wood" },
                _add1Wood,
                _add5Wood,
                _add10Wood
            }
        };
        AddChild(leftPanel);
        AddChild(new VerticalSeparator());
        AddChild(rightPanel);
    }

    public void Update() {
        int availableFireWood = 0;
        foreach (Item item in _house.Storage) {
            if (item.ItemDef == Defs.Items.Firewood) {
                availableFireWood += item.StackSize;
            }
        }

        foreach (Item item in Core.Sim.World.PlayerPawns[0].Inventory) {
            if (item.ItemDef == Defs.Items.Firewood) {
                availableFireWood += item.StackSize;
            }
        }

        _add1Wood.Enabled = availableFireWood > 0 && _house.Firewood < 30;
        _add5Wood.Enabled = availableFireWood >= 5 && _house.Firewood <= 25;
        _add10Wood.Enabled = availableFireWood >= 10 && _house.Firewood <= 20;

        _woodInFireLabel.Text = _house.Firewood.ToString();

        if (_house.IsFireBurning) {
            _fireStatusLabel.Text = $"\\c[{UiTextColor.TextColorGreen}]Fire is burning\n\\c[{UiTextColor.TextColorBlue}]~{_house.Firewood}/hours";
        }
        else {
            _fireStatusLabel.Text = $"\\c[{UiTextColor.TextColorRed}]Fire is out";
        }

        _startFire.Enabled = !_house.IsFireBurning && _house.Firewood > 0;
    }
}