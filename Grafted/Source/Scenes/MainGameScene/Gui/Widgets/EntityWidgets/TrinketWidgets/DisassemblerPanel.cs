namespace Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.TrinketWidgets;

[UsedImplicitly]
public sealed class DisassemblerPanel : EntityPanelBase
{
    private readonly VerticalStackPanel _itemsPanel;

    public DisassemblerPanel(BaseGui gui, Item _, EntityPanelProperties? props = null) : base(gui, _, props)
    {
        Padding = new Thickness(20);
        MinWidth = 720;
        Height = 720;

        // Subtitle
        Widgets.Add(new Label(BaseContent.Styles.Label.Small)
        {
            Text = "Break down equipment into raw materials",
            TextColor = new Color(140, 130, 120),
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 16)
        });

        // Items list container
        _itemsPanel = new VerticalStackPanel { Spacing = 8 };
        Widgets.Add(new ScrollViewer
        {
            VerticalAlignment = VerticalAlignment.Stretch,
            Content = _itemsPanel
        });

        Redraw();
    }

    private void Redraw()
    {
        _itemsPanel.Widgets.Clear();

        var disassemblableItems = Core.Context.PlayerPawn.Inventory
            .Where(item => item.ItemDef.DisassembleProperties is not null)
            .ToList();

        if (disassemblableItems.Count == 0)
        {
            _itemsPanel.Widgets.Add(CreateEmptyState());
            return;
        }

        foreach (var item in disassemblableItems)
        {
            _itemsPanel.Widgets.Add(new DisassembleItemCard(item, Redraw));
        }
    }

    private static Widget CreateEmptyState()
    {
        var panel = new VerticalStackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Spacing = 12,
            Margin = new Thickness(0, 60, 0, 0)
        };

        panel.Widgets.Add(new Label(BaseContent.Styles.Label.Huge)
        {
            Text = "∅",
            TextColor = new Color(60, 55, 50),
            HorizontalAlignment = HorizontalAlignment.Center
        });

        panel.Widgets.Add(new Label(BaseContent.Styles.Label.Normal)
        {
            Text = "No items to disassemble",
            TextColor = new Color(100, 90, 80),
            HorizontalAlignment = HorizontalAlignment.Center
        });

        panel.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
        {
            Text = "Equipment and certain items can be broken down",
            TextColor = new Color(80, 70, 60),
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 8, 0, 0)
        });

        return panel;
    }

    public override void Update()
    {
    }
}

public sealed class DisassembleItemCard : Panel
{
    private readonly Item _item;
    private readonly DisassembleProperties _properties;
    private readonly Action _redraw;
    private IBrush? _defaultBackground;
    private IBrush? _hoverBackground;

