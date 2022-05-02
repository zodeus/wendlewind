using Grafted.Definitions;
using Grafted.Sim.Entities.Items;
using Myra.Graphics2D;
using Myra.Graphics2D.TextureAtlases;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.Styles;

namespace Grafted.Sim.Gui.TownWidgets.HouseWidgets;

public class FirewoodPanel : VerticalStackPanel {
    private readonly TownStructureHouse _house;
    private int _availableWood = 0;
    private int _availableFireWood = 0;
    private readonly TextButton _button;
    private readonly Label _availableWoodLogLabel;
    private readonly Label _availableFireWoodLabel;

    public FirewoodPanel(Town town) {
        Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrame];
        Padding = new Thickness(20);
        Spacing = 15;
        _house = town.GetStructure<TownStructureHouse>()!;
        _button = new TextButton(BaseContent.Styles.Button.Normal) {
            Text = "Chop Firewood", VerticalAlignment = VerticalAlignment.Center
        };
        _availableWoodLogLabel = new Label {
            VerticalAlignment = VerticalAlignment.Center, Padding = new Thickness(8),
            Width = 32,
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.IconFrame]
        };
        _availableFireWoodLabel = new Label {
            VerticalAlignment = VerticalAlignment.Center, Padding = new Thickness(8),
            Width = 32,
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.IconFrame]
        };
        _button.Click += (_, _) => {
            Item wood = _house.Storage.Take(Defs.Items.WoodLog, 1)
                        ?? Core.Sim.World.PlayerPawns[0].Inventory.Items.Take(Defs.Items.WoodLog, 1)!;
            _house.ChopFirewood(wood);
        };
        AddChild(new Label(BaseContent.Styles.Label.Medium) { Text = "Available Wood" });
        AddChild(new HorizontalStackPanel {
            Spacing = 10,
            Widgets = {
                new Image {
                    Background = new TextureRegion(Defs.Items.WoodLog.Icon),
                    Width = 40, Height = 40, VerticalAlignment = VerticalAlignment.Center
                },
                _availableWoodLogLabel
            }
        });
        AddChild(new Label { Text = "Takes 60 minutes per log,\nproduces 20 pieces of firewood" });
        AddChild(new HorizontalStackPanel {
            Spacing = 10,
            Widgets = {
                _button,
                new Image {
                    Background = new TextureRegion(Defs.Items.Firewood.Icon),
                    Width = 40, Height = 40, VerticalAlignment = VerticalAlignment.Center
                },
                _availableFireWoodLabel
            }
        });
    }

    public void Update() {
        _availableWood = _house.AmountOfItem(Defs.Items.WoodLog);
        _availableFireWood = _house.AmountOfItem(Defs.Items.Firewood);
        _button.Enabled = _availableWood > 0;
        _availableWoodLogLabel.Text = _availableWood.ToString();
        _availableFireWoodLabel.Text = _availableFireWood.ToString();
    }
}