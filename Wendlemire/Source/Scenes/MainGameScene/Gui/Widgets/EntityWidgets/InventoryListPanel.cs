using FontStashSharp.RichText;
using Image = Myra.Graphics2D.UI.Image;

namespace Wendlemire.Scenes.MainGameScene.Gui.Widgets.EntityWidgets;

public sealed class InventoryCardGrid : VerticalStackPanel, IUpdatable
{
    private const int CardWidth = 118;
    private const int CardHeight = 62;
    private const int CardSpacing = 4;

    private readonly BaseGui _gui;
    private readonly IReadOnlyList<Item> _items;
    private readonly Dictionary<Item, InventoryItemCard> _cards = [];
    private int _cardsPerRow = -1;

    public InventoryCardGrid(BaseGui gui, IReadOnlyList<Item> items)
    {
        _gui = gui;
        _items = items;
        Spacing = CardSpacing;
        HorizontalAlignment = HorizontalAlignment.Stretch;
        Rebuild();
    }

    public void Update()
    {
        var perRow = CardsPerRow();
        if (perRow != _cardsPerRow)
        {
            Rebuild();
            return;
        }

        foreach (var card in _cards.Values)
        {
            card.Refresh();
        }
    }

    private int CardsPerRow()
    {
        var width = Math.Max(ActualBounds.Width, Bounds.Width);
        if (width <= 0)
        {
            return 1;
        }

        return Math.Max(1, (width + CardSpacing) / (CardWidth + CardSpacing));
    }

    private void Rebuild()
    {
        _cardsPerRow = CardsPerRow();
        _cards.Clear();
        Widgets.Clear();

        HorizontalStackPanel? row = null;
        for (var i = 0; i < _items.Count; i++)
        {
            if (i % _cardsPerRow == 0)
            {
                row = new HorizontalStackPanel { Spacing = CardSpacing };
                Widgets.Add(row);
            }

            var item = _items[i];
            var card = new InventoryItemCard(_gui, item)
            {
                Width = CardWidth,
                Height = CardHeight
            };
            _cards[item] = card;
            row!.Widgets.Add(card);
        }
    }
}

internal sealed class InventoryItemCard : CursorButton
{
    private readonly Item _item;
    private readonly Label _stackLabel;

    public InventoryItemCard(BaseGui gui, Item item) : base(BaseContent.Styles.Button.Dark)
    {
        _item = item;
        Padding = new Thickness(4, 4, 4, 3);

        var icon = new Panel
        {
            Width = BaseContent.IconSizes.Default + 4,
            Height = BaseContent.IconSizes.Default + 4,
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.IconFrame],
            HorizontalAlignment = HorizontalAlignment.Center,
            Widgets =
            {
                new Image
                {
                    Background = item.GetIconImage(),
                    Width = BaseContent.IconSizes.Default,
                    Height = BaseContent.IconSizes.Default,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            }
        };

        _stackLabel = new Label(BaseContent.Styles.Label.Small)
        {
            TextColor = Color.Goldenrod,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom
        };
        icon.Widgets.Add(_stackLabel);

        var name = new Label(BaseContent.Styles.Label.Small)
        {
            Text = item.Label,
            HorizontalAlignment = HorizontalAlignment.Center,
            SingleLine = true,
            AutoEllipsisMethod = AutoEllipsisMethod.Character,
            MaxWidth = 108
        };

        Content = new VerticalStackPanel
        {
            Spacing = 2,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Widgets = { icon, name }
        };

        Click += (_, _) => gui.ViewEntity(item);
        this.WithDynamicTooltip(() => item.Label, () => item.Description);
        Refresh();
    }

    public void Refresh()
    {
        _stackLabel.Text = _item.IsStackable && _item.StackSize > 1 ? $"x{_item.StackSize}" : "";
        _stackLabel.Visible = _stackLabel.Text.Length > 0;
    }
}
