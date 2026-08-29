using Panel = Myra.Graphics2D.UI.Panel;

namespace Wendlewind.Scenes.MainGameScene.Gui.Widgets.DefWidgets;

public class ItemDefPanel : DefPanelBase {
    private readonly ItemDef _resource;

    public ItemDefPanel(ItemDef resource, DefPanelProperties? properties = null) : base(resource, properties) {
        _resource = resource;
        MinWidth = 300;
        Spacing = 5;
        var grid = new Grid {
            ColumnsProportions = {
                Proportion.Fill,
                Proportion.Auto
            }
        };
        Widgets.Add(grid);
        grid.Widgets.Add(GenerateDetails(resource));
        var iconPanel = new Panel {
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.IconFrame],
            Padding = new Thickness(3),
            Widgets = { new Image { Background = new TextureRegion(resource.Icon), Width = 64, Height = 64 } },
            VerticalAlignment = VerticalAlignment.Top
        };
        Grid.SetRow(iconPanel, 0);
        Grid.SetColumn(iconPanel, 1);
        grid.Widgets.Add(iconPanel);
    }

    private Widget GenerateDetails(ItemDef item) {
        Grid grid = new() {
            RowSpacing = 5, ColumnSpacing = 5,
            DefaultRowProportion = Proportion.Auto,
            ColumnsProportions = {
                Proportion.Auto, Proportion.Fill
            }
        };
        int gridRow = 0;
        var descLabel = new Label { Text = item.Description };
        Grid.SetRow(descLabel, gridRow);
        Grid.SetColumn(descLabel, 0);
        Grid.SetColumnSpan(descLabel, 2);
        grid.Widgets.Add(descLabel);
        //gridRow++;
        return grid;
    }
}