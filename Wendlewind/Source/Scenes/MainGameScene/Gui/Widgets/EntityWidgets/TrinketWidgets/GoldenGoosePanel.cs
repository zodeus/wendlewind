using Myra.Graphics2D.Brushes;

namespace Wendlewind.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.TrinketWidgets;

[UsedImplicitly]
public sealed class GoldenGoosePanel : EntityPanelBase
{
    private readonly GoldenGooseHandler? _handler;
    private readonly Item _item;
    private readonly Label _hungerValueLabel;
    private readonly Label _beansPreviewLabel;
    private readonly HorizontalProgressBar _hungerBar;
    private readonly VerticalStackPanel _foodList;

    public GoldenGoosePanel(BaseGui gui, Item item, EntityPanelProperties? properties = null)
        : base(gui, item, properties)
    {
        _item = item;
        _handler = item.TrinketHandler as GoldenGooseHandler;
        Width = 400;
        Spacing = 0;

        // Combined header with icon and hunger info
        var headerSection = new Panel
        {
            Margin = new Thickness(0, 0, 0, 16)
        };

        var headerContent = new HorizontalStackPanel
        {
            Spacing = 16,
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
                            Background = new TextureRegion(item.GetIcon()),
                            Width = 56,
                            Height = 56,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center
                        }
                    }
                }
            }
        };

        var hungerContent = new VerticalStackPanel
        {
            Spacing = 8,
            VerticalAlignment = VerticalAlignment.Center
        };

        hungerContent.Widgets.Add(new Label(BaseContent.Styles.Label.Normal)
        {
            Text = "Hunger Level",
            TextColor = Color.Goldenrod
        });

        _hungerValueLabel = new Label(BaseContent.Styles.Label.Large)
        {
            TextColor = _handler!.GetHungerColor()
        };
        hungerContent.Widgets.Add(_hungerValueLabel);

        _hungerBar = new HorizontalProgressBar(BaseContent.Styles.Bar.Health)
        {
            Height = 12,
            Width = 220,
            Filler = new ColoredRegion(
                Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Bar.Neutral],
                _handler!.GetHungerColor())
        };
        hungerContent.Widgets.Add(_hungerBar);

        headerContent.Widgets.Add(hungerContent);
        headerSection.Widgets.Add(headerContent);
        Widgets.Add(headerSection);

        // Beans preview section
        var beansSection = new Panel
        {
            Background = new SolidBrush(new Color(40, 38, 20)),
            Padding = new Thickness(8),
            Margin = new Thickness(0, 0, 0, 16)
        };

        var beansContent = new HorizontalStackPanel { Spacing = 12 };

        // Golden bean icon placeholder
        beansContent.Widgets.Add(new Image
        {
            Background = new TextureRegion(Defs.Items.GoldenBean.GetIcon()),
            Width = 32,
            Height = 32,
            VerticalAlignment = VerticalAlignment.Center
        });

        var beansInfo = new VerticalStackPanel { Spacing = 2 };
        beansInfo.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
        {
            Text = "Next Encounter Yield",
            TextColor = new Color(120, 110, 80)
        });

        _beansPreviewLabel = new Label(BaseContent.Styles.Label.Medium)
        {
            TextColor = Color.Goldenrod
        };
        beansInfo.Widgets.Add(_beansPreviewLabel);
        beansContent.Widgets.Add(beansInfo);

        beansSection.Widgets.Add(beansContent);
        Widgets.Add(beansSection);

        // Feed section header
        Widgets.Add(new Label(BaseContent.Styles.Label.Normal)
        {
            Text = "Feed the Goose",
            TextColor = new Color(180, 170, 130),
            Margin = new Thickness(0, 0, 0, 8)
        });

        // Food list
        _foodList = new VerticalStackPanel
        {
            Spacing = 4
        };

        var scrollViewer = new ScrollViewer
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Height = 180,
            Content = _foodList
        };
        Widgets.Add(scrollViewer);

        // Usage hint
        var hintLabel = new Label(BaseContent.Styles.Label.Small)
        {
            Text = "Feed raw food to keep the goose happy. A well-fed goose produces more golden beans after each encounter.",
            TextColor = new Color(100, 100, 100),
            Wrap = true,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 12, 0, 0)
        };
        Widgets.Add(hintLabel);
        RefreshFoodList();
        RefreshDisplay();
    }
    
    private void RefreshDisplay()
    {
        if (_handler == null) return;
        
        // Update hunger display
        _hungerValueLabel.Text = $"{_handler.Hunger}%";
        _hungerValueLabel.TextColor = _handler.GetHungerColor();
        _hungerBar.Value = _handler.Hunger;
        
        // Update hunger bar color based on level
        var barColor = _handler.GetHungerColor();
        _hungerBar.Filler = new ColoredRegion(
            Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Bar.Neutral], 
            barColor);
        
        // Update beans preview
        var beansCount = _handler.BeansToGenerate;
        _beansPreviewLabel.Text = beansCount switch
        {
            0 => "None (goose hungry!)",
            1 => "1 Golden Bean",
            _ => $"{beansCount} Golden Beans"
        };
        _beansPreviewLabel.TextColor = _handler!.GetHungerColor();
    }
    
    private void RefreshFoodList()
    {
        _foodList.Widgets.Clear();
        
        var inventory = Core.Context.PlayerPawn.Inventory;
        var foundFood = false;
        
        foreach (var item in inventory)
        {
            if (_handler?.CanEat(item.ItemDef) != true) continue;
            
            foundFood = true;
            var foodRow = CreateFoodRow(item);
            _foodList.Widgets.Add(foodRow);
        }
        
        if (!foundFood)
        {
            _foodList.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
            {
                Text = "No suitable food in inventory.\nFind Raw Fish, Raw Meat, or Raw Corn.",
                TextColor = new Color(100, 100, 100),
                HorizontalAlignment = HorizontalAlignment.Center,
                Wrap = true
            });
        }
    }
    
    private HorizontalStackPanel CreateFoodRow(Item food)
    {
        var row = new HorizontalStackPanel
        {
            Spacing = 10,
            Margin = new Thickness(0, 4, 0, 4)
        };
        
        // Feed button first (like SlingshotPanel)
        var feedButton = new CursorButton(BaseContent.Styles.Button.Normal)
        {
            VerticalAlignment = VerticalAlignment.Center,
            Content = new Label(BaseContent.Styles.Label.Small) { Text = "Feed" }
        };
        
        var capturedFood = food;
        feedButton.TouchDown += (_, _) =>
        {
            if (_handler == null) return;
            
            // Feed the goose first (before modifying the item)
            _handler.Feed(capturedFood);
            
            // Then consume one item from the stack
            if (capturedFood.StackSize > 1)
            {
                capturedFood.StackSize--;
            }
            else
            {
                Core.Context.PlayerPawn.Inventory.Remove(capturedFood);
                capturedFood.Destroy();
            }
            RefreshFoodList();
            RefreshDisplay();
        };
        
        row.Widgets.Add(feedButton);
        
        // Food icon
        row.Widgets.Add(new Image
        {
            Background = new TextureRegion(food.GetIcon()),
            Width = 32,
            Height = 32,
            VerticalAlignment = VerticalAlignment.Center
        });
        
        // Food info
        var hungerGain = _handler!.GetNutritionValue(food);
        row.Widgets.Add(new VerticalStackPanel
        {
            VerticalAlignment = VerticalAlignment.Center,
            Spacing = 2,
            Widgets =
            {
                new Label(BaseContent.Styles.Label.Normal)
                {
                    Text = $"{food.Label} x{food.StackSize}",
                    TextColor = new Color(200, 190, 160)
                },
                new Label(BaseContent.Styles.Label.Small)
                {
                    Text = $"+{hungerGain} hunger",
                    TextColor = _handler!.GetHungerColor()
                }
            }
        });
        
        return row;
    }
    
    public override void Update()
    {
        RefreshDisplay();
    }
}

