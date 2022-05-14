using System;
using Grafted.Definitions;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.Styles;

namespace Grafted.Sim.Gui.Widgets.DefWidgets;

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
        Header.Proportions.Add(Proportion.Fill);
        AddChild(Header);
        if (properties?.ShowTitle ?? false) {
            Header.Margin = new Thickness(0, 0, 0, 10);
            Header.AddChild(new Label("large") { Text = resource.Label, VerticalAlignment = VerticalAlignment.Center });
        }

        if (properties?.ShowCloseButton ?? false) {
            Header.Margin = new Thickness(0, 0, 0, 10);
            ImageButton closeButton = new(BaseContent.Styles.Button.Small) {
                Image = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Icon.Close],
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center
            };
            closeButton.Click += (_, _) => {
                properties.CloseButtonAction?.Invoke();
            };
            Header.AddChild(closeButton);
        }
    }
}