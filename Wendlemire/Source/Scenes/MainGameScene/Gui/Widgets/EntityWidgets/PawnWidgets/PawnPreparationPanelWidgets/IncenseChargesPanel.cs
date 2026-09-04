using Image = Myra.Graphics2D.UI.Image;

namespace Wendlemire.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets.PawnPreparationPanelWidgets;

public sealed class IncenseChargesPanel : PrepCard, IUpdatable
{
    private readonly Pawn _pawn;
    private readonly Label _igniteLabel;
    private readonly VerticalStackPanel _slotRows;
    private readonly PrepItemGrid _inventory;
    private string _slotSignature = "";
    private int _slotsPerRow = -1;

    public IncenseChargesPanel(BaseGui gui, Pawn pawn) : base("Incense")
    {
        _pawn = pawn;

        _igniteLabel = new Label(BaseContent.Styles.Label.Small)
        {
            Text = IgniteCaption(pawn.IncenseCapacity),
            TextColor = new Color(200, 180, 140)
        };
        Body.Widgets.Add(_igniteLabel);
        Body.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
        {
            Text = "Burns until extinguished",
            TextColor = new Color(160, 160, 160)
        });

        _slotRows = new VerticalStackPanel { Spacing = PrepSlots.Spacing };
        Body.Widgets.Add(_slotRows);

        _inventory = new PrepItemGrid(
            gui,
            pawn.Inventory,
            item => item.ItemDef.IncenseProperties?.Effect != null,
            TryLight,
            LightTooltip,
            isDisabled: IsMuted,
            pagedRow: true);
        SetInventory(_inventory, 64);

        RebuildSlots();
    }

    public void Update()
    {
        _pawn.PruneActiveIncense();
        var perRow = SlotsPerRow();
        var signature = SlotSignature();
        if (perRow != _slotsPerRow || signature != _slotSignature)
        {
            RebuildSlots();
        }

        _inventory.Update();
    }

    private bool IsMuted(Item item)
    {
        return !_pawn.CanLightIncense(item);
    }

    private string LightTooltip(Item item)
    {
        if (_pawn.ActiveIncense.Any(a => a.Def == item.ItemDef.IncenseProperties?.Effect?.Def))
        {
            return "Already lit";
        }

        if (_pawn.CanLightIncense(item))
        {
            return $"Click to light · slot {_pawn.ActiveIncense.Count + 1} at {IncenseProperties.GetIgniteTick(_pawn.ActiveIncense.Count)}";
        }

        return "Unlock more incense slots in later rounds";
    }

    private void TryLight(Item item)
    {
        if (_pawn.TryLightIncense(item))
        {
            RebuildSlots();
        }
    }

    private void RebuildSlots()
    {
        _slotsPerRow = SlotsPerRow();
        _slotSignature = SlotSignature();
        _igniteLabel.Text = IgniteCaption(_pawn.IncenseCapacity);
        _slotRows.Widgets.Clear();

        HorizontalStackPanel? row = null;
        for (var i = 0; i < IncenseProperties.MaxActive; i++)
        {
            if (i % _slotsPerRow == 0)
            {
                row = new HorizontalStackPanel { Spacing = PrepSlots.Spacing };
                _slotRows.Widgets.Add(row);
            }

            var index = i;
            row!.Widgets.Add(index < _pawn.ActiveIncense.Count
                ? FilledSlot(_pawn.ActiveIncense[index], index)
                : index < _pawn.IncenseCapacity
                    ? EmptySlot(index)
                    : LockedSlot(index + 1));
        }
    }

    private Widget FilledSlot(ActiveIncense incense, int index)
    {
        var itemDef = incense.SourceMoniker != null
            ? DefRepository<ItemDef>.GetByMoniker(incense.SourceMoniker, raiseError: false)
            : null;
        var name = incense.Def?.Label ?? itemDef?.Label ?? incense.SourceMoniker ?? "Incense";
        var igniteTick = IncenseProperties.GetIgniteTick(index);

        var icon = new Image
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };
        if (itemDef != null)
        {
            icon.Background = itemDef.GetIconImage();
        }
        else if (incense.Def != null)
        {
            icon.Background = new TextureRegion(incense.Def.GetTexture());
        }

        var button = new CursorButton
        {
            Width = PrepSlots.Size - PrepSlots.Pad * 2,
            Height = PrepSlots.Size - PrepSlots.Pad * 2,
            Padding = new Thickness(0),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Content = icon
        };
        button.Click += (_, _) =>
        {
            _pawn.ExtinguishIncense(index);
            RebuildSlots();
        };
        button.WithTooltip(name, $"Lights at {igniteTick} · Click to extinguish");

        var column = new VerticalStackPanel { Spacing = 2 };
        column.Widgets.Add(PrepSlots.Frame(button));
        column.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
        {
            Text = igniteTick.ToString(),
            TextColor = new Color(200, 180, 140),
            HorizontalAlignment = HorizontalAlignment.Center
        });
        return column;
    }

    private static Widget LockedSlot(int slotNumber)
    {
        var tip = SlotUnlockTooltip.ForSlot(PrepSlotKind.Incense, slotNumber);
        var column = new VerticalStackPanel { Spacing = 2 };
        column.Widgets.Add(LockedSlotChrome.Slot(tip.title, tip.description));
        column.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
        {
            Text = "—",
            TextColor = new Color(80, 76, 70),
            HorizontalAlignment = HorizontalAlignment.Center
        });
        return column;
    }

    private static string IgniteCaption(int capacity)
    {
        return capacity switch
        {
            <= 1 => "Slot lights at 120",
            2 => "Slots light at 120, then 240",
            _ => "Slots light at 120, 240, then 360"
        };
    }

    private static Widget EmptySlot(int index)
    {
        var igniteTick = IncenseProperties.GetIgniteTick(index);
        var empty = new Panel();
        empty.WithTooltip($"Empty slot · lights at {igniteTick}");

        var column = new VerticalStackPanel { Spacing = 2 };
        column.Widgets.Add(PrepSlots.Frame(empty));
        column.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
        {
            Text = igniteTick.ToString(),
            TextColor = new Color(120, 115, 105),
            HorizontalAlignment = HorizontalAlignment.Center
        });
        return column;
    }

    private int SlotsPerRow()
    {
        var width = Math.Max(_slotRows.ActualBounds.Width, _slotRows.Bounds.Width);
        if (width <= 0)
        {
            return Math.Max(1, IncenseProperties.MaxActive);
        }

        return Math.Max(1, (width + PrepSlots.Spacing) / (PrepSlots.Size + PrepSlots.Spacing));
    }

    private string SlotSignature()
    {
        return _pawn.IncenseCapacity + ":" + string.Join(",", _pawn.ActiveIncense.Select(a =>
            $"{a.Def?.Moniker ?? a.SourceMoniker}:{a.EncountersRemaining}"));
    }
}
