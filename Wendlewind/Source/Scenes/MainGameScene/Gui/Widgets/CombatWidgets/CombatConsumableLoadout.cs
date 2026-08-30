namespace Wendlewind.Scenes.MainGameScene.Gui.Widgets.CombatWidgets;

internal sealed class CombatConsumableLoadout : Panel, IUpdatable
{
    private const int MedicalSlots = 5;
    private const int FoodSlots = 4;
    private const int IncenseSlots = 3;
    private const int ColumnWidth = 180;
    private const int CellSpacing = 5;
    private const int CellSize = (ColumnWidth - CellSpacing * 2) / 3;
    private const int CellPad = 3;
    private const int IconSize = CellSize - CellPad * 2;

    public readonly Pawn Pawn;
    private readonly MedicalBar _medicalBar;

    public CombatConsumableLoadout(ZoneGui gui, Pawn pawn, bool mirror = false)
    {
        Pawn = pawn;
        Width = ColumnWidth;
        HorizontalAlignment = HorizontalAlignment.Stretch;

        _medicalBar = new MedicalBar(pawn, item => gui.ViewEntity(item), IconSize);
        Widgets.Add(BuildGrid(gui, pawn, mirror));
    }

    public void NotifyMedicalUsed(string? itemMoniker)
    {
        _medicalBar.NotifyUsed(itemMoniker);
    }

    public void Update()
    {
        _medicalBar.Update();
    }

    private Widget BuildGrid(ZoneGui gui, Pawn pawn, bool mirror)
    {
        var grid = new Grid
        {
            ColumnSpacing = CellSpacing,
            RowSpacing = CellSpacing,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Top
        };
        for (var i = 0; i < 3; i++)
        {
            grid.ColumnsProportions.Add(new Proportion(ProportionType.Pixels, CellSize));
        }

        for (var i = 0; i < MedicalSlots; i++)
        {
            grid.RowsProportions.Add(new Proportion(ProportionType.Pixels, CellSize));
        }

        var medicalButtons = _medicalBar.Widgets.ToList();
        foreach (var button in medicalButtons)
        {
            button.RemoveFromParent();
        }

        var medicalCol = mirror ? 2 : 0;
        var foodCol = 1;
        var incenseCol = mirror ? 0 : 2;

        for (var i = 0; i < MedicalSlots; i++)
        {
            Place(grid, i < medicalButtons.Count ? Cell(medicalButtons[i]) : EmptyCell(), i, medicalCol);
        }

        var food = pawn.MealPlan.Items;
        for (var i = 0; i < FoodSlots; i++)
        {
            if (i < food.Count && food[i] is { IsDestroyed: false } meal)
            {
                var item = meal;
                var icon = ItemIcon(item);
                icon.TouchDown += (_, _) => gui.ViewEntity(item);
                icon.WithTooltip(() => FoodTooltip(item));
                Place(grid, Cell(icon), i, foodCol);
            }
            else
            {
                Place(grid, EmptyCell(), i, foodCol);
            }
        }

        var incense = pawn.ActiveIncense;
        for (var i = 0; i < IncenseSlots; i++)
        {
            var cell = i < incense.Count
                ? Cell(CreateIncenseIcon(incense[i]))
                : EmptyCell();
            Place(grid, cell, i, incenseCol);
        }

        return grid;
    }

    private static void Place(Grid grid, Widget cell, int row, int column)
    {
        Grid.SetRow(cell, row);
        Grid.SetColumn(cell, column);
        grid.Widgets.Add(cell);
    }

    private static Widget CreateIncenseIcon(ActiveIncense incense)
    {
        var itemDef = incense.SourceMoniker != null
            ? DefRepository<ItemDef>.GetByMoniker(incense.SourceMoniker, raiseError: false)
            : null;
        var icon = new Image
        {
            Width = IconSize,
            Height = IconSize,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        if (itemDef != null)
        {
            icon.Background = itemDef.GetIconImage();
        }
        else if (incense.Def != null)
        {
            icon.Background = new TextureRegion(incense.Def.GetTexture());
        }

        var name = incense.Def?.Label ?? itemDef?.Label ?? "Incense";
        var left = incense.EncountersRemaining;
        icon.WithTooltip(name, left == 1 ? "1 battle left" : $"{left} battles left");
        return icon;
    }

    private static Widget FoodTooltip(Item item)
    {
        var container = new VerticalStackPanel { Spacing = 4, Padding = new Thickness(4) };
        container.Widgets.Add(new Label(BaseContent.Styles.Label.Normal)
        {
            Text = item.Label,
            TextColor = Color.Gold
        });

        var description = item.Def.Description;
        if (!string.IsNullOrWhiteSpace(description))
        {
            container.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
            {
                Text = description,
                Wrap = true,
                MaxWidth = 280,
                TextColor = new Color(200, 200, 200)
            });
        }

        var nutrition = item.GetStatValue(Defs.Stats.NutritionalValue);
        container.Widgets.Add(new HorizontalStackPanel
        {
            Spacing = 6,
            Widgets =
            {
                new Label(BaseContent.Styles.Label.Small) { Text = "Nutrition:", TextColor = new Color(180, 180, 180) },
                new Label(BaseContent.Styles.Label.Small)
                {
                    Text = $"{nutrition:0.##}",
                    TextColor = FoodProperties.GetNutritionColor(nutrition)
                }
            }
        });

        var effects = item.ItemDef.FoodProperties?.Effects;
        if (effects is { Count: > 0 })
        {
            container.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
            {
                Text = "Effects:",
                TextColor = new Color(220, 180, 100),
                Margin = new Thickness(0, 4, 0, 2)
            });

            foreach (var effect in effects)
            {
                var row = new HorizontalStackPanel
                {
                    Spacing = 6,
                    Margin = new Thickness(8, 0, 0, 0)
                };
                row.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
                {
                    Text = effect.Def.Label,
                    TextColor = FoodProperties.GetEffectColor(effect.Def)
                });

                container.Widgets.Add(row);
            }
        }

        return container;
    }

    private static CursorButton ItemIcon(Item item)
    {
        return new CursorButton
        {
            Width = IconSize,
            Height = IconSize,
            Padding = new Thickness(0),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Content = new Image
            {
                Background = item.GetIconImage(),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            }
        };
    }

    private static Panel Cell(Widget content)
    {
        return new Panel
        {
            Width = CellSize,
            Height = CellSize,
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.IconFrame],
            Padding = new Thickness(CellPad),
            Widgets = { content }
        };
    }

    private static Panel EmptyCell()
    {
        return Cell(new Panel());
    }
}
