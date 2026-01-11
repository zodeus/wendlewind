using SolidBrush = Myra.Graphics2D.Brushes.SolidBrush;

namespace Grafted.Scenes.MainGameScene.Gui.Widgets.MiscWidgets;

public sealed class TabPanel : VerticalStackPanel
{
    private readonly HorizontalStackPanel _tabButtonsPanel;
    private readonly List<CursorButton> _tabButtons = [];
    private readonly List<Widget> _tabContents = [];
    private readonly List<Panel> _tabIndicators = [];

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

    public void AddTab(string? labelText, Widget widget, IBrush? icon = null, Func<bool>? hasIndicator = null)
    {
        _tabContents.Add(widget);
        
        HorizontalStackPanel row = new();
        if (icon != null)
        {
            row.Widgets.Add(new Image { Background = icon, Width = 48, Height = 48 });
        }

        CursorButton? label = null;
        Panel? indicator = null;
        
        if (labelText != null)
        {
            // Container for button + indicator
            var buttonContainer = new Panel();
            
            label = new CursorButton(ButtonStyle) { Content = new Label(LabelStyle) { Text = labelText } };
            buttonContainer.Widgets.Add(label);
            
            // Add indicator dot (initially hidden)
            indicator = new Panel
            {
                Width = 10,
                Height = 10,
                Background = new SolidBrush(new Color(60, 200, 60)),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 2, 2, 0),
                Visible = false
            };
            buttonContainer.Widgets.Add(indicator);
            _tabIndicators.Add(indicator);
            
            _tabButtons.Add(label);
            row.Widgets.Add(buttonContainer);
        }
        else
        {
            // No label, add a placeholder indicator
            _tabIndicators.Add(null!);
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

    /// <summary>
    /// Updates all tab contents (not just the active one)
    /// </summary>
    public void Update()
    {
        foreach (var content in _tabContents)
        {
            if (content is IUpdatable updateable)
            {
                updateable.Update();
            }
        }
    }
    
    /// <summary>
    /// Sets indicator visibility for a specific tab index
    /// </summary>
    public void SetTabIndicator(int tabIndex, bool visible)
    {
        if (tabIndex >= 0 && tabIndex < _tabIndicators.Count && _tabIndicators[tabIndex] != null)
        {
            _tabIndicators[tabIndex].Visible = visible;
        }
    }
}