using SolidBrush = Myra.Graphics2D.Brushes.SolidBrush;

namespace Grafted.Scenes.MainGameScene.Gui.Widgets.MiscWidgets.Boak;

internal sealed class BoakItemsWeaponPanel : Panel
{
    private static readonly Color HeaderBgColor = new(42, 32, 35);
    private static readonly Color RowEvenColor = new(30, 26, 28);
    private static readonly Color RowOddColor = new(42, 35, 38);
    private static readonly Color AccentColor = new(220, 80, 80);
    private static readonly Color GoldColor = new(232, 170, 0);
    private static readonly Color MutedTextColor = new(160, 160, 170);
    private static readonly Color BorderColor = new(70, 55, 58);
    private static readonly Color FilterBarBgColor = new(25, 20, 22);
    private static readonly Color ToggleActiveColor = new(80, 180, 120);

    private readonly IReadOnlyList<ItemDef> _allDefs;
    private readonly ScrollViewer _scrollViewer;
    private bool _hideBuiltIn = true;

    public BoakItemsWeaponPanel(IReadOnlyList<ItemDef> defs, IReadOnlyList<WeaponManeuverDef> toolManeuverDefs)
    {
        _allDefs = defs.OrderBy(d => d.BaseStats.FirstOrNull(s => s?.Def == Defs.Stats.MeleePower)?.Value).ToList();

        var rootContainer = new VerticalStackPanel { Spacing = 0 };

        // Filter bar
        var filterBar = CreateFilterBar();
        rootContainer.Widgets.Add(filterBar);

        // Scroll viewer for the table content
        _scrollViewer = new ScrollViewer();
        rootContainer.Widgets.Add(_scrollViewer);

        Widgets.Add(rootContainer);

        // Build initial content
        RebuildTable();
    }

    private Panel CreateFilterBar()
    {
        var filterBar = new Panel
        {
            Background = new SolidBrush(FilterBarBgColor),
            Padding = new Thickness(16, 10, 16, 10)
        };

        var filterStack = new HorizontalStackPanel
        {
            Spacing = 16,
            VerticalAlignment = VerticalAlignment.Center
        };

        // "Hide Built-In" toggle
        var builtInToggle = new CheckButton
        {
            IsChecked = _hideBuiltIn,
            VerticalAlignment = VerticalAlignment.Center
        };

        var toggleLabel = new Label(BaseContent.Styles.Label.Small)
        {
            Text = "Hide Built-In",
            TextColor = _hideBuiltIn ? ToggleActiveColor : MutedTextColor,
            VerticalAlignment = VerticalAlignment.Center
        };

        builtInToggle.Click += (_, _) =>
        {
            _hideBuiltIn = builtInToggle.IsChecked;
            toggleLabel.TextColor = _hideBuiltIn ? ToggleActiveColor : MutedTextColor;
            RebuildTable();
        };

        toggleLabel.TouchDown += (_, _) =>
        {
            builtInToggle.IsChecked = !builtInToggle.IsChecked;
            _hideBuiltIn = builtInToggle.IsChecked;
            toggleLabel.TextColor = _hideBuiltIn ? ToggleActiveColor : MutedTextColor;
            RebuildTable();
        };

        var togglePanel = new HorizontalStackPanel
        {
            Spacing = 6,
            VerticalAlignment = VerticalAlignment.Center,
            Widgets = { builtInToggle, toggleLabel }
        };
        filterStack.Widgets.Add(togglePanel);

        // Info label
        var infoLabel = new Label(BaseContent.Styles.Label.Small)
        {
            Text = "(Built-in = natural weapons like claws, teeth, fists)",
            TextColor = new Color(90, 85, 88),
            VerticalAlignment = VerticalAlignment.Center
        };
        filterStack.Widgets.Add(infoLabel);

        filterBar.Widgets.Add(filterStack);
        return filterBar;
    }

