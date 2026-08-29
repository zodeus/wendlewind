using System.Text.RegularExpressions;

namespace Wendlewind.Scenes.MainGameScene.Gui.Widgets.MiscWidgets.Boak;

internal class BoakPawnsBodyPartsPanel : VerticalStackPanel
{
    private static readonly Regex PrefixRegex = new(@"^([A-Z][a-z]+)");
    
    private static readonly Color GoldColor = new(232, 170, 0);
    private static readonly Color DimColor = new(120, 120, 120);
    private static readonly Color GreenColor = new(100, 200, 100);
    private static readonly Color WarningColor = new(200, 160, 100);
    private static readonly Color HeaderBgColor = new(45, 40, 50);

    public BoakPawnsBodyPartsPanel(IReadOnlyList<BodyPartDef> partDefs, IReadOnlyList<BodyPartSocketDef> socketDefs)
    {
        Spacing = 4;
        Padding = new Thickness(12);

        // Group by moniker prefix
        var grouped = partDefs
            .GroupBy(ExtractPrefix)
            .OrderBy(g => g.Key)
            .ToList();

        foreach (var group in grouped)
        {
            // Group header with background
            var headerPanel = new HorizontalStackPanel
            {
                Spacing = 8,
                Margin = new Thickness(0, 12, 0, 4),
                Widgets =
                {
                    new Label(BaseContent.Styles.Label.Medium)
                    {
                        Text = $"▸ {group.Key}",
                        TextColor = GoldColor
                    },
                    new Label(BaseContent.Styles.Label.Small)
                    {
                        Text = $"({group.Count()})",
                        TextColor = DimColor,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                }
            };
            Widgets.Add(headerPanel);

            // Create compact grid for this group
            var grid = CreateGroupGrid(group.ToList());
            Widgets.Add(grid);
        }
    }

    private Grid CreateGroupGrid(List<BodyPartDef> parts)
    {
        var grid = new Grid
        {
            RowSpacing = 1,
            ColumnSpacing = 10,
            DefaultColumnProportion = Proportion.Auto
        };

        // Header row
        var headers = new[] { "", "Name", "HP", "Type", "HitW", "V", "O", "Subst", "Mob", "Slots", "Sockets" };
        for (int col = 0; col < headers.Length; col++)
        {
            var label = new Label(BaseContent.Styles.Label.Small)
            {
                Text = headers[col],
                TextColor = DimColor,
                Padding = new Thickness(0, 2, 0, 2)
            };
            Grid.SetRow(label, 0);
            Grid.SetColumn(label, col);
            grid.Widgets.Add(label);
        }

        // Data rows - ordered by Substance name
        int row = 1;
        foreach (var def in parts.OrderBy(p => p.Substance.ToString()))
        {
            int col = 0;

            // Icon (compact 24x24)
            var iconPanel = new Panel
            {
                Width = 24,
                Height = 24,
                Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.DeepGold],
                Padding = new Thickness(1),
                VerticalAlignment = VerticalAlignment.Center,
                Widgets = { new Image { Width = 22, Height = 22, Background = new TextureRegion(def.Icon) } }
            };
            AddCell(grid, iconPanel, row, col++);

            // Name (without prefix for density)
            var shortName = StripPrefix(def.Moniker);
            AddCell(grid, CreateLabel(shortName), row, col++);

            // HP
            var hp = def.BaseStats.FirstOrNull(s => s?.Def == Defs.Stats.MaxHitPoints)?.Value ?? 0;
            AddCell(grid, CreateLabel($"{hp:N0}"), row, col++);

            // Type (abbreviated)
            AddCell(grid, CreateLabel(FormatPartType(def.BodyPartType)), row, col++);

            // Hit Weight
            AddCell(grid, CreateLabel($"{def.HitWeight:N0}"), row, col++);

            // IsVital (compact)
            AddCell(grid, CreateBoolLabel(def.IsVital), row, col++);

            // IsOrgan (compact)
            AddCell(grid, CreateBoolLabel(def.IsOrgan), row, col++);

            // Substance (abbreviated)
            AddCell(grid, CreateLabel(FormatSubstance(def.Substance)), row, col++);

            // Mobility %
            var mobText = def.MobilityFraction > 0 ? $"{def.MobilityFraction:P0}" : "-";
            AddCell(grid, CreateLabel(mobText, def.MobilityFraction > 0 ? WarningColor : DimColor), row, col++);

            // Equipment Slots (abbreviated list)
            var slotText = FormatSlots(def.EquipmentSlots);
            AddCell(grid, CreateLabel(slotText, def.EquipmentSlots?.Count > 0 ? Color.White : DimColor), row, col++);

            // Sockets (abbreviated)
            var socketText = FormatSockets(def.Sockets);
            AddCell(grid, CreateLabel(socketText, def.Sockets?.Count > 0 ? Color.White : DimColor), row, col++);

            row++;
        }

        return grid;
    }

