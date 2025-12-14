using Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets;
using Myra.Graphics2D.Brushes;

namespace Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.TrinketWidgets;

[UsedImplicitly]
public sealed class SteroidInjectorPanel : EntityPanelBase
{
    public SteroidInjectorHandler Injector { get; set; }
    
    private readonly Label _fuelValueLabel;
    private readonly VerticalStackPanel _partsList;
    private readonly Dictionary<BodyPart, BodyPartRow> _partRows = [];
    private double _maxFuelSeen;
    
    // Color palette
    private static readonly Color FuelColor = new(232, 170, 0);
    private static readonly Color FuelDimColor = new(120, 88, 0);
    private static readonly Color SectionBgColor = Color.Transparent;
    private static readonly Color RowHoverColor = new(20, 20, 30);
    private static readonly Color VitalIndicatorColor = new(200, 60, 60);
    private static readonly Color BoostColor = new(120, 220, 120);
    private static readonly Color DisabledTextColor = new(80, 80, 85);
    private static readonly Color CostAffordableColor = new(180, 200, 180);
    private static readonly Color CostUnaffordableColor = new(180, 80, 80);

    public SteroidInjectorPanel(BaseGui gui, Item item, EntityPanelProperties? properties = null) : base(gui, item, properties)
    {
        Padding = new Thickness(0);
        Spacing = 0;
        MinWidth = 520;
        
        Injector = (item.TrinketHandler as SteroidInjectorHandler)!;
        _maxFuelSeen = Math.Max(Injector.TotalDamage, 1000);

        // === UNIFIED HEADER WITH FUEL GAUGE ===
        var header = new HorizontalStackPanel
        {
            Padding = new Thickness(20, 16),
            Spacing = 20,
            Margin = new Thickness(0, 0, 0, 2)
        };
        
        // Item icon in decorative frame
        header.Widgets.Add(new Panel
        {
            Width = 80,
            Height = 80,
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.RoundElite64],
            Widgets =
            {
                new Image
                {
                    Background = new TextureRegion(item.Icon),
                    Width = 52,
                    Height = 52,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            }
        });
        
        // Fuel info section (right side of icon)
        var fuelInfo = new VerticalStackPanel
        {
            VerticalAlignment = VerticalAlignment.Center,
            Spacing = 6
        };
        
        // "Fuel Remaining" label
        fuelInfo.Widgets.Add(new Label(BaseContent.Styles.Label.Normal)
        {
            Text = "Fuel Remaining",
            TextColor = FuelDimColor
        });
        
        // Fuel value with units
        var fuelValueRow = new HorizontalStackPanel { Spacing = 10 };
        _fuelValueLabel = new Label(BaseContent.Styles.Label.Huge)
        {
            TextColor = FuelColor,
            VerticalAlignment = VerticalAlignment.Bottom
        };
        fuelValueRow.Widgets.Add(_fuelValueLabel);
        fuelValueRow.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
        {
            Text = "damage stored",
            TextColor = new Color(100, 100, 105),
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(0, 0, 0, 8)
        });
        fuelInfo.Widgets.Add(fuelValueRow);
        

        header.Widgets.Add(fuelInfo);
        Widgets.Add(header);

        // === COLUMN HEADERS ===
        var columnHeaders = CreateColumnHeaders();
        Widgets.Add(columnHeaders);

        // === BODY PARTS LIST ===
        _partsList = new VerticalStackPanel { Spacing = 1 };
        