    private void RebuildTable()
    {
        var filteredDefs = _hideBuiltIn
            ? _allDefs.Where(d => d.EquipmentProperties?.SlotUsedToEquip != EquipmentSlotType.BuiltIn).ToList()
            : _allDefs.ToList();

        var mainContainer = new VerticalStackPanel { Spacing = 0 };

        // Header Row
        var headerGrid = CreateRow(isHeader: true, rowIndex: 0);
        AddHeaderCell(headerGrid, "Weapon", 0);
        AddHeaderCell(headerGrid, "Type", 1);
        AddHeaderCell(headerGrid, "Damage", 2);
        AddHeaderCell(headerGrid, "Durability", 3);
        AddHeaderCell(headerGrid, "Maneuvers", 4);
        AddHeaderCell(headerGrid, "Modifiers", 5);
        mainContainer.Widgets.Add(headerGrid);

        // Divider
        mainContainer.Widgets.Add(CreateDivider());

        // Data Rows
        var rowIndex = 0;
        foreach (var def in filteredDefs)
        {
            var rowGrid = CreateRow(isHeader: false, rowIndex);

            // Weapon column with icon and name
            var itemCell = new HorizontalStackPanel
            {
                Spacing = 12,
                VerticalAlignment = VerticalAlignment.Center
            };

            var iconContainer = new Panel
            {
                Width = 56,
                Height = 56,
                Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrame],
                Padding = new Thickness(4)
            };
            iconContainer.Widgets.Add(new Image
            {
                Width = 48,
                Height = 48,
                Background = new TextureRegion(def.Icon),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            });
            itemCell.Widgets.Add(iconContainer);

            itemCell.Widgets.Add(new Label(BaseContent.Styles.Label.Normal)
            {
                Text = def.Moniker ?? def.Label,
                VerticalAlignment = VerticalAlignment.Center,
                TextColor = GoldColor
            });
            AddDataCell(rowGrid, itemCell, 0);

            // Damage Type with icon-like styling
            var damageType = def.WeaponProperties?.DamageType.ToString() ?? "—";
            var damageTypeLabel = new Label(BaseContent.Styles.Label.Small)
            {
                Text = damageType,
                TextColor = GetDamageTypeColor(damageType),
                VerticalAlignment = VerticalAlignment.Center,
                Background = new SolidBrush(new Color(10,10,10)),
                Padding = new Thickness(8, 4, 8, 4)
            };
            var damageTypeContainer = new Panel
            {
                VerticalAlignment = VerticalAlignment.Center,
                Widgets = { damageTypeLabel }
            };
            AddDataCell(rowGrid, damageTypeContainer, 1);

            // Damage
            var damage = def.BaseStats.FirstOrNull(s => s?.Def == Defs.Stats.MeleePower)?.Value;
            AddDataCell(rowGrid, CreateValueLabel(damage?.ToString() ?? "—", GetDamageColor(damage)), 2);

            // Durability
            var durability = def.BaseStats.FirstOrNull(s => s?.Def == Defs.Stats.MaxDurability)?.Value;
            AddDataCell(rowGrid, CreateValueLabel(durability?.ToString() ?? "—"), 3);

            // Maneuvers - styled as tags
            var maneuvers = def.WeaponProperties?.WeaponManeuvers?.Select(f => f.Label).ToList() ?? [];
            if (maneuvers.Count > 0)
            {
                var maneuverStack = new HorizontalStackPanel
                {
                    Spacing = 6,
                    VerticalAlignment = VerticalAlignment.Center
                };
                foreach (var maneuver in maneuvers.Take(3)) // Limit to 3 to prevent overflow
                {
                    maneuverStack.Widgets.Add(CreateTag(maneuver, new Color(80, 140, 200)));
                }
                if (maneuvers.Count > 3)
                {
                    maneuverStack.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
                    {
                        Text = $"+{maneuvers.Count - 3}",
                        TextColor = MutedTextColor,
                        VerticalAlignment = VerticalAlignment.Center
                    });
                }
                AddDataCell(rowGrid, maneuverStack, 4);
            }
            else
            {
                AddDataCell(rowGrid, CreateValueLabel("—", MutedTextColor), 4);
            }

            // Modifiers
            var modifiers = def.WeaponProperties?.BodyPartModifiers?.Select(f => f.Def.Label).ToList() ?? [];
            var modText = modifiers.Count > 0 ? string.Join(", ", modifiers) : "—";
            AddDataCell(rowGrid, CreateValueLabel(modText, modifiers.Count > 0 ? Color.White : MutedTextColor), 5);

            mainContainer.Widgets.Add(rowGrid);

            // Subtle divider between rows
            if (rowIndex < filteredDefs.Count - 1)
            {
                mainContainer.Widgets.Add(CreateRowDivider());
            }

            rowIndex++;
        }

        _scrollViewer.Content = mainContainer;
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

        grid.ColumnsProportions.Add(new Proportion(ProportionType.Pixels, 350)); // Weapon
        grid.ColumnsProportions.Add(new Proportion(ProportionType.Pixels, 120)); // Type
        grid.ColumnsProportions.Add(new Proportion(ProportionType.Pixels, 90));  // Damage
        grid.ColumnsProportions.Add(new Proportion(ProportionType.Pixels, 100)); // Durability
        grid.ColumnsProportions.Add(new Proportion(ProportionType.Pixels, 200)); // Maneuvers
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
        return new Label(BaseContent.Styles.Label.Small)
        {
            Text = text,
            TextColor = color ?? Color.White,
            VerticalAlignment = VerticalAlignment.Center
        };
    }

    private static Panel CreateTag(string text, Color bgColor)
    {
        var label = new Label(BaseContent.Styles.Label.Small)
        {
            Text = text,
            TextColor = Color.White,
            Padding = new Thickness(6, 2, 6, 2)
        };
        return new Panel
        {
            Background = new SolidBrush(new Color(20,20,30)),
            VerticalAlignment = VerticalAlignment.Center,
            Widgets = { label }
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

    private static Color GetDamageTypeColor(string? type)
    {
        return type?.ToLowerInvariant() switch
        {
            "slash" or "slashing" => new Color(220, 100, 100),
            "blunt" or "bludgeoning" => new Color(180, 140, 100),
            "pierce" or "piercing" => new Color(100, 180, 220),
            "fire" => new Color(255, 140, 60),
            "cold" or "ice" => new Color(140, 200, 255),
            "poison" => new Color(140, 220, 100),
            _ => new Color(200, 200, 200)
        };
    }

    private static Color GetDamageColor(float? value)
    {
        if (value == null) return new Color(160, 160, 170);
        return value switch
        {
            >= 40 => new Color(255, 100, 100),   // High - bright red
            >= 25 => new Color(255, 180, 100),   // Medium-high - orange
            >= 15 => new Color(255, 220, 100),   // Medium - yellow
            _ => new Color(200, 200, 200)        // Low - white
        };
    }
}
