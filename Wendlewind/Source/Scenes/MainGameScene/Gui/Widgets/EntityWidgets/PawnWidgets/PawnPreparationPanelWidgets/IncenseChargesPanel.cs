using Image = Myra.Graphics2D.UI.Image;

namespace Wendlewind.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets.PawnPreparationPanelWidgets;

public sealed class IncenseChargesPanel : PrepCard, IUpdatable
{
    private readonly Pawn _pawn;
    private readonly VerticalStackPanel _slotRows;
    private readonly PrepBuffList _buffs;
    private readonly PrepItemGrid _inventory;
    private string _slotSignature = "";
    private int _slotsPerRow = -1;

    public IncenseChargesPanel(BaseGui gui, Pawn pawn) : base("Incense")
    {
        _pawn = pawn;

        Body.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
        {
            Text = "Burns until extinguished",
            TextColor = new Color(160, 160, 160)
        });

        _slotRows = new VerticalStackPanel { Spacing = PrepSlots.Spacing };
        Body.Widgets.Add(_slotRows);
        _buffs = new PrepBuffList();
        Body.Widgets.Add(_buffs);

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
        if (!_pawn.HasFlameStick())
        {
            return "Need a Flame Stick equipped to light incense";
        }

        if (_pawn.ActiveIncense.Any(a => a.Def == item.ItemDef.IncenseProperties?.Effect?.Def))
        {
            return "Already lit";
        }

        if (_pawn.CanLightIncense(item))
        {
            return "Click to light";
        }

        return "All incense slots are full";
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
                : EmptySlot());
        }

        _buffs.SetEffects(PrepBuffList.FromIncense(_pawn));
    }

    private Widget FilledSlot(ActiveIncense incense, int index)
    {
        var itemDef = incense.SourceMoniker != null
            ? DefRepository<ItemDef>.GetByMoniker(incense.SourceMoniker, raiseError: false)
            : null;
        var name = incense.Def?.Label ?? itemDef?.Label ?? incense.SourceMoniker ?? "Incense";

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
        button.WithTooltip(name, "Click to extinguish");
        return PrepSlots.Frame(button);
    }

    private static Widget EmptySlot()
    {
        var empty = new Panel();
        empty.WithTooltip("Empty incense slot");
        return PrepSlots.Frame(empty);
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
        return IncenseProperties.MaxActive + ":" + string.Join(",", _pawn.ActiveIncense.Select(a =>
            $"{a.Def?.Moniker ?? a.SourceMoniker}:{a.EncountersRemaining}"));
    }
}
