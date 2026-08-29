namespace Wendlewind.Scenes.MainGameScene.Gui.Widgets.DefWidgets;

public class DefPanel : DefPanelBase {
    private readonly Def _resource;

    public DefPanel(Def resource, DefPanelProperties? properties = null) : base(resource, properties) {
        _resource = resource;
        MinWidth = 300;
        Spacing = 5;
        Widgets.Add(new Label { Text = resource.Moniker });
    }
}