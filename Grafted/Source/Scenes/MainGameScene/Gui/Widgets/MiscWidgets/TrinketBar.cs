namespace Grafted.Scenes.MainGameScene.Gui.Widgets.MiscWidgets;

public sealed class TrinketBar : VerticalStackPanel
{
    private Dictionary<Item, TrinketBarCell> _trinkets = [];
    private HorizontalStackPanel _currentRow = new();
    public int TrinketsPerRow { get; set; } = 10;

    public TrinketBar(PawnInventory inventory, TrinketType type, Action<Item>? clickAction = null)
    {
        Widgets.Add(_currentRow);
        inventory.ItemAdded += CreatePanel;
        foreach (var trinket in inventory.Trinkets)
        {
            CreatePanel(trinket);
        }

        return;

        void CreatePanel(Entity entity)
        {
            if (entity is not Item { ItemDef: { ItemType: ItemType.Trinket } } trinket) return;
            if (trinket.ItemDef.TrinketProperties?.Type != type) return;
            
            if (_currentRow.Widgets.Count >= TrinketsPerRow)
            {
                _currentRow = new HorizontalStackPanel();
                Widgets.Add(_currentRow);
            }

            var panel = new TrinketBarCell(trinket, clickAction) { VerticalAlignment = VerticalAlignment.Bottom };
            _trinkets[trinket] = panel;
            _currentRow.Widgets.Add(panel);
        }
    }

    override public void InternalRender(RenderContext context)
    {
        base.InternalRender(context);
        foreach (var (item, button) in _trinkets)
        {
            button.Update();
        }
    }
}

public sealed class TrinketBarCell : VerticalStackPanel
{
    private readonly Item _trinket;
    private readonly CursorButton _button;
    private Window? _tooltipWindow;
    private Label? _tooltipTitle;
    private Label? _tooltipDescription;

    public TrinketBarCell(Item trinket, Action<Item>? clickAction)
    {
        Spacing = 0;
        _trinket = trinket;
        _button = new CursorButton
        {
            
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrame],
            Padding = new Thickness(6),
            Width = BaseContent.IconSizes.Large,
            Height = BaseContent.IconSizes.Large,
            Content = new Panel
            {
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Background = new TextureRegion(trinket.Icon),
            }
        };
        _button.Click += (_, _) => {
            if (clickAction != null) {
                clickAction(trinket);
                return;
            }
            trinket.TrinketHandler?.OnClick();
        };
        
        // Hover tooltip
        _button.MouseEntered += (_, _) => ShowTooltip();
        _button.MouseLeft += (_, _) => HideTooltip();
        
        _trinket.TrinketHandler?.PrepareTrinketButton(_button);
        Widgets.Add(_button);
    }

    private void EnsureTooltipCreated()
    {
        if (_tooltipWindow != null) return;

        _tooltipTitle = new Label(BaseContent.Styles.Label.Normal)
        {
            TextColor = Color.White
        };
        _tooltipDescription = new Label(BaseContent.Styles.Label.Small)
        {
            TextColor = new Color(180, 180, 180),
            Wrap = true,
            MaxWidth = 250
        };

        var content = new VerticalStackPanel { Spacing = 4 };
        content.Widgets.Add(_tooltipTitle);
        content.Widgets.Add(_tooltipDescription);

        _tooltipWindow = new Window
        {
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.IconFrame],
            Padding = new Thickness(10,3,10,10),
            Content = content
        };
        _tooltipWindow.TitlePanel.Visible = false;
    }

    private void ShowTooltip()
    {
        if (Desktop == null) return;
        
        EnsureTooltipCreated();
        
        // Update tooltip content
        _tooltipTitle!.Text = _trinket.Label;
        _tooltipDescription!.Text = _trinket.Def.Description;
        
        // Position tooltip near the mouse
        var screenPos = Mouse.GetState().Position;
        var uiX = (int)((screenPos.X - Core.UiOffset.X) / Core.UiScale);
        var uiY = (int)((screenPos.Y - Core.UiOffset.Y) / Core.UiScale);
        
        if (!_tooltipWindow!.IsPlaced)
        {
            _tooltipWindow.Show(Desktop, new Point(uiX + 15, uiY + 15));
        }
        else
        {
            _tooltipWindow.Left = uiX + 15;
            _tooltipWindow.Top = uiY + 15;
        }
    }

    private void HideTooltip()
    {
        _tooltipWindow?.Close();
    }

    public void Update()
    {
        _trinket.TrinketHandler?.Update(_button);
        
        // Update tooltip position while hovering
        if (_tooltipWindow?.IsPlaced == true)
        {
            var screenPos = Mouse.GetState().Position;
            var uiX = (int)((screenPos.X - Core.UiOffset.X) / Core.UiScale);
            var uiY = (int)((screenPos.Y - Core.UiOffset.Y) / Core.UiScale);
            
            _tooltipWindow.Left = uiX + 15;
            _tooltipWindow.Top = uiY + 15;
        }
    }
}