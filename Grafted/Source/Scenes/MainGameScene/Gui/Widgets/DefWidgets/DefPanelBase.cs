namespace Grafted.Scenes.MainGameScene.Gui.Widgets.DefWidgets;

public class DefPanelProperties {
    public bool ShowTitle { get; set; } = true;
    public bool ShowCloseButton { get; set; }
    public IBrush? Background { get; set; } = null;
    public Action? CloseButtonAction;
}

public abstract class DefPanelBase : VerticalStackPanel {
    public readonly HorizontalStackPanel Header;

    protected DefPanelBase(Def resource, DefPanelProperties? properties) {

        Background = properties?.Background;
        Padding = new Thickness(15);
        Header = new HorizontalStackPanel { Spacing = 20 };
        Widgets.Add(Header);
        if (properties?.ShowTitle ?? false) {
            Header.Margin = new Thickness(0, 0, 0, 10);
            Header.Widgets.Add(new Label("large") { Text = resource.Label, VerticalAlignment = VerticalAlignment.Center });
        }

        if (properties?.ShowCloseButton ?? false) {
            Header.Margin = new Thickness(0, 0, 0, 10);
            var closeButton = new CursorButton(BaseContent.Styles.Button.Small) {
                Content = new Image { Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Icon.Close] },
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center
            };
            StackPanel.SetProportionType(closeButton, ProportionType.Fill);
            closeButton.Click += (_, _) => {
                properties.CloseButtonAction?.Invoke();
            };
            Header.Widgets.Add(closeButton);
        }
    }
}