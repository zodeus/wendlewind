namespace Grafted.Scenes.MainGameScene.Gui.Widgets.MiscWidgets;

public sealed class TabPanel : VerticalStackPanel
{
    private readonly HorizontalStackPanel _tabButtonsPanel;
    private readonly List<CursorButton> _tabButtons = [];

    //private readonly Dictionary<string, Widget> _tabs = new();
    private ScrollViewer _activeTab = new();
    public string ButtonStyle { get; set; } = BaseContent.Styles.Button.Large;
    private static string LabelStyle => BaseContent.Styles.Label.Normal;

    public TabPanel(bool tabsOnTop = true)
    {
        Spacing = 10;
        _tabButtonsPanel = new HorizontalStackPanel { Spacing = 5 };
        if (tabsOnTop)
        {
            Widgets.Add(_tabButtonsPanel);
        }

        Widgets.Add(_activeTab);

        if (!tabsOnTop)
        {
            Widgets.Add(_tabButtonsPanel);
        }
    }

    public void AddTab(string? labelText, Widget widget, IBrush? icon = null)
    {
        HorizontalStackPanel row = new();
        if (icon != null)
        {
            row.Widgets.Add(new Image { Background = icon, Width = 48, Height = 48 });
        }

        CursorButton? label = null;
        if (labelText != null)
        {
            label = new CursorButton(ButtonStyle) { Content = new Label(LabelStyle) { Text = labelText } };
  
            _tabButtons.Add(label);
            row.Widgets.Add(label);
        }

        if (widget is IUpdatable updateable)
        {
            updateable.Update();
        }

        row.TouchDown += (_, _) =>
        {
            SetActiveTab(widget);
            if (label != null)
            {
                ((Label)label.Content).TextColor = Color.DarkGoldenrod;
            }
        };
        _tabButtonsPanel.Widgets.Add(row);

        if (_activeTab.Content == null)
        {
            SetActiveTab(widget);
            ((Label)label!.Content).TextColor = Color.DarkGoldenrod;
        }
    }

    private void SetActiveTab(Widget widget)
    {
        foreach (var button in _tabButtons)
        {
            ((Label)button.Content).SetStyle(LabelStyle);
        }

        _activeTab.Content?.RemoveFromParent();
        _activeTab.Content = widget;
    }

    public void Update()
    {
        if (_activeTab.Content is IUpdatable updateable)
        {
            updateable.Update();
        }
    }
}