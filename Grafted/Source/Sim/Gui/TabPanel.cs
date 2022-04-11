using Myra.Graphics2D;
using Myra.Graphics2D.UI;

namespace Grafted.Sim.Gui;

public class TabPanel : VerticalStackPanel {
    private readonly HorizontalStackPanel _tabButtons;
    //private readonly Dictionary<string, Widget> _tabs = new();
    private ScrollViewer _activeTab = new();
    public string ButtonStyle { get; set; } = BaseContent.Styles.Button.Normal;

    public TabPanel() {
        Spacing = 10;
        _tabButtons = new HorizontalStackPanel { Spacing = 5 };
        AddChild(_tabButtons);
        AddChild(new HorizontalSeparator());
        AddChild(_activeTab);
    }

    public void AddTab(string? label, IBrush? icon, Widget widget) {
        HorizontalStackPanel row = new();
        if (icon != null) {
            row.AddChild(new Image { Background = icon, Width = 48, Height = 48 });
        }

        if (label != null) {
            row.AddChild(new TextButton(ButtonStyle) { Text = label });
        }


        row.TouchDown += (_, _) => SetActiveTab(widget);
        _tabButtons.AddChild(row);

        if (_activeTab.Content == null) {
            SetActiveTab(widget);
        }
    }

    private void SetActiveTab(Widget widget) {
        _activeTab.Content?.RemoveFromParent();
        _activeTab.Content = widget;
    }
}