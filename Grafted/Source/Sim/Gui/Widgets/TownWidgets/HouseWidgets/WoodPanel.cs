using Grafted.Definitions;
using Grafted.Sim.Entities.Items;
using Grafted.Sim.Zones.Handlers;
using Myra.Graphics2D;
using Myra.Graphics2D.TextureAtlases;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.Styles;

namespace Grafted.Sim.Gui.Widgets.TownWidgets.HouseWidgets;

public class WoodPanel : VerticalStackPanel {
    private readonly TownStructureHouse _house;
    private int _availableWood = 0;
    private int _availableFireWood = 0;
    private int _availableWoodBoards = 0;
    private readonly TextButton _firewoodButton;
    private readonly TextButton _woodBoardsButton;
    private readonly Label _availableWoodLogLabel;
    private readonly Label _availableFireWoodLabel;
    private readonly Label _availableWoodBoardsLabel;

    public WoodPanel(Town town) {
        Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrame];
        Padding = new Thickness(20);
        Spacing = 15;
        _house = town.GetStructure<TownStructureHouse>()!;
        _firewoodButton = new TextButton(BaseContent.Styles.Button.Normal) {
            Text = "Chop Firewood", Width = 150, VerticalAlignment = VerticalAlignment.Center
        };
        _firewoodButton.Click += (_, _) => {
            Item wood = _house.Storage.Take(Defs.Items.WoodLog, 1)
                        ?? Core.Sim.World.PlayerPawns[0].Inventory.Entities.Take(Defs.Items.WoodLog, 1)!;
            _house.ChopFirewood(wood);
        };

        _woodBoardsButton = new TextButton(BaseContent.Styles.Button.Normal) {
            Text = "Cut Boards", Width = 150, VerticalAlignment = VerticalAlignment.Center
        };
        _woodBoardsButton.Click += (_, _) => {
            Item wood = _house.Storage.Take(Defs.Items.WoodLog, 1)
                        ?? Core.Sim.World.PlayerPawns[0].Inventory.Entities.Take(Defs.Items.WoodLog, 1)!;
            _house.CutBoard(wood);
        };
        _availableWoodLogLabel = new Label(BaseContent.Styles.Label.Medium) {
            VerticalAlignment = VerticalAlignment.Center, Padding = new Thickness(8),
            Width = 32,
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.IconFrame]
        };
        _availableFireWoodLabel = new Label(BaseContent.Styles.Label.Medium) {
            VerticalAlignment = VerticalAlignment.Center, Padding = new Thickness(8),
            Width = 32,
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.IconFrame]
        };
        _availableWoodBoardsLabel = new Label(BaseContent.Styles.Label.Medium) {
            VerticalAlignment = VerticalAlignment.Center, Padding = new Thickness(8),
            Width = 32,
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.IconFrame]
        };
        AddChild(new Label(BaseContent.Styles.Label.Large) { Text = "Wood Stuff" });
        AddChild(new HorizontalStackPanel {
            Spacing = 10,
            Widgets = {
                new Label(BaseContent.Styles.Label.Medium) {
                    Text = "Available Wood", VerticalAlignment = VerticalAlignment.Center, Width = 172
                },
                new Image {
                    Background = new TextureRegion(Defs.Items.WoodLog.Icon),
                    Width = 40, Height = 40, VerticalAlignment = VerticalAlignment.Center
                },
                _availableWoodLogLabel
            }
        });

        // Firewood
        AddChild(new HorizontalSeparator());
        AddChild(new Label {
            Text = $"Takes \\c[{UiTextColor.TextColorBlue}]2 hours \\c[{UiTextColor.TextColorDefault}]per log," +
                   $"\nproduces \\c[{UiTextColor.TextColorGreen}]100 \\c[{UiTextColor.TextColorDefault}]pieces of \\c[{UiTextColor.TextColorItem}]Firewood"
        });
        AddChild(new HorizontalStackPanel {
            Spacing = 10,
            Widgets = {
                _firewoodButton,
                new Image {
                    Background = new TextureRegion(Defs.Items.Firewood.Icon),
                    Width = 40, Height = 40, VerticalAlignment = VerticalAlignment.Center
                },
                _availableFireWoodLabel
            }
        });

        // Wood Boards
        AddChild(new HorizontalSeparator());
        AddChild(new Label {
            Text = $"Takes \\c[{UiTextColor.TextColorBlue}]4 hours \\c[{UiTextColor.TextColorDefault}]per log," +
                   $"\nproduces \\c[{UiTextColor.TextColorGreen}]8 \\c[{UiTextColor.TextColorItem}]wooden boards"
        });
        AddChild(new HorizontalStackPanel {
            Spacing = 10,
            Widgets = {
                _woodBoardsButton,
                new Image {
                    Background = new TextureRegion(Defs.Items.WoodBoard.Icon),
                    Width = 40, Height = 40, VerticalAlignment = VerticalAlignment.Center
                },
                _availableWoodBoardsLabel
            }
        });
    }

    public void Update() {
        _availableWood = _house.AmountOfItem(Defs.Items.WoodLog);
        _availableFireWood = _house.AmountOfItem(Defs.Items.Firewood);
        _availableWoodBoards = _house.AmountOfItem(Defs.Items.WoodBoard);

        _availableWoodLogLabel.Text = _availableWood.ToString();
        _availableFireWoodLabel.Text = _availableFireWood.ToString();
        _availableWoodBoardsLabel.Text = _availableWoodBoards.ToString();

        _firewoodButton.Enabled = _availableWood > 0 && Core.Sim.World.PlayerPawn.Body.Energy > .1;
        _woodBoardsButton.Enabled = _availableWood > 0 && Core.Sim.World.PlayerPawn.Body.Energy > .3;
    }
}