    private static string ExtractPrefix(BodyPartDef def)
    {
        var match = PrefixRegex.Match(def.Moniker);
        return match.Success ? match.Groups[1].Value : "Other";
    }

    private static string StripPrefix(string moniker)
    {
        var match = PrefixRegex.Match(moniker);
        if (match.Success)
        {
            var prefix = match.Groups[1].Value;
            var remainder = moniker[prefix.Length..];
            return remainder.Length > 0 ? remainder : moniker;
        }
        return moniker;
    }

    private static string FormatPartType(BodyPartType type)
    {
        return type switch
        {
            BodyPartType.Undefined => "-",
            BodyPartType.Head => "Head",
            BodyPartType.Torso => "Torso",
            BodyPartType.Minion => "Minion",
            BodyPartType.Antenna => "Ant",
            _ => type.ToString().Length > 6 ? type.ToString()[..6] : type.ToString()
        };
    }

    private static string FormatSubstance(SubstanceType type)
    {
        return type switch
        {
            SubstanceType.Undefined => "-",
            SubstanceType.Flesh => "Flesh",
            SubstanceType.Bone => "Bone",
            SubstanceType.Chitin => "Chitn",
            SubstanceType.Wood => "Wood",
            SubstanceType.Stone => "Stone",
            SubstanceType.Metal => "Metal",
            _ => type.ToString().Length > 5 ? type.ToString()[..5] : type.ToString()
        };
    }

    private static string FormatSlots(List<EquipmentSlotType>? slots)
    {
        if (slots == null || slots.Count == 0) return "-";
        if (slots.Count == 1) return slots[0].ToString();
        return $"{slots.Count}×";
    }

    private static string FormatSockets(List<BodyPartSocketDef>? sockets)
    {
        if (sockets == null || sockets.Count == 0) return "-";
        if (sockets.Count <= 2)
        {
            return string.Join(", ", sockets.Select(s => AbbreviateSocketName(s.Label)));
        }
        return $"{sockets.Count}×";
    }

    private static string AbbreviateSocketName(string name)
    {
        // Remove common suffixes for brevity
        return name
            .Replace(" Socket", "")
            .Replace("Socket", "");
    }

    private static Label CreateLabel(string text, Color? color = null)
    {
        return new Label(BaseContent.Styles.Label.Small)
        {
            Text = text,
            TextColor = color ?? Color.White,
            VerticalAlignment = VerticalAlignment.Center,
            Padding = new Thickness(0, 1, 0, 1)
        };
    }

    private static Label CreateBoolLabel(bool value)
    {
        return new Label(BaseContent.Styles.Label.Small)
        {
            Text = value ? "●" : "-",
            TextColor = value ? GreenColor : DimColor,
            VerticalAlignment = VerticalAlignment.Center,
            Padding = new Thickness(0, 1, 0, 1)
        };
    }

    private static void AddCell(Grid grid, Widget widget, int row, int column)
    {
        Grid.SetRow(widget, row);
        Grid.SetColumn(widget, column);
        grid.Widgets.Add(widget);
    }
}
