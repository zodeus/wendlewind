using Myra.Graphics2D.Brushes;

namespace Grafted.Scenes.MainGameScene.Gui.Widgets.MiscWidgets;

public sealed class ZoneTimelineWindow : Window
{
    private const int NodeSize = 64;
    private const int NodeSpacing = 24;
    private const int BiomeSpacing = 40;

    public ZoneTimelineWindow()
    {
        Title = "Journey Timeline";
        Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrameBright];
        Padding = new Thickness(30);

        var scrollViewer = new ScrollViewer
        {
            Content = BuildTimeline(),
            ShowHorizontalScrollBar = true,
            ShowVerticalScrollBar = true,
            MaxHeight = 800
        };

        Content = new VerticalStackPanel
        {
            Spacing = 15,
            Widgets =
            {
                new HorizontalSeparator { Margin = new Thickness(0, 0, 0, 10) },
                scrollViewer
            }
        };
    }

    private Widget BuildTimeline()
    {
        var mainPanel = new VerticalStackPanel
        {
            Spacing = BiomeSpacing,
            Padding = new Thickness(20)
        };

        // Group encounters by biome
        var encountersByBiome = DefRepository<EncounterDef>.Defs
            .GroupBy(e => e.Biome)
            .OrderBy(g => DefRepository<BiomeDef>.Defs.ToList().IndexOf(g.Key))
            .ToList();

        var currentZone = Core.Context.CurrentZone;

        foreach (var biomeGroup in encountersByBiome)
        {
            var biomePanel = CreateBiomeRow(biomeGroup.Key, biomeGroup.ToList(), currentZone);
            mainPanel.Widgets.Add(biomePanel);
        }

        return mainPanel;
    }

    private Widget CreateBiomeRow(BiomeDef biome, List<EncounterDef> encounters, Zone? currentZone)
    {
        var isCurrentBiome = currentZone?.BiomeDef == biome;
        var zone = Core.Context.World.Zones.FirstOrDefault(z => z.BiomeDef == biome);
        var isCompleted = zone?.IsComplete ?? false;

        var container = new VerticalStackPanel
        {
            Spacing = 12
        };

        // Biome header with background preview
        var headerPanel = new HorizontalStackPanel
        {
            Spacing = 15,
            VerticalAlignment = VerticalAlignment.Center
        };

        // Biome icon/preview
        // var biomePreview = new Panel
        // {
        //     Width = 80,
        //     Height = 50,
        //     Background = new ColoredRegion(
        //         new TextureRegion(biome.BackgroundTexture),
        //         isCompleted ? new Color(100, 200, 100, 180) : (isCurrentBiome ? Color.White : new Color(80, 80, 80, 200))
        //     )
        // };
        // headerPanel.Widgets.Add(biomePreview);

        // Biome name
        var biomeLabel = new Label(BaseContent.Styles.Label.Medium)
        {
            Text = biome.Label,
            TextColor = isCurrentBiome ? Color.Gold : (isCompleted ? Color.LimeGreen : Color.LightGray),
            VerticalAlignment = VerticalAlignment.Center
        };
        headerPanel.Widgets.Add(biomeLabel);

        // Status indicator
        var statusLabel = new Label(BaseContent.Styles.Label.Small)
        {
            Text = isCompleted ? "✓ CLEARED" : (isCurrentBiome ? "◆ IN PROGRESS" : ""),
            TextColor = isCompleted ? Color.LimeGreen : Color.Orange,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(20, 0, 0, 0)
        };
        headerPanel.Widgets.Add(statusLabel);

        container.Widgets.Add(headerPanel);

        // Encounter nodes row
        var encounterRow = new HorizontalStackPanel
        {
            Spacing = 8,
            Margin = new Thickness(20, 0, 0, 0)
        };

        for (int i = 0; i < encounters.Count; i++)
        {
            var encounter = encounters[i];
            var encounterNode = CreateEncounterNode(encounter, i, currentZone, biome, isCompleted);
            encounterRow.Widgets.Add(encounterNode);

            // Add connector line between nodes (except after last)
            if (i < encounters.Count - 1)
            {
                encounterRow.Widgets.Add(CreateConnector(biome, i, isCompleted));
            }
        }

        container.Widgets.Add(encounterRow);

        return container;
    }

    private Widget CreateEncounterNode(EncounterDef encounter, int index, Zone? currentZone, BiomeDef biome, bool biomeCompleted)
    {
        // Get the zone for this biome from World.Zones to track actual progress
        var zone = Core.Context.World.Zones.FirstOrDefault(z => z.BiomeDef == biome);
        var zoneStage = zone?.Stage ?? 0;
        
        var isCurrentBiome = currentZone?.BiomeDef == biome;
        var isCurrentEncounter = isCurrentBiome && index == zoneStage;
        var isPastEncounter = index < zoneStage;
        var isCompleted = biomeCompleted || isPastEncounter;

        // Get enemy info
        var enemy = encounter.Enemies.FirstOrDefault();
        var enemyType = enemy?.PawnDef?.Label ?? "?";

        // Node colors based on state
        Color nodeColor;
        Color borderColor;
        if (encounter.IsBoss)
        {
            nodeColor = isCompleted ? new Color(180, 140, 60) : new Color(120, 60, 20);
            borderColor = Color.Gold;
        }
        else if (isCurrentEncounter)
        {
            nodeColor = new Color(60, 100, 160);
            borderColor = Color.Cyan;
        }
        else if (isCompleted)
        {
            nodeColor = new Color(60, 120, 60);
            borderColor = Color.LimeGreen;
        }
        else
        {
            nodeColor = new Color(10, 10, 10);
            borderColor = new Color(30, 30, 30);
        }

        var nodePanel = new VerticalStackPanel
        {
            Spacing = 6,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        // Inner content - stage number or boss icon
        var innerContent = new Label(BaseContent.Styles.Label.Medium)
        {
            Text = encounter.IsBoss ? "☠" : (index + 1).ToString(),
            TextColor = isCompleted ? Color.White : (isCurrentEncounter ? Color.Cyan : Color.Gray),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        // Border effect using a larger background panel
        var borderPanel = new Panel
        {
            Width = NodeSize + 4,
            Height = NodeSize + 4,
            Background = new SolidBrush(borderColor),
            HorizontalAlignment = HorizontalAlignment.Center
        };
        
        var innerNodePanel = new Panel
        {
            Width = NodeSize,
            Height = NodeSize,
            Background = new SolidBrush(nodeColor),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(2),
            Widgets = { innerContent }
        };
        borderPanel.Widgets.Add(innerNodePanel);

        // Pulsing effect for current encounter (via different styling)
        if (isCurrentEncounter)
        {
            borderPanel.Background = new SolidBrush(Color.Cyan);
        }

        nodePanel.Widgets.Add(borderPanel);

        // Enemy type label below node
        var enemyLabel = new Label(BaseContent.Styles.Label.Small)
        {
            Text = encounter.IsBoss ? "BOSS" : enemyType,
            TextColor = encounter.IsBoss ? Color.OrangeRed : Color.LightGray,
            HorizontalAlignment = HorizontalAlignment.Center,
            MaxWidth = NodeSize + 20,
            Wrap = true
        };
        nodePanel.Widgets.Add(enemyLabel);

        return nodePanel;
    }

    private Widget CreateConnector(BiomeDef biome, int index, bool biomeCompleted)
    {
        // Get the zone for this biome from World.Zones to track actual progress
        var zone = Core.Context.World.Zones.FirstOrDefault(z => z.BiomeDef == biome);
        var zoneStage = zone?.Stage ?? 0;
        var isPast = biomeCompleted || index < zoneStage;

        return new Panel
        {
            Width = NodeSpacing,
            Height = 4,
            Background = new SolidBrush(isPast ? Color.LimeGreen : new Color(20, 20, 20)),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, -24, 0, 0) // Offset to align with node centers
        };
    }
}

