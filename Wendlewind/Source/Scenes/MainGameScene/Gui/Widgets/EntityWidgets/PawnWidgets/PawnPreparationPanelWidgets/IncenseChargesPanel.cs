using Image = Myra.Graphics2D.UI.Image;

namespace Wendlewind.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets.PawnPreparationPanelWidgets;

public sealed class IncenseChargesPanel : PrepCard, IUpdatable
{
    private readonly Pawn _pawn;
    private readonly VerticalStackPanel _active;
    private readonly PrepItemGrid _inventory;
    private int _lastCount = -1;

    public IncenseChargesPanel(BaseGui gui, Pawn pawn) : base("Incense")
    {
        _pawn = pawn;

        Body.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
        {
            Text = "Lasts multiple battles",
            TextColor = new Color(160, 160, 160)
        });

        _active = new VerticalStackPanel { Spacing = 6 };
        Body.Widgets.Add(_active);

        _inventory = new PrepItemGrid(
            gui,
            pawn.Inventory,
            item => item.ItemDef.IncenseProperties?.Effect != null,
            TryLight,
            _ => _pawn.HasFlameStick()
                ? "Click to light"
                : "Need a Flame Stick equipped to light incense",
            isDisabled: _ => !_pawn.HasFlameStick());
        SetInventory(_inventory);

        Rebuild();
    }

    public void Update()
    {
        if (_pawn.ActiveIncense.Count != _lastCount)
        {
            Rebuild();
        }

        _inventory.Update();
    }

    private void TryLight(Item item)
    {
        if (_pawn.TryLightIncense(item))
        {
            Rebuild();
        }
    }

    private void Rebuild()
    {
        _lastCount = _pawn.ActiveIncense.Count;
        _active.Widgets.Clear();

        if (_pawn.ActiveIncense.Count == 0)
        {
            _active.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
            {
                Text = _pawn.HasFlameStick()
                    ? "Click an incense below to light it"
                    : "Need a Flame Stick equipped to light incense",
                TextColor = new Color(140, 140, 140)
            });
            return;
        }

        foreach (var incense in _pawn.ActiveIncense)
        {
            _active.Widgets.Add(CreateIncenseRow(incense));
        }
    }

    private static Widget CreateIncenseRow(ActiveIncense incense)
    {
        var itemDef = incense.SourceMoniker != null
            ? DefRepository<ItemDef>.GetByMoniker(incense.SourceMoniker, raiseError: false)
            : null;
        var total = itemDef?.IncenseProperties?.GetDurationInEncounters() ?? incense.EncountersRemaining;
        var remaining = incense.EncountersRemaining;
        var name = incense.Def?.Label ?? itemDef?.Label ?? incense.SourceMoniker ?? "Incense";

        var icon = new Image
        {
            Width = BaseContent.IconSizes.Small,
            Height = BaseContent.IconSizes.Small,
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

        var header = new HorizontalStackPanel { Spacing = 8 };
        header.Widgets.Add(icon);
        header.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
        {
            Text = name,
            TextColor = Color.Orange,
            VerticalAlignment = VerticalAlignment.Center
        });

        var pips = new HorizontalStackPanel { Spacing = 3 };
        for (var i = 0; i < Math.Max(total, remaining); i++)
        {
            var filled = i < remaining;
            pips.Widgets.Add(new Panel
            {
                Width = 10,
                Height = 10,
                VerticalAlignment = VerticalAlignment.Center,
                Background = new SolidBrush(filled ? new Color(220, 150, 70) : new Color(50, 45, 40))
            });
        }

        var remainingLabel = new Label(BaseContent.Styles.Label.Small)
        {
            Text = remaining == 1 ? "1 battle left" : $"{remaining} battles left",
            TextColor = new Color(200, 180, 140),
            VerticalAlignment = VerticalAlignment.Center
        };

        var pipRow = new HorizontalStackPanel { Spacing = 8 };
        pipRow.Widgets.Add(pips);
        pipRow.Widgets.Add(remainingLabel);

        return new VerticalStackPanel
        {
            Spacing = 4,
            Widgets = { header, pipRow }
        };
    }
}
