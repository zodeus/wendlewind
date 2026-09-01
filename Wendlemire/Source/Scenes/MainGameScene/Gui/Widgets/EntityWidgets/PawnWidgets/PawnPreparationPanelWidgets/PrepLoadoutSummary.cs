using Image = Myra.Graphics2D.UI.Image;

namespace Wendlemire.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets.PawnPreparationPanelWidgets;

internal sealed class PrepLoadoutSummary : VerticalStackPanel, IUpdatable
{
    private const int CellSize = 32;
    private const int CellPad = 3;
    private const int CellSpacing = 4;
    private const int DefaultIconsPerRow = 6;

    private readonly BaseGui _gui;
    private readonly Pawn _pawn;
    private string _signature = "";
    private int _iconsPerRow = DefaultIconsPerRow;

    public PrepLoadoutSummary(BaseGui gui, Pawn pawn)
    {
        _gui = gui;
        _pawn = pawn;
        Spacing = 6;
        HorizontalAlignment = HorizontalAlignment.Stretch;
        Rebuild();
    }

    public void Update()
    {
        var perRow = IconsPerRow();
        var signature = Signature();
        if (perRow == _iconsPerRow && signature == _signature)
        {
            return;
        }

        Rebuild();
    }

    private void Rebuild()
    {
        _iconsPerRow = IconsPerRow();
        _signature = Signature();
        Widgets.Clear();

        AddRow("Gear", GearCells());
        AddRow("Food", FoodCells());
        AddRow("Incense", IncenseCells());
        AddRow("Potions", PotionCells());
        AddRow("Medical", MedicalCells());

        Visible = Widgets.Count > 0;
    }

    private void AddRow(string title, List<Widget> cells)
    {
        if (cells.Count == 0)
        {
            return;
        }

        var wrap = new VerticalStackPanel { Spacing = CellSpacing };
        HorizontalStackPanel? line = null;
        for (var i = 0; i < cells.Count; i++)
        {
            if (i % _iconsPerRow == 0)
            {
                line = new HorizontalStackPanel { Spacing = CellSpacing };
                wrap.Widgets.Add(line);
            }

            line!.Widgets.Add(cells[i]);
        }

        Widgets.Add(new VerticalStackPanel
        {
            Spacing = 2,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Widgets =
            {
                new Label(BaseContent.Styles.Label.Small)
                {
                    Text = title,
                    TextColor = new Color(160, 160, 160)
                },
                wrap
            }
        });
    }

    private List<Widget> GearCells()
    {
        var cells = new List<Widget>();
        foreach (var item in _pawn.Equipment)
        {
            if (item == null || item.IsDestroyed || IsBuiltin(item) || item.ItemDef.ItemType == ItemType.Potion)
            {
                continue;
            }

            cells.Add(IconCell(item.GetIconImage(), item.Label, item.Def.Description, () => Inspect(item)));
        }

        return cells;
    }

    private List<Widget> FoodCells()
    {
        return _pawn.MealPlan.Items
            .Where(item => item is { IsDestroyed: false })
            .Select(item => IconCell(item.GetIconImage(), item.Label, item.Def.Description, () => Inspect(item)))
            .ToList();
    }

    private List<Widget> IncenseCells()
    {
        var cells = new List<Widget>();
        foreach (var incense in _pawn.ActiveIncense)
        {
            var itemDef = incense.SourceMoniker != null
                ? DefRepository<ItemDef>.GetByMoniker(incense.SourceMoniker, raiseError: false)
                : null;
            var name = incense.Def?.Label ?? itemDef?.Label ?? incense.SourceMoniker ?? "Incense";
            IImage? icon = itemDef != null
                ? itemDef.GetIconImage()
                : incense.Def != null
                    ? new TextureRegion(incense.Def.GetTexture())
                    : null;
            Action? inspect = itemDef != null ? () => Inspect(itemDef) : null;
            cells.Add(IconCell(icon, name, "Burns until extinguished", inspect));
        }

        return cells;
    }

