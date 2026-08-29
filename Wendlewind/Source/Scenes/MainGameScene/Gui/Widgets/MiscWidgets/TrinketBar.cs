namespace Wendlewind.Scenes.MainGameScene.Gui.Widgets.MiscWidgets;

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
        
        // Hover tooltip using dynamic getter since label could change
        _button.WithDynamicTooltip(() => _trinket.Label, () => _trinket.Def.Description);
        
        _trinket.TrinketHandler?.PrepareTrinketButton(_button);
        Widgets.Add(_button);
    }

    public void Update()
    {
        _trinket.TrinketHandler?.Update(_button);
        TooltipHelper.UpdatePosition();
    }
}