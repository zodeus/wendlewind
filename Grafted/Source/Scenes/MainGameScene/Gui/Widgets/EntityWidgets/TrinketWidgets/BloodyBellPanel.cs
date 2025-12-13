using Myra.Graphics2D.Brushes;

namespace Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.TrinketWidgets;

[UsedImplicitly]
public sealed class BloodyBellPanel : EntityPanelBase
{
    private readonly BloodyBellHandler? _handler;
    private readonly Label _drainPercentLabel;
    private readonly Label _totalRingsLabel;
    private readonly HorizontalProgressBar _drainBar;
    
    public BloodyBellPanel(BaseGui gui, Item item, EntityPanelProperties? properties = null) : base(gui, item, properties)
    {
        _handler = item.TrinketHandler as BloodyBellHandler;
        Padding = new Thickness(24);
        Width = 380;
        Spacing = 0;
        
        // Header with icon and description
        var header = new HorizontalStackPanel
        {
            Spacing = 16,
            Margin = new Thickness(0, 0, 0, 16),
            Widgets =
            {
                new Panel
                {
                    Width = 80,
                    Height = 80,
                    Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.RoundElite64],
                    Widgets =
                    {
                        new Image
                        {
                            Background = new TextureRegion(item.Icon),
                            Width = 56,
                            Height = 56,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center
                        }
                    }
                },
                new VerticalStackPanel
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    Spacing = 4,
                    Widgets =
                    {
                        new Label(BaseContent.Styles.Label.Small)
                        {
                            Text = item.Def.Description,
                            TextColor = new Color(150, 150, 150),
                            Wrap = true,
                            MaxWidth = 240
                        }
                    }
                }
            }
        };
        Widgets.Add(header);
        
        // Blood drain section
        var drainSection = new Panel
        {
            Background = new SolidBrush(new Color(45, 20, 20)),
            Padding = new Thickness(16, 12),
            Margin = new Thickness(0, 0, 0, 16)
        };
        
        var drainContent = new VerticalStackPanel { Spacing = 8 };
        
        var drainHeaderRow = new HorizontalStackPanel
        {
            Widgets =
            {
                new Label(BaseContent.Styles.Label.Normal)
                {
                    Text = "Blood Drain",
                    TextColor = new Color(180, 80, 80)
                }
            }
        };
        drainContent.Widgets.Add(drainHeaderRow);
        
        _drainPercentLabel = new Label(BaseContent.Styles.Label.Large)
        {
            TextColor = new Color(220, 60, 60)
        };
        drainContent.Widgets.Add(_drainPercentLabel);
        
        _drainBar = new HorizontalProgressBar(BaseContent.Styles.Bar.Health)
        {
            Height = 10,
            Filler = new ColoredRegion(Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Bar.Neutral], new Color(180, 40, 40))
        };
        drainContent.Widgets.Add(_drainBar);
        
        drainSection.Widgets.Add(drainContent);
        Widgets.Add(drainSection);
        
        // Stats section
        var statsSection = new Panel
        {
            Background = new SolidBrush(new Color(30, 30, 35)),
            Padding = new Thickness(16, 14),
            Margin = new Thickness(0, 0, 0, 16)
        };
        
        var statsGrid = new Grid
        {
            ColumnSpacing = 12,
            RowSpacing = 10,
            DefaultColumnProportion = Proportion.Auto
        };
        statsGrid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
        statsGrid.ColumnsProportions.Add(new Proportion(ProportionType.Fill));
        
        AddStatRow(statsGrid, 0, "Total Rings", out _totalRingsLabel, new Color(200, 80, 80));
        
        statsSection.Widgets.Add(statsGrid);
        Widgets.Add(statsSection);
        
        // Usage hint
        var hintLabel = new Label(BaseContent.Styles.Label.Small)
        {
            Text = "Activate before attacking to drain enemy blood and restore your own.",
            TextColor = new Color(100, 100, 100),
            Wrap = true,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        Widgets.Add(hintLabel);
        
        RefreshDisplay();
    }
    
    private static void AddStatRow(Grid grid, int row, string labelText, out Label valueLabel, Color valueColor)
    {
        var label = new Label(BaseContent.Styles.Label.Normal)
        {
            Text = labelText,
            TextColor = new Color(100, 100, 100)
        };
        Grid.SetColumn(label, 0);
        Grid.SetRow(label, row);
        grid.Widgets.Add(label);
        
        valueLabel = new Label(BaseContent.Styles.Label.Normal)
        {
            TextColor = valueColor,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        Grid.SetColumn(valueLabel, 1);
        Grid.SetRow(valueLabel, row);
        grid.Widgets.Add(valueLabel);
    }
    
    private void RefreshDisplay()
    {
        if (_handler == null)
        {
            _drainPercentLabel.Text = "N/A";
            return;
        }
        
        // Drain percentage display
        var drainPercent = _handler.CurrentBloodDrainPercent * 100;
        _drainPercentLabel.Text = $"{drainPercent:F1}%";
        
        // Progress bar showing drain percent (max is 25%)
        _drainBar.Value = drainPercent / 25f * 100f;
        
        // Total rings
        _totalRingsLabel.Text = _handler.TotalRings.ToString();
    }
    
    public override void Update()
    {
        RefreshDisplay();
    }
}

