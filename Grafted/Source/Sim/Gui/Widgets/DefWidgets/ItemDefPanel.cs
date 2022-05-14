using Grafted.Sim.Entities.Items;
using Myra.Graphics2D;
using Myra.Graphics2D.TextureAtlases;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.Styles;
using Panel = Myra.Graphics2D.UI.Panel;

namespace Grafted.Sim.Gui.Widgets.DefWidgets;

public class ItemDefPanel : DefPanelBase {
    private readonly ItemDef _resource;

    public ItemDefPanel(ItemDef resource, DefPanelProperties? properties = null) : base(resource, properties) {
        _resource = resource;
        MinWidth = 300;
        Spacing = 5;
        var grid = new Grid {
            //ShowGridLines = true,
            ColumnsProportions = {
                Proportion.Fill,
                Proportion.Auto
            }
        };
        AddChild(grid);
        grid.AddChild(GenerateDetails(resource));
        grid.AddChild(new Panel {
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.IconFrame],
            Padding = new Thickness(3),
            Widgets = { new Image { Background = new TextureRegion(resource.Icon), Width = 64, Height = 64 } },
            VerticalAlignment = VerticalAlignment.Top,
            GridRow = 0, GridColumn = 1
        });
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
        grid.AddChild(new Label { Text = item.Description, GridRow = gridRow, GridColumn = 0, GridColumnSpan = 2 });
        //gridRow++;
        return grid;
    }
}