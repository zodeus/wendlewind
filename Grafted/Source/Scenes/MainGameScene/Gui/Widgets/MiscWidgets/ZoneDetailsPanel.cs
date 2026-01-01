namespace Grafted.Scenes.MainGameScene.Gui.Widgets.MiscWidgets;

public class ZoneDetailsPanel : VerticalStackPanel
{
    private static readonly Color SectionLabelColor = new(150, 140, 120);
    private static readonly Color BossColor = new(200, 60, 40);
    private static readonly Color StatNumberColor = new(232, 170, 0);
    private static readonly int IconSize = BaseContent.IconSizes.Large;

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
        var section = new VerticalStackPanel { Spacing = 4 };
        var enemy = zoneDef.Encounters.First(e=>e.ShrineProperties == null).Enemies.First();
        
        section.Widgets.Add(new Label(BaseContent.Styles.Label.Normal)
        {
            Text = $"Opponent: {enemy.PawnDef.Species}"
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
                Spacing = 4, VerticalAlignment = VerticalAlignment.Top
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
        var allUniqueResources = zoneDef.Resources.Select(r => r.Item).Concat(allDrops).Distinct();
        var iconsRow = new HorizontalStackPanel { Spacing = 10 };
        foreach (var resource in allUniqueResources)
        {
            iconsRow.Widgets.Add(CreateIconCell(resource.Icon));
        }
        section.Widgets.Add(iconsRow);
        return section;
    }

    private static Widget CreateLootChestsSection(ZoneDef zoneDef)
    {

        var uniqueChests = zoneDef.Encounters.SelectMany(e => e.PotentialLootBoxes).Distinct().OrderBy(c => c.Moniker);
        var section = new VerticalStackPanel { Spacing = 8 };
        var iconsRow = new HorizontalStackPanel { Spacing = 10 };
        section.Widgets.Add(CreateSectionLabel("Potential Chests"));
        foreach (var chest in uniqueChests)
        {
            var chestCell = new VerticalStackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                Spacing = 4
            };
            chestCell.Widgets.Add(CreateIconCell(chest.Icon));
            iconsRow.Widgets.Add(chestCell);
        }
        section.Widgets.Add(iconsRow);

        return section;
    }

    private static Label CreateSectionLabel(string text)
    {
        return new Label(BaseContent.Styles.Label.Small)
        {
            Text = text,
            TextColor = SectionLabelColor
        };
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