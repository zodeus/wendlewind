using Wendlewind.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets;
using Wendlewind.Scenes.MainGameScene.Gui.Widgets.PawnRenderer;

namespace Wendlewind.Scenes.MainGameScene.Gui.Widgets.CombatWidgets;

internal sealed class PawnCombatPanel : HorizontalStackPanel
{
    public readonly Pawn Pawn;
    private readonly Encounter _encounter;
    private PawnRenderWidget? _bodyWidget;
    private readonly BaseGui _gui;
    private readonly List<IUpdatable> _updatables = new();
    private PawnEquipmentPanel? _equipment;

    /// <summary>
    /// Gets the body render widget for this pawn, if available.
    /// </summary>
    public PawnRenderWidget? BodyWidget => _bodyWidget;
    public PawnEquipmentPanel? EquipmentPanel => _equipment;

    public PawnCombatPanel(BaseGui gui, Pawn pawn, Encounter encounter, bool includePortrait = true)
    {
        Pawn = pawn;
        _encounter = encounter;
        _gui = gui;
        var isPlayer = pawn.PawnType == PawnType.Player;
        var equipment = GenerateEquipment();

        if (includePortrait)
        {
            var pawnPanel = GeneratePawnPanel();
            if (isPlayer)
            {
                Widgets.Add(equipment);
                Widgets.Add(pawnPanel);
            }
            else
            {
                Widgets.Add(pawnPanel);
                Widgets.Add(equipment);
            }
        }
        else
        {
            Widgets.Add(equipment);
        }

        Update(0f);
    }

    private Widget GenerateEquipment()
    {
        var align = Pawn.PawnType == PawnType.Player
            ? HorizontalAlignment.Right
            : HorizontalAlignment.Left;

        const int panelPad = 10;
        const int namePlateHeight = 44;
        var layout = EquipmentGridLayout.Build(Pawn);
        var rows = Math.Max(1, layout.Rows);
        var targetHeight = BaseContent.IconSizes.Portrait + namePlateHeight;
        var innerHeight = targetHeight - panelPad * 2;
        var cellSize = Math.Max(20, (innerHeight - (rows - 1) * 2) / rows);

        _equipment = new PawnEquipmentPanel(_gui, Pawn, cellSize: cellSize, showSlotHints: false, readOnly: true, hoverToInspect: true)
        {
            VerticalAlignment = VerticalAlignment.Top,
            HorizontalAlignment = align
        };
        _updatables.Add(_equipment);

        var equipmentFrame = new VerticalStackPanel
        {
            Height = targetHeight,
            VerticalAlignment = VerticalAlignment.Top,
            HorizontalAlignment = align,
            Padding = new Thickness(panelPad),
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrame],
            ClipToBounds = false,
            Widgets = { _equipment }
        };

        var effects = new PawnBodyEffectsPanel(_gui, Pawn, EffectsPanelOrientation.Vertical)
        {
            VerticalAlignment = VerticalAlignment.Top
        };
        _updatables.Add(effects);

        var row = new HorizontalStackPanel
        {
            Spacing = 4,
            VerticalAlignment = VerticalAlignment.Top,
            HorizontalAlignment = align
        };
        if (Pawn.PawnType == PawnType.Player)
        {
            row.Widgets.Add(effects);
            row.Widgets.Add(equipmentFrame);
        }
        else
        {
            row.Widgets.Add(equipmentFrame);
            row.Widgets.Add(effects);
        }

        return row;
    }

    private PawnRenderWidget CreateBodyWidget(int size)
    {
        _bodyWidget = new PawnRenderWidget(Pawn, size)
        {
            Width = size,
            Height = size,
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrame]
        };

        if (Pawn.PawnType == PawnType.Player)
        {
            _bodyWidget.HorizontalAlignment = HorizontalAlignment.Right;
        }

        if (_encounter.Weather != null)
        {
            _bodyWidget.SetWeather(_encounter.Weather);
        }

        return _bodyWidget;
    }

    private Widget GeneratePawnPanel()
    {
        VerticalStackPanel panel = new()
        {
            VerticalAlignment = VerticalAlignment.Top,
            DefaultProportion = Proportion.Auto
        };

        panel.Widgets.Add(CreateBodyWidget(BaseContent.IconSizes.Portrait));

        Label namePlate = new()
        {
            Text = Pawn.LabelShort,
            Height = 44,
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrame],
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Padding = new Thickness(12)
        };
        namePlate.TouchDown += (_, _) =>
        {
            _gui.ViewEntity(Pawn);
        };
        panel.Widgets.Add(namePlate);

        return panel;
    }

    public void Update(float deltaTime)
    {
        _bodyWidget?.Update(deltaTime);
        _equipment?.Update(deltaTime);
        foreach (var u in _updatables)
        {
            if (u != _equipment)
            {
                u.Update();
            }
        }
    }
}