    private List<Widget> PotionCells()
    {
        return _pawn.Equipment.Potions
            .Where(potion => potion is { IsDestroyed: false })
            .Select(potion =>
            {
                var trigger = potion.PotionTrigger?.Describe()
                              ?? potion.ItemDef.PotionProperties?.DefaultTrigger?.Describe()
                              ?? "No trigger";
                return IconCell(potion.GetIconImage(), potion.Label, trigger, () => Inspect(potion));
            })
            .ToList();
    }

    private List<Widget> MedicalCells()
    {
        var cells = new List<Widget>();
        foreach (var slot in _pawn.MedicalChest.Slots)
        {
            if (slot.Def == null)
            {
                continue;
            }

            var overlay = slot.IsInfinite ? "∞" : slot.Charges.ToString();
            cells.Add(IconCell(
                slot.Def.GetIconImage(),
                slot.Def.Label,
                slot.Trigger.Describe(),
                () => Inspect(slot.Def),
                overlay));
        }

        return cells;
    }

    private Widget IconCell(IImage? icon, string title, string? description, Action? inspect, string? overlay = null)
    {
        var content = new Panel
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };
        content.Widgets.Add(new Image
        {
            Background = icon,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        });
        if (!string.IsNullOrEmpty(overlay))
        {
            content.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
            {
                Text = overlay,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom,
                TextColor = new Color(220, 180, 140)
            });
        }

        var button = new CursorButton
        {
            Width = CellSize - CellPad * 2,
            Height = CellSize - CellPad * 2,
            Padding = new Thickness(0),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Content = content
        };
        if (inspect != null)
        {
            button.Click += (_, _) => inspect();
            button.TouchDown += (_, _) =>
            {
                if (Mouse.GetState().RightButton == ButtonState.Pressed)
                {
                    inspect();
                }
            };
        }

        button.WithTooltip(title, description);
        return new Panel
        {
            Width = CellSize,
            Height = CellSize,
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.IconFrame],
            Padding = new Thickness(CellPad),
            Widgets = { button }
        };
    }

    private void Inspect(Item item)
    {
        if (!item.IsDestroyed)
        {
            _gui.ViewEntity(item);
        }
    }

    private void Inspect(ItemDef def)
    {
        var live = _pawn.Inventory.FirstOrDefault(i => i.Def == def && !i.IsDestroyed);
        if (live != null)
        {
            _gui.ViewEntity(live);
            return;
        }

        _gui.ViewEntity(_pawn.Context.Factory.CreateEntity<Item>(def, 1));
    }

    private static bool IsBuiltin(Item item)
    {
        return item.ItemDef.EquipmentProperties?.SlotUsedToEquip == EquipmentSlotType.BuiltIn;
    }

    private int IconsPerRow()
    {
        var width = Math.Max(ActualBounds.Width, Bounds.Width);
        if (width <= 0)
        {
            return DefaultIconsPerRow;
        }

        return Math.Max(1, (width + CellSpacing) / (CellSize + CellSpacing));
    }

    private string Signature()
    {
        var gear = string.Join(",", _pawn.Equipment
            .Where(i => i != null && !i.IsDestroyed && !IsBuiltin(i) && i.ItemDef.ItemType != ItemType.Potion)
            .Select(i => i.Id));
        var food = string.Join(",", _pawn.MealPlan.Items.Select(i => i?.Id ?? -1));
        var incense = string.Join(",", _pawn.ActiveIncense.Select(a =>
            $"{a.Def?.Moniker ?? a.SourceMoniker}:{a.EncountersRemaining}"));
        var potions = string.Join(",", _pawn.Equipment.Potions
            .Where(p => p is { IsDestroyed: false })
            .Select(p => $"{p.Id}:{p.PotionTrigger?.Describe()}"));
        var medical = string.Join(",", _pawn.MedicalChest.Slots.Select(s =>
            $"{s.Def?.Moniker}:{s.Charges}:{s.IsInfinite}:{s.Trigger.Describe()}"));
        return $"{gear}|{food}|{incense}|{potions}|{medical}";
    }
}
