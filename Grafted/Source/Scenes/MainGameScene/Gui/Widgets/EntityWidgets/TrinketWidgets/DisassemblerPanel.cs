using Grafted.Sim.Entities;

namespace Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.TrinketWidgets;

[UsedImplicitly]
public sealed class DisassemblerPanel : EntityPanelBase
{
    public DisassemblerPanel(BaseGui gui, Item _, EntityPanelProperties? props = null) : base(gui, _, props)
    {
        Padding = new Thickness(15);
        MinWidth = 500;
        Height = 700;
        Redraw();
    }

    private void Redraw()
    {
        Widgets.Clear();
        var panel = new VerticalStackPanel() { Spacing = 10 };
        Widgets.Add(new ScrollViewer()
        {
            VerticalAlignment = VerticalAlignment.Stretch,
            Content = panel
        });
        foreach (var item in Core.Context.PlayerPawn.Inventory)
        {
            if (item.ItemDef.DisassembleProperties is null) continue;
            panel.Widgets.Add(new DisassembleItemPanel(item, Redraw));
        }
    }


    public override void Update()
    {
    }
}

public sealed class DisassembleItemPanel : HorizontalStackPanel
{
    private readonly Item _item;
    private readonly DisassembleProperties _properties;

    public DisassembleItemPanel(Item item, Action redraw)
    {
        _item = item;
        Spacing = 10;
        _properties = item.ItemDef.DisassembleProperties!;
        Widgets.Clear();
        var button = new Button(BaseContent.Styles.Button.Normal)
        {
            VerticalAlignment = VerticalAlignment.Center,
            Content = new Image
            {
                Width = BaseContent.IconSizes.Small,
                Height = BaseContent.IconSizes.Small,
                Background = new ColoredRegion(
                    Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Icon.Disassemble],
                    Color.White
                )
            }
        };
        button.Click += (_, __) =>
        {
            Disassemble();
            redraw();
        };
        Widgets.Add(button);
        Widgets.Add(new EntityIcon(_item));
        Widgets.Add(new VerticalSeparator());
        foreach (var resource in _properties.Items)
        {
            Widgets.Add(new HorizontalStackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                Widgets =
                {
                    new EntityIcon(resource.Item, BaseContent.IconSizes.Medium),
                    new Label(BaseContent.Styles.Label.Small)
                    {
                        VerticalAlignment = VerticalAlignment.Bottom,
                        Text = $"x{resource.Count}"
                    }
                }
            });
        }
    }


    private void Disassemble()
    {
        foreach (var resource in _properties.Items)
        {
            Core.Context.PlayerPawn.Inventory.TryAdd(EntityGenerator.CreateEntity<Item>(resource.Item, resource.Count));
        }

        _item.Destroy();
    }
}