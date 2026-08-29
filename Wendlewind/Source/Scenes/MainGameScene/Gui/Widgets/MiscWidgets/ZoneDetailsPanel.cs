namespace Wendlewind.Scenes.MainGameScene.Gui.Widgets.MiscWidgets;

public class ZoneDetailsPanel : VerticalStackPanel
{
    private static readonly Color SectionLabelColor = new(150, 140, 120);
    private static readonly int IconSize = BaseContent.IconSizes.Large;
    private const int MaxIconsPerRow = 6;

    public ZoneDetailsPanel(Zone zone)
    {
        var zoneDef = zone.ZoneDef;

        Spacing = 15;
        MinWidth = 500;

        // Title
        Widgets.Add(new Label(BaseContent.Styles.Label.Huge)
        {
            Text = zoneDef.Label,
            TextColor = Color.DarkGoldenrod,
            HorizontalAlignment = HorizontalAlignment.Center
        });

        // ENCOUNTERS section (includes weather beside pawn)
        Widgets.Add(CreateOpponentSection(zoneDef));

        // RESOURCES section
        if (zoneDef.Resources.Count > 0)
        {
            Widgets.Add(CreateResourcesSection(zoneDef));
        }

        // LOOT CHESTS section

        Widgets.Add(CreateLootChestsSection(zoneDef));

    }

    private static Widget CreateOpponentSection(ZoneDef zoneDef)
    {
        var section = new VerticalStackPanel { Spacing = 8 };
        var enemy = zoneDef.Encounters.First(e=>e.MysteryProperties == null).Enemies.First();
        
        section.Widgets.Add(new Label(BaseContent.Styles.Label.Normal)
        {
            Text = $"{enemy.PawnDef.Species}"
        });
        
        // Horizontal row with pawn render and weather side by side
        var renderAndWeatherRow = new HorizontalStackPanel { Spacing = 20 };
        
        var renderWidget = new PawnRenderWidget(PawnGenerator.CreatePawn(new PawnRequest(enemy.PawnDef.Species, enemy.PawnDef, Defs.PawnLoadouts.DefaultStarterLoadout, PawnType.Enemy)), 128)
        {
            Width = 128,
            Height = 128
        };
        renderAndWeatherRow.Widgets.Add(renderWidget);
        
        // Weather section beside pawn
        if (zoneDef.Weathers.Count > 0)
        {
            var weatherSection = new VerticalStackPanel 
            { 
                Spacing = 4, 
                VerticalAlignment = VerticalAlignment.Top
            };
            var label = CreateSectionLabel("Weather");
            label.Margin = new Thickness(0, 10, 0, 0);
            weatherSection.Widgets.Add(label);
            
            var weatherFlow = new VerticalStackPanel { Spacing = 8 };
            foreach (var weather in zoneDef.Weathers)
            {
                weatherFlow.Widgets.Add(new Label(BaseContent.Styles.Label.Normal)
                {
                    Text = weather.Label,
                    TextColor = weather.DisplayColor
                });
            }
            weatherSection.Widgets.Add(weatherFlow);
            renderAndWeatherRow.Widgets.Add(weatherSection);
        }
        
        section.Widgets.Add(renderAndWeatherRow);
        
        // Body description below the pawn renderer
        var bodyDef = enemy.PawnDef.Body;
        if (!string.IsNullOrEmpty(bodyDef.Description))
        {
            section.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
            {
                Text = bodyDef.Description,
                TextColor = new Color(220, 180, 120),
                MaxWidth = 500,
                Wrap = true
            });
        }
        
        return section;
    }
    private static List<ItemDef> GetAllEnemyDrops(ZoneDef zoneDef)
    {
        var drops = new HashSet<ItemDef>();
        foreach (var encounter in zoneDef.Encounters)
        {
            foreach (var enemy in encounter.Enemies)
            {
                foreach (var drop in enemy.InventoryItems)
                {
                    drops.Add(drop.Item);
                }
            }
        }
        return drops.ToList();
    }

    private static Widget CreateResourcesSection(ZoneDef zoneDef)
    {
        var section = new VerticalStackPanel { Spacing = 8 };
        section.Widgets.Add(CreateSectionLabel("Potential Resources"));
        var allDrops = GetAllEnemyDrops(zoneDef);
        var allUniqueResources = zoneDef.Resources.Select(r => r.Item).Concat(allDrops).Distinct().ToList();
        section.Widgets.Add(CreateWrappedIconRows(allUniqueResources, r => CreateIconCell(r.Icon).WithTooltip(r.Label)));
        return section;
    }

    private static Widget CreateLootChestsSection(ZoneDef zoneDef)
    {
        var uniqueChests = zoneDef.Encounters.SelectMany(e => e.PotentialLootBoxes).Distinct().OrderBy(c => c.Moniker).ToList();
        var section = new VerticalStackPanel { Spacing = 8 };
        section.Widgets.Add(CreateSectionLabel("Potential Chests"));
        section.Widgets.Add(CreateWrappedIconRows(uniqueChests, c => CreateIconCell(c.Icon).WithTooltip(c.Label)));
        return section;
    }

    override public void InternalRender(RenderContext context)
    {
        base.InternalRender(context);
        TooltipHelper.UpdatePosition();
    }

    private static Label CreateSectionLabel(string text)
    {
        return new Label(BaseContent.Styles.Label.Small)
        {
            Text = text,
            TextColor = SectionLabelColor
        };
    }

    private static Widget CreateWrappedIconRows<T>(List<T> items, Func<T, Widget> createWidget)
    {
        var container = new VerticalStackPanel { Spacing = 10 };
        var rowCount = (items.Count + MaxIconsPerRow - 1) / MaxIconsPerRow;

        for (var r = 0; r < rowCount; r++)
        {
            var row = new HorizontalStackPanel { Spacing = 10 };
            for (var col = 0; col < MaxIconsPerRow; col++)
            {
                var index = r * MaxIconsPerRow + col;
                if (index < items.Count)
                {
                    row.Widgets.Add(createWidget(items[index]));
                }
            }
            container.Widgets.Add(row);
        }

        return container;
    }

    private static Widget CreateIconCell(Texture2D icon)
    {
        return new Panel
        {
            Width = IconSize,
            Height = IconSize,
            Widgets =
            {
                new Image
                {
                    Background = new TextureRegion(icon),
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch
                }
            }
        };
    }
}