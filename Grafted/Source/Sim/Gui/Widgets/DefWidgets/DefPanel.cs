using Grafted.Definitions;
using Myra.Graphics2D.UI;

namespace Grafted.Sim.Gui.Widgets.DefWidgets;

public class DefPanel : DefPanelBase {
    private readonly Def _resource;

    public DefPanel(Def resource, DefPanelProperties? properties = null) : base(resource, properties) {
        _resource = resource;
        MinWidth = 300;
        Spacing = 5;
        AddChild(new Label { Text = resource.Moniker });
    }
}