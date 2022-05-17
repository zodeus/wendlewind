using Grafted.Definitions;
using Grafted.Sim.Entities;
using Grafted.Sim.Entities.Items;
using Grafted.Sim.Zones.Handlers;
using Myra.Graphics2D;
using Myra.Graphics2D.TextureAtlases;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.Styles;

namespace Grafted.Sim.Gui.Widgets.TownWidgets.HouseWidgets;

public class WellPanel : VerticalStackPanel {
    private readonly TownStructureHouse _house;
    private int _availableWater = 0;
    private readonly TextButton _waterButton;
    private readonly Label _availableWaterLabel;

    public WellPanel(Town town) {
        Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrame];
        Padding = new Thickness(20);
        Spacing = 15;
        _house = town.GetStructure<TownStructureHouse>()!;
        _waterButton = new TextButton(BaseContent.Styles.Button.Normal) {
            Text = "Fetch Water", VerticalAlignment = VerticalAlignment.Center
        };
        _waterButton.Click += (_, _) => {
            Core.Sim.World.PlayerPawns[0].Body.ApplyEnergyLoss(0.05f);
            Core.Sim.World.ProgressTime(SimTime.MinutesToSeconds(15));
            _house.Storage.TryAdd(EntityGenerator.CreateEntity<Item>(Defs.Items.WaterBucket, 1));
        };

        _availableWaterLabel = new Label(BaseContent.Styles.Label.Medium) {
            VerticalAlignment = VerticalAlignment.Center, Padding = new Thickness(8),
            Width = 32,
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.IconFrame]
        };
        AddChild(new Label(BaseContent.Styles.Label.Large) { Text = "The Watery Well" });
        AddChild(new HorizontalStackPanel {
            Spacing = 10,
            Widgets = {
                new Label(BaseContent.Styles.Label.Medium) {
                    Text = "Available Water", VerticalAlignment = VerticalAlignment.Center
                },
                new Image {
                    Background = new TextureRegion(Defs.Items.WaterBucket.Icon),
                    Width = 40, Height = 40, VerticalAlignment = VerticalAlignment.Center
                },
                _availableWaterLabel
            }
        });
        AddChild(new HorizontalStackPanel {
            Spacing = 10, Widgets = {
                _waterButton,
                new Label(BaseContent.Styles.Label.Medium) {
                    VerticalAlignment = VerticalAlignment.Center,
                    Text = $"Takes \\c[{UiTextColor.TextColorTime}]15 minutes"
                }
            }
        });

    }

    public void Update() {
        _availableWaterLabel.Text = _house.AmountOfItem(Defs.Items.WaterBucket).ToString();

        //_waterButton.Enabled = _availableWood > 0 && Core.Sim.World.PlayerPawn.Body.Energy > .1;
    }
}