using SolidBrush = Myra.Graphics2D.Brushes.SolidBrush;

namespace Wendlewind.Scenes.MainGameScene.Gui.Widgets.MiscWidgets.Boak;

internal sealed class BoakItemsArmorPanel : ScrollViewer
{
    private static readonly Color HeaderBgColor = new(35, 32, 42);
    private static readonly Color RowEvenColor = new(28, 26, 34);
    private static readonly Color RowOddColor = new(38, 35, 46);
    private static readonly Color AccentColor = new(232, 170, 0);
    private static readonly Color MutedTextColor = new(160, 160, 170);
    private static readonly Color BorderColor = new(60, 55, 70);

    public BoakItemsArmorPanel(IReadOnlyList<ItemDef> defs)
    {
        defs = defs.OrderBy(d => d.BaseStats.FirstOrNull(s => s?.Def == Defs.Stats.PhysicalResistance)?.Value).ToList();

        var mainContainer = new VerticalStackPanel { Spacing = 0 };

        // Header Row
        var headerGrid = CreateRow(isHeader: true, rowIndex: 0);
        AddHeaderCell(headerGrid, "Item", 0);
        AddHeaderCell(headerGrid, "Phys. Res", 1);
        AddHeaderCell(headerGrid, "Durability", 2);
        AddHeaderCell(headerGrid, "Max Ench", 3);
        AddHeaderCell(headerGrid, "Slot", 4);
        AddHeaderCell(headerGrid, "Modifiers", 5);
        mainContainer.Widgets.Add(headerGrid);

        // Divider
        mainContainer.Widgets.Add(CreateDivider());

        // Data Rows
        var rowIndex = 0;
        foreach (var def in defs)
        {
            var rowGrid = CreateRow(isHeader: false, rowIndex);

            // Item column with icon and name
            var itemCell = new HorizontalStackPanel
            {
                Spacing = 12,
                VerticalAlignment = VerticalAlignment.Center
            };

            var iconContainer = new Panel
            {
                Width = 56,
                Height = 56,
                Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.RoundDark64],
                Padding = new Thickness(4)
            };
            iconContainer.Widgets.Add(new Image
            {
                Margin = new Thickness(6),
                Width = 48,
                Height = 48,
                Background = new TextureRegion(def.Icon),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            });
            itemCell.Widgets.Add(iconContainer);

            itemCell.Widgets.Add(new Label(BaseContent.Styles.Label.Normal)
            {
                Text = def.Label,
                VerticalAlignment = VerticalAlignment.Center,
                TextColor = AccentColor
            });
            AddDataCell(rowGrid, itemCell, 0);

            // Physical Resistance
            var physRes = def.BaseStats.FirstOrNull(s => s?.Def == Defs.Stats.PhysicalResistance)?.Value;
            AddDataCell(rowGrid, CreateValueLabel(physRes?.ToString() ?? "—", GetResistanceColor(physRes)), 1);

            // Durability
            var durability = def.BaseStats.FirstOrNull(s => s?.Def == Defs.Stats.MaxDurability)?.Value;
            AddDataCell(rowGrid, CreateValueLabel(durability?.ToString() ?? "—"), 2);

            // Max Enchantments
            var maxEnchantments = def.EquipmentProperties?.MaxEnchantments ?? 0;
            AddDataCell(rowGrid, CreateValueLabel(maxEnchantments > 0 ? maxEnchantments.ToString() : "—", MutedTextColor), 3);

            // Slot
            var slotText = def.EquipmentProperties?.SlotUsedToEquip.ToString() ?? "—";
            AddDataCell(rowGrid, CreateValueLabel(slotText, MutedTextColor), 4);

            // Modifiers
            var modifiers = def.WeaponProperties?.BodyPartModifiers.Select(f => f.Def.Label) ?? [];
            var modText = string.Join(", ", modifiers);
            if (string.IsNullOrEmpty(modText)) modText = "—";
            AddDataCell(rowGrid, CreateValueLabel(modText, MutedTextColor), 5);

            mainContainer.Widgets.Add(rowGrid);

            // Subtle divider between rows
            if (rowIndex < defs.Count - 1)
            {
                mainContainer.Widgets.Add(CreateRowDivider());
            }

            rowIndex++;
        }

        Content = mainContainer;
    }

    private static Grid CreateRow(bool isHeader, int rowIndex)
    {
        var grid = new Grid
        {
            Padding = new Thickness(16, isHeader ? 14 : 10, 16, isHeader ? 14 : 10)
        };

        if (isHeader)
        {
            grid.Background = new SolidBrush(HeaderBgColor);
        }
        else
        {
            grid.Background = new SolidBrush(rowIndex % 2 == 0 ? RowEvenColor : RowOddColor);
        }

        grid.ColumnsProportions.Add(new Proportion(ProportionType.Pixels, 350)); // Item
        grid.ColumnsProportions.Add(new Proportion(ProportionType.Pixels, 120)); // Phys Res
        grid.ColumnsProportions.Add(new Proportion(ProportionType.Pixels, 100)); // Durability
        grid.ColumnsProportions.Add(new Proportion(ProportionType.Pixels, 90));  // Max Ench
        grid.ColumnsProportions.Add(new Proportion(ProportionType.Pixels, 140)); // Slot
        grid.ColumnsProportions.Add(new Proportion(ProportionType.Fill));        // Modifiers

        return grid;
    }

    private static void AddHeaderCell(Grid grid, string text, int column)
    {
        var label = new Label(BaseContent.Styles.Label.Small)
        {
            Text = text.ToUpperInvariant(),
            TextColor = AccentColor,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(label, column);
        grid.Widgets.Add(label);
    }

    private static void AddDataCell(Grid grid, Widget widget, int column)
    {
        Grid.SetColumn(widget, column);
        grid.Widgets.Add(widget);
    }

    private static Label CreateValueLabel(string text, Color? color = null)
    {
        return new Label(BaseContent.Styles.Label.Normal)
        {
            Text = text,
            TextColor = color ?? Color.White,
            VerticalAlignment = VerticalAlignment.Center
        };
    }

    private static Panel CreateDivider()
    {
        return new Panel
        {
            Height = 2,
            Background = new SolidBrush(AccentColor),
            Margin = new Thickness(0)
        };
    }

    private static Panel CreateRowDivider()
    {
        return new Panel
        {
            Height = 1,
            Background = new SolidBrush(BorderColor),
            Margin = new Thickness(16, 0, 16, 0)
        };
    }

    private static Color GetResistanceColor(float? value)
    {
        if (value == null) return MutedTextColor;
        return value switch
        {
            >= 50 => new Color(100, 220, 100),  // High - green
            >= 25 => new Color(220, 200, 100),  // Medium - yellow
            _ => new Color(220, 140, 100)        // Low - orange
        };
    }
}
