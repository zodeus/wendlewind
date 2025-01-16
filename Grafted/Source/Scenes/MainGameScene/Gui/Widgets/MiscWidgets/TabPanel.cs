namespace Grafted.Scenes.MainGameScene.Gui.Widgets.MiscWidgets;

public class TabPanel : VerticalStackPanel {
    private readonly HorizontalStackPanel _tabButtons;
    //private readonly Dictionary<string, Widget> _tabs = new();
    private ScrollViewer _activeTab = new();
    public string ButtonStyle { get; set; } = BaseContent.Styles.Button.Normal;

    public TabPanel() {
        Spacing = 10;
        _tabButtons = new HorizontalStackPanel { Spacing = 5 };
        Widgets.Add(_tabButtons);
        Widgets.Add(new HorizontalSeparator());
        Widgets.Add(_activeTab);
    }

    public void AddTab(string? label, Widget widget, IBrush? icon = null) {
        HorizontalStackPanel row = new();
        if (icon != null) {
            row.Widgets.Add(new Image { Background = icon, Width = 48, Height = 48 });
        }

        if (label != null) {
            row.Widgets.Add(new TextButton(ButtonStyle) { Text = label });
        }

        if (widget is IUpdatable updateable) {
            updateable.Update();
        }

        row.TouchDown += (_, _) => SetActiveTab(widget);
        _tabButtons.Widgets.Add(row);

        if (_activeTab.Content == null) {
            SetActiveTab(widget);
        }
    }

    private void SetActiveTab(Widget widget) {
        _activeTab.Content?.RemoveFromParent();
        _activeTab.Content = widget;
    }

    public void Update() {
        if (_activeTab.Content is IUpdatable updateable) {
            updateable.Update();
        }
    }
}