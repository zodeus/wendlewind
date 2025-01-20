using Grafted.Sim.Entities;
using Grafted.Sim.Entities.Items.Trinkets;

namespace Grafted.Scenes.MainGameScene.Gui.Widgets.MiscWidgets;

public sealed class TrinketBar : HorizontalStackPanel
{
    private Dictionary<Item, TrinketBarCell> _trinkets = [];

    public TrinketBar(BaseGui gui, EntityContainer container, TrinketType type, Action<Item> clickAction)
    {
        container.ItemAdded += CreatePanel;
        foreach (var trinket in container)
        {
            CreatePanel(trinket);
        }

        return;

        void CreatePanel(Entity entity)
        {
            if (entity is not Item { ItemDef: { ItemType: ItemType.Trinket } } trinket) return;
            if (trinket.ItemDef.TrinketProperties?.Type != type) return;

            var panel = new TrinketBarCell(trinket, clickAction) { VerticalAlignment = VerticalAlignment.Bottom };
            _trinkets[trinket] = panel;
            Widgets.Add(panel);
        }
    }

    public void Update()
    {
        foreach (var (item, button) in _trinkets)
        {
            button.Update();
        }
    }
}

public sealed class TrinketBarCell : VerticalStackPanel
{
    private readonly Item _trinket;
    private readonly Label _label;
    private readonly Button _button;

    public TrinketBarCell(Item trinket, Action<Item> clickAction)
    {
        _trinket = trinket;
        _label = new Label(BaseContent.Styles.Label.Small)
        {
            HorizontalAlignment = HorizontalAlignment.Center, Visible = false
        };
        Widgets.Add(_label);

        _button = new Button()
        {
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrame],
            Padding = new Thickness(12),
            Width = 85,
            Height = 85,
            Content = new Image
            {
                VerticalAlignment = VerticalAlignment.Stretch, HorizontalAlignment = HorizontalAlignment.Stretch,
                Background = new TextureRegion(trinket.Icon),
            }
        };
        _button.Click += (_, _) => clickAction(trinket);
        Widgets.Add(_button);
    }

    public void Update()
    {
        if (_trinket.TrinketHandler?.IsActive == true)
        {
            _label.Text = _trinket.TrinketHandler.Charges.ToString();
            _button.Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrameBright];
            _label.TextColor = Color.DarkGoldenrod;
        }
        else
        {
            _label.Text = _trinket.TrinketHandler?.Cooldown > 0 ? $"{_trinket.TrinketHandler?.Cooldown}" : "";
            _label.TextColor = Color.DarkRed;
            if (_trinket.TrinketHandler?.Cooldown > 0)
            {
                _button.Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrameRed];
            }
            else
            {
                _button.Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrame];
            }
        }

        _label.Visible = _label.Text != "";
    }
}