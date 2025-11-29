using Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets;
using Grafted.Sim.Entities.Items.Trinkets;

namespace Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.TrinketWidgets;

[UsedImplicitly]
public sealed class SteroidInjectorPanel : EntityPanelBase
{
    public SteroidInjectorHandler Injector { get; set; }
    public Label InjectorLabel { get; set; }

    private readonly Grid _grid;
    private Dictionary<BodyPart, Button> _injectorButtons = [];

    public SteroidInjectorPanel(BaseGui gui, Item item, EntityPanelProperties? properties = null) : base(gui, item, properties)
    {
        Padding = new Thickness(0, 0, 0, 5);

        Injector = (item.TrinketHandler as SteroidInjectorHandler)!;
        InjectorLabel = new Label(BaseContent.Styles.Label.Medium)
        {
            Margin = new Thickness(0, 10, 0, 20),
        };

        Widgets.Add(new HorizontalStackPanel
        {
            Margin = new Thickness(15, 0, 0, 0),
            Spacing = 20,
            Widgets =
            {
                new Image { Width = 64, Height = 64, Background = new TextureRegion(item.Icon) },
                InjectorLabel
            }
        });
        _grid = new Grid
        {
            Margin = new Thickness(20, 0, 30, 0),
            RowSpacing = 5,
            ColumnSpacing = 40,
            DefaultColumnProportion = Proportion.Auto
        };
        Widgets.Add(new ScrollViewer
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Height = 700,
            Content = _grid
        });

        var gridColum = 2;
        AddGridCell(new Label(BaseContent.Styles.Label.Medium) { Text = "Cost", HorizontalAlignment = HorizontalAlignment.Right }, 0, gridColum++);
        AddGridCell(new Label(BaseContent.Styles.Label.Medium) { Text = "HP" }, 0, gridColum);

        var gridRow = 1;
        foreach (var bodyPart in Core.Context.PlayerPawn.Body.AllExternalParts)
        {
            GeneratePartRow(bodyPart, gridRow);

            gridRow++;
        }
    }

    private void GeneratePartRow(BodyPart bodyPart, int gridRow)
    {
        var gridColum = 0;
        var hpLabel = new Label(BaseContent.Styles.Label.Normal) { VerticalAlignment = VerticalAlignment.Center };
        var injectButton = new Button(BaseContent.Styles.Button.Plus64)
        {
            Width = 48, Height = 48, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center
        };
        var partCell = new HorizontalStackPanel
        {
            Spacing = 10,
            Widgets =
            {
                new Image { Width = 64, Height = 64 },
                new Label(BaseContent.Styles.Label.Normal) { VerticalAlignment = VerticalAlignment.Center, Text = $"{bodyPart.Label}" }
            }
        };
        var costLabel = new Label { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Right };
        injectButton.Click += (_, _) =>
        {
            Injector.InjectPart(bodyPart);
            Refresh();
        };
        _injectorButtons.Add(bodyPart, injectButton);

        AddGridCell(injectButton, gridRow, gridColum++);
        AddGridCell(partCell, gridRow, gridColum++);
        AddGridCell(costLabel, gridRow, gridColum++);
        AddGridCell(hpLabel, gridRow, gridColum);
        Refresh();
        return;

        void Refresh()
        {
            InjectorLabel.Text = $"Fuel Remaining /c[{TC.Golden}]{Injector.TotalDamage:N0}";
            costLabel.Text = $"{Injector.CalculateTotalCost(bodyPart):N0}";
            hpLabel.Text = $"{bodyPart.HitPoints:N0}/{bodyPart.MaxHitPoints:N0}";
            ((Image)partCell.Widgets[0]).Background = new ColoredRegion(new TextureRegion(bodyPart.Icon), BodyPartColor.Get(bodyPart));
            ((Label)partCell.Widgets[1]).TextColor = BodyPartColor.Get(bodyPart);

            foreach (var (part, button) in _injectorButtons)
            {
                button.Enabled = Injector.HasFuelFor(part);
            }
        }
    }

    private void AddGridCell(Widget widget, int row, int column)
    {
        Grid.SetRow(widget, row);
        Grid.SetColumn(widget, column);
        _grid.Widgets.Add(widget);
    }

    public override void Update()
    {
    }
}