        var scrollViewer = new ScrollViewer
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Height = 580,
            Content = _partsList
        };
        Widgets.Add(scrollViewer);
        
        // Generate rows for each body part
        foreach (var bodyPart in Core.Context.PlayerPawn.Body.AllExternalParts)
        {
            var row = new BodyPartRow(this, bodyPart);
            _partRows[bodyPart] = row;
            _partsList.Widgets.Add(row);
        }
        
        // === FOOTER HINT ===
        var footerHint = new Label(BaseContent.Styles.Label.Small)
        {
            Text = "Inject fuel into body parts to permanently increase their max HP.",
            TextColor = new Color(90, 95, 100),
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 12, 0, 8),
            Wrap = true
        };
        Widgets.Add(footerHint);
        
        RefreshAll();
    }

    private static Grid CreateColumnHeaders()
    {
        var headerGrid = new Grid
        {
            Background = new SolidBrush(new Color(32, 36, 42)),
            Padding = new Thickness(16, 10),
            ColumnSpacing = 8
        };
        
        // Define column proportions
        headerGrid.ColumnsProportions.Add(new Proportion(ProportionType.Pixels, 48));  // Button
        headerGrid.ColumnsProportions.Add(new Proportion(ProportionType.Pixels, 48));  // Icon
        headerGrid.ColumnsProportions.Add(new Proportion(ProportionType.Fill));        // Name
        headerGrid.ColumnsProportions.Add(new Proportion(ProportionType.Pixels, 80));  // Cost
        headerGrid.ColumnsProportions.Add(new Proportion(ProportionType.Pixels, 60));  // Boost
        headerGrid.ColumnsProportions.Add(new Proportion(ProportionType.Pixels, 140)); // HP Bar

        var headerColor = new Color(100, 105, 115);
        
        AddHeaderLabel(headerGrid, "", 0);
        AddHeaderLabel(headerGrid, "", 1);
        AddHeaderLabel(headerGrid, "Body Part", 2, HorizontalAlignment.Left, headerColor);
        AddHeaderLabel(headerGrid, "Cost", 3, HorizontalAlignment.Right, headerColor);
        AddHeaderLabel(headerGrid, "Boost", 4, HorizontalAlignment.Right, headerColor);
        AddHeaderLabel(headerGrid, "Current HP", 5, HorizontalAlignment.Center, headerColor);
        
        return headerGrid;
    }

    private static void AddHeaderLabel(Grid grid, string text, int column, 
        HorizontalAlignment align = HorizontalAlignment.Center, Color? color = null)
    {
        var label = new Label(BaseContent.Styles.Label.Small)
        {
            Text = text,
            TextColor = color ?? Color.White,
            HorizontalAlignment = align,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(label, column);
        grid.Widgets.Add(label);
    }

    private void RefreshAll()
    {
        // Update fuel display
        _fuelValueLabel.Text = $"{Injector.TotalDamage:N0}";
        
        // Track max fuel seen for bar scaling
        if (Injector.TotalDamage > _maxFuelSeen)
            _maxFuelSeen = Injector.TotalDamage;
        
        
        // Update all part rows
        foreach (var (_, row) in _partRows)
        {
            row.Refresh();
        }
    }

    public void OnPartInjected()
    {
        RefreshAll();
    }

    public override void Update()
    {
    }

    /// <summary>
    /// A styled row representing a single body part in the injector panel.
    /// </summary>
    private sealed class BodyPartRow : Grid
    {
        private readonly SteroidInjectorPanel _panel;
        private readonly BodyPart _bodyPart;
        private readonly Button _injectButton;
        private readonly Label _costLabel;
        private readonly Label _boostLabel;
        private readonly Label _hpLabel;
        private readonly HorizontalProgressBar _hpBar;
        private readonly Image _partIcon;
        private readonly Label _partNameLabel;
        private readonly Panel _vitalIndicator;

        public BodyPartRow(SteroidInjectorPanel panel, BodyPart bodyPart)
        {
            _panel = panel;
            _bodyPart = bodyPart;
            
            Background = new SolidBrush(SectionBgColor);
            Padding = new Thickness(12, 8);
            ColumnSpacing = 8;
            
            // Column proportions matching header
            ColumnsProportions.Add(new Proportion(ProportionType.Pixels, 48));  // Button
            ColumnsProportions.Add(new Proportion(ProportionType.Pixels, 48));  // Icon
            ColumnsProportions.Add(new Proportion(ProportionType.Fill));        // Name
            ColumnsProportions.Add(new Proportion(ProportionType.Pixels, 80));  // Cost
            ColumnsProportions.Add(new Proportion(ProportionType.Pixels, 60));  // Boost
            ColumnsProportions.Add(new Proportion(ProportionType.Pixels, 140)); // HP Bar

            // === Inject Button ===
            _injectButton = new Button(BaseContent.Styles.Button.Plus64)
            {
                Width = 40,
                Height = 40,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            _injectButton.Click += OnInjectClicked;
            AddCell(_injectButton, 0);

            // === Part Icon ===
            _partIcon = new Image
            {
                Width = 40,
                Height = 40,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            AddCell(_partIcon, 1);

            // === Part Name with Vital Indicator ===
            var nameContainer = new HorizontalStackPanel
            {
                Spacing = 6,
                VerticalAlignment = VerticalAlignment.Center
            };
            
            _partNameLabel = new Label(BaseContent.Styles.Label.Normal)
            {
                Text = bodyPart.Label,
                VerticalAlignment = VerticalAlignment.Center
            };
            nameContainer.Widgets.Add(_partNameLabel);
            
            // Vital indicator badge
            _vitalIndicator = new Panel
            {
                Background = new SolidBrush(VitalIndicatorColor),
                Padding = new Thickness(6, 2),
                VerticalAlignment = VerticalAlignment.Center,
                Visible = bodyPart.AllInternalParts.Any(p => p.IsVital)
            };
            _vitalIndicator.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
            {
                Text = "VITAL",
                TextColor = Color.White
            });
            nameContainer.Widgets.Add(_vitalIndicator);
            
            AddCell(nameContainer, 2);

            // === Cost Label ===
            _costLabel = new Label(BaseContent.Styles.Label.Normal)
            {
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center
            };
            AddCell(_costLabel, 3);

            // === Boost Label ===
            _boostLabel = new Label(BaseContent.Styles.Label.Normal)
            {
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                TextColor = BoostColor
            };
            AddCell(_boostLabel, 4);

            // === HP Bar with Label ===
            var hpContainer = new Panel
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Center
            };
            
            _hpBar = new HorizontalProgressBar(BaseContent.Styles.Bar.Health)
            {
                Height = 20,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Center
            };
            hpContainer.Widgets.Add(_hpBar);
            
            _hpLabel = new Label(BaseContent.Styles.Label.Small)
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextColor = Color.White
            };
            hpContainer.Widgets.Add(_hpLabel);
            
            AddCell(hpContainer, 5);

            // Hover effect
            MouseEntered += (_, _) => Background = new SolidBrush(RowHoverColor);
            MouseLeft += (_, _) => Background = new SolidBrush(SectionBgColor);
        }

        private void AddCell(Widget widget, int column)
        {
            Grid.SetColumn(widget, column);
            Widgets.Add(widget);
        }

        private void OnInjectClicked(object? sender, EventArgs e)
        {
            _panel.Injector.InjectPart(_bodyPart);
            _panel.OnPartInjected();
        }

        public void Refresh()
        {
            var cost = _panel.Injector.CalculateTotalCost(_bodyPart);
            var canAfford = _panel.Injector.HasFuelFor(_bodyPart);
            var hpBoost = (int)Math.Clamp(_bodyPart.MaxHitPoints * 0.1, 1, 9999999);
            
            // Update button state
            _injectButton.Enabled = canAfford;
            
            // Update icon color based on health
            _partIcon.Background = new ColoredRegion(new TextureRegion(_bodyPart.Icon), BodyPartColor.Get(_bodyPart));
            
            // Update name color
            _partNameLabel.TextColor = canAfford ? BodyPartColor.Get(_bodyPart) : DisabledTextColor;
            
            // Update cost label
            _costLabel.Text = $"{cost:N0}";
            _costLabel.TextColor = canAfford ? CostAffordableColor : CostUnaffordableColor;
            
            // Update boost label
            _boostLabel.Text = $"+{hpBoost}";
            _boostLabel.TextColor = canAfford ? BoostColor : DisabledTextColor;
            
            // Update HP bar
            var hpPercent = (float)_bodyPart.HealthPercent * 100;
            _hpBar.Value = hpPercent;
            _hpLabel.Text = $"{_bodyPart.HitPoints:N0}/{_bodyPart.MaxHitPoints:N0}";
            
            // Color the HP bar filler based on health
            var hpColor = BodyPartColor.Get(_bodyPart);
            _hpBar.Filler = new ColoredRegion(Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Bar.Neutral], hpColor);
        }
    }
}
