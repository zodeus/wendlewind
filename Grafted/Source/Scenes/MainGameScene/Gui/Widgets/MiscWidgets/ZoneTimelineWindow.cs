using Grafted.Sim.LootBoxes;
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
        if (isCompleted)
        {
            nodeColor = new Color(51, 92, 1);
            borderColor = new Color(68, 122, 1);
        }
        else if (encounter.IsBoss)
        {
            nodeColor = new Color(18, 6, 0);
            borderColor = new Color(66, 23, 1);
        }
        else if (isCurrentEncounter)
        {
            nodeColor = new Color(60, 100, 160);
            borderColor = Color.Cyan;
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

        // Inner content - grid of chest icons or checkmark for completed
        Widget innerContent;
        if (isCompleted)
        {
            innerContent = new Label(BaseContent.Styles.Label.Medium)
            {
                Text = "✓",
                TextColor = Color.LimeGreen,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
        }
        else
        {
            innerContent = CreateChestIconGrid(encounter.PotentialLootBoxes, isCurrentEncounter);
        }

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
            Background = new SolidBrush(isPast ? new Color(68, 122, 1) : new Color(20, 20, 20)),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, -24, 0, 0) // Offset to align with node centers
        };
    }

    private Widget CreateChestIconGrid(List<LootBoxDef> lootBoxes, bool isCurrentEncounter)
    {
        if (lootBoxes.Count == 0)
        {
            return new Label(BaseContent.Styles.Label.Small)
            {
                Text = "?",
                TextColor = isCurrentEncounter ? Color.Cyan : Color.Gray,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
        }

        // Calculate grid dimensions - prefer 2 columns for 2-4 items, 3 for more
        var count = lootBoxes.Count;
        var columns = count <= 2 ? count : (count <= 4 ? 2 : 3);
        var rows = (int)Math.Ceiling((double)count / columns);

        // Calculate icon size to fit within NodeSize with some padding
        var padding = 4;
        var availableSize = NodeSize - (padding * 2);
        var iconSize = Math.Min(availableSize / columns, availableSize / rows) - 2;
        iconSize = Math.Max(iconSize, 12); // Minimum icon size

        var grid = new Grid
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            ColumnSpacing = 2,
            RowSpacing = 2
        };

        // Add columns and rows
        for (int c = 0; c < columns; c++)
            grid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
        for (int r = 0; r < rows; r++)
            grid.RowsProportions.Add(new Proportion(ProportionType.Auto));

        // Add chest icons
        for (int i = 0; i < lootBoxes.Count; i++)
        {
            var box = lootBoxes[i];
            var row = i / columns;
            var col = i % columns;

            var iconColor = isCurrentEncounter ? Color.White : new Color(150, 150, 150);

            var icon = new Image
            {
                Background = new TextureRegion(box.Icon),
                Width = iconSize,
                Height = iconSize,
                Color = iconColor
            };

            Grid.SetRow(icon, row);
            Grid.SetColumn(icon, col);
            grid.Widgets.Add(icon);
        }

        return grid;
    }
}

