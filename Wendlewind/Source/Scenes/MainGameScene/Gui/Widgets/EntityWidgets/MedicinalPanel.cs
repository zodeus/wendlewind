using System.Globalization;

namespace Wendlewind.Scenes.MainGameScene.Gui.Widgets.EntityWidgets;

/// <summary>
/// Panel for displaying medicinal items. Uses the handler's GetInfoPanel() method
/// to display custom infographics if available, otherwise shows a default layout.
/// </summary>
public class MedicinalPanel : EntityPanelBase
{
    private readonly Item _item;
    private readonly Label? _stackLabel;
    private readonly Widget? _customContent;

    public MedicinalPanel(BaseGui gui, Item item, EntityPanelProperties? properties = null) 
        : base(gui, item, properties)
    {
        _item = item;

        // Try to get a custom info panel from the handler
        var handler = item.ItemDef.MedicinalProperties?.Handler;
        _customContent = handler?.GetInfoPanel(item);

        if (_customContent != null)
        {
            // Use the custom panel content directly
            Widgets.Add(_customContent);
        }
        else
        {
            // Fall back to default consumable-style layout
            Padding = new Thickness(20);
            MinWidth = 300;
            
            _stackLabel = new Label("small")
            {
                Text = $"Stack Size: x{item.StackSize}",
                Margin = new Thickness(0, 0, 0, 15),
                Visible = item.IsStackable
            };
            
            Widgets.Add(new HorizontalStackPanel
            {
                Spacing = 10,
                Widgets =
                {
                    new Image { Background = new TextureRegion(item.Icon), Width = 128, Height = 128 },
                    new Label(BaseContent.Styles.Label.Normal)
                    {
                        Text = item.Def.Description,
                        Wrap = true,
                        MaxWidth = 400,
                        Margin = new Thickness(0, 10, 0, 0)
                    },
                }
            });
            Widgets.Add(_stackLabel);

            foreach (var baseStat in item.Def.BaseStats)
            {
                var row = new HorizontalStackPanel { Spacing = 10 };
                row.Widgets.Add(new Label("small") { Text = $"{baseStat.Def.Label}:" });
                row.Widgets.Add(new Label("small") { Text = item.GetStatValue(baseStat.Def).ToString(CultureInfo.InvariantCulture) });
                Widgets.Add(row);
            }
        }
    }

    public override void Update()
    {
        if (_stackLabel != null)
        {
            _stackLabel.Text = $"Stack Size: x{_item.StackSize}";
        }
    }
}