    public DisassembleItemCard(Item item, Action redraw)
    {
        _item = item;
        _redraw = redraw;
        _properties = item.ItemDef.DisassembleProperties!;

        Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrame];
        _defaultBackground = Background;
        _hoverBackground = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrameBright];
        Padding = new Thickness(12);
        HorizontalAlignment = HorizontalAlignment.Stretch;

        var mainLayout = new HorizontalStackPanel
        {
            Spacing = 12,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        // Left section: Item info
        var leftSection = new HorizontalStackPanel { Spacing = 12 };

        // Item icon with frame
        var iconContainer = new Panel
        {
            Width = 64,
            Height = 64,
            VerticalAlignment = VerticalAlignment.Center
        };

        var iconFrame = new Panel
        {
            Width = 64,
            Height = 64,
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.DeepGold],
            Padding = new Thickness(4)
        };
        iconFrame.Widgets.Add(new Image
        {
            Background = new TextureRegion(item.Icon),
            Width = 56,
            Height = 56,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        });
        iconContainer.Widgets.Add(iconFrame);

        leftSection.Widgets.Add(iconContainer);

        // Item details (name + durability)
        var itemDetails = new VerticalStackPanel
        {
            Spacing = 4,
            VerticalAlignment = VerticalAlignment.Center,
            Width = 140
        };

        itemDetails.Widgets.Add(new Label(BaseContent.Styles.Label.Normal)
        {
            Text = item.Label,
            TextColor = Color.White,
            Wrap = true,
            MaxWidth = 140
        });

        // Show durability if the item has it
        if (item.MaxDurability > 0)
        {
            var durabilityContainer = new VerticalStackPanel { Spacing = 2 };

            var durabilityPercent = item.Durability / item.MaxDurability * 100f;
            var durabilityBar = new HorizontalProgressBar(BaseContent.Styles.Bar.Durability)
            {
                Width = 100,
                Height = 14,
                Minimum = 0,
                Maximum = 100,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            durabilityBar.Value = durabilityPercent;
            durabilityContainer.Widgets.Add(durabilityBar);

            var durabilityColor = durabilityPercent > 50 ? new Color(120, 200, 120) :
                                  durabilityPercent > 25 ? new Color(220, 180, 80) :
                                  new Color(200, 100, 100);

            durabilityContainer.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
            {
                Text = $"{item.Durability:0}/{item.MaxDurability:0}",
                TextColor = durabilityColor
            });

            itemDetails.Widgets.Add(durabilityContainer);
        }

        leftSection.Widgets.Add(itemDetails);
        mainLayout.Widgets.Add(leftSection);

        // Arrow indicator
        mainLayout.Widgets.Add(new Label(BaseContent.Styles.Label.Large)
        {
            Text = "→",
            TextColor = new Color(100, 90, 80),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(8, 0, 8, 0)
        });

        // Right section: Output resources
        var outputSection = new HorizontalStackPanel
        {
            Spacing = 8,
            VerticalAlignment = VerticalAlignment.Center
        };

        foreach (var resource in _properties.Items)
        {
            outputSection.Widgets.Add(CreateResourceOutput(resource));
        }

        mainLayout.Widgets.Add(outputSection);

        // Spacer to push button to right
        var spacer = new Panel { Width = 1 };
        HorizontalStackPanel.SetProportionType(spacer, ProportionType.Fill);
        mainLayout.Widgets.Add(spacer);

        // Disassemble button
        var button = CreateDisassembleButton();
        mainLayout.Widgets.Add(button);

        Widgets.Add(mainLayout);

        // Hover effects
        MouseEntered += (_, _) =>
        {
            Background = _hoverBackground;
            Mouse.SetCursor(Microsoft.Xna.Framework.Input.MouseCursor.Hand);
        };

        MouseLeft += (_, _) =>
        {
            Background = _defaultBackground;
            Mouse.SetCursor(Microsoft.Xna.Framework.Input.MouseCursor.Arrow);
        };
    }

    private static Widget CreateResourceOutput(ResourceCount resource)
    {
        var container = new VerticalStackPanel
        {
            Spacing = 2,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        var iconFrame = new Panel
        {
            Width = 48,
            Height = 48,
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.RoundDark64],
            Padding = new Thickness(4),
            HorizontalAlignment = HorizontalAlignment.Center
        };

        iconFrame.Widgets.Add(new Image
        {
            Background = new TextureRegion(resource.Item.Icon),
            Width = 40,
            Height = 40,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        });

        container.Widgets.Add(iconFrame);

        if (resource.Count > 1)
        {
            container.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
            {
                Text = $"×{resource.Count}",
                TextColor = new Color(180, 220, 180),
                HorizontalAlignment = HorizontalAlignment.Center
            });
        }

        return container;
    }

    private Button CreateDisassembleButton()
    {
        var button = new Button(BaseContent.Styles.Button.Dark)
        {
            VerticalAlignment = VerticalAlignment.Center,
            Padding = new Thickness(12, 8, 12, 8)
        };

        var buttonContent = new HorizontalStackPanel
        {
            Spacing = 6
        };

        buttonContent.Widgets.Add(new Image
        {
            Width = 20,
            Height = 20,
            Background = new ColoredRegion(
                Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Icon.Disassemble],
                new Color(220, 100, 100)
            ),
            VerticalAlignment = VerticalAlignment.Center
        });

        buttonContent.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
        {
            Text = "Break Down",
            TextColor = new Color(220, 100, 100),
            VerticalAlignment = VerticalAlignment.Center
        });

        button.Content = buttonContent;

        button.Click += (_, _) =>
        {
            Disassemble();
            _redraw();
        };

        button.MouseEntered += (_, _) =>
        {
            Mouse.SetCursor(Microsoft.Xna.Framework.Input.MouseCursor.Hand);
        };

        return button;
    }

    private void Disassemble()
    {
        foreach (var resource in _properties.Items)
        {
            Core.Context.PlayerPawn.Inventory.TryAdd(
                EntityGenerator.CreateEntity<Item>(resource.Item, resource.Count)
            );
        }

        Core.Context.Achievements.OnItemDisassembled(_item);
        _item.Destroy();
    }
}
