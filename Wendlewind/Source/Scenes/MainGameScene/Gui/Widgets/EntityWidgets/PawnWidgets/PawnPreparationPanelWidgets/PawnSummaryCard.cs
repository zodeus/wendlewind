using Wendlewind.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets.PawnBodyPanelWidgets;
using Wendlewind.Scenes.MainGameScene.Gui.Widgets.PawnRenderer;

namespace Wendlewind.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets.PawnPreparationPanelWidgets;

public sealed class PawnSummaryCard : VerticalStackPanel, IUpdatable
{
    private readonly BaseGui _gui;
    private readonly Pawn _pawn;
    private readonly PawnRenderWidget _portrait;
    private readonly PawnCapabilitiesOverlay _capabilities;
    private readonly PawnSkillsPanel _skills;
    private readonly PrepBuffList _buffs;
    private readonly PrepLoadoutSummary _loadout;
    private Window? _bodyWindow;
    private PawnBodyPanel? _bodyOverlay;
    private string _buffSignature = "";

    public PawnSummaryCard(BaseGui gui, Pawn pawn)
    {
        _gui = gui;
        _pawn = pawn;
        Spacing = 8;
        Padding = new Thickness(10);
        Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrame];
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;

        Widgets.Add(new Label(BaseContent.Styles.Label.Normal)
        {
            Text = pawn.LabelShort,
            TextColor = Color.Goldenrod,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 4, 0, 0)
        });

        _portrait = new PawnRenderWidget(pawn, 192)
        {
            Width = 192,
            Height = 192,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        _portrait.Clicked += (_, _) => OpenBodyOverlay();
        Widgets.Add(_portrait);

        _capabilities = new PawnCapabilitiesOverlay(pawn.Body);
        Widgets.Add(_capabilities);

        Widgets.Add(new Label(BaseContent.Styles.Label.Small)
        {
            Text = "Stance",
            TextColor = new Color(180, 180, 180)
        });
        Widgets.Add(new BodyStanceBar(pawn)
        {
            HorizontalAlignment = HorizontalAlignment.Left
        });

        _skills = new PawnSkillsPanel(pawn.Skills);
        Widgets.Add(_skills);

        _buffs = new PrepBuffList();
        _loadout = new PrepLoadoutSummary(gui, pawn);
        var details = new VerticalStackPanel
        {
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Widgets = { _buffs, _loadout }
        };
        var scroll = new ScrollViewer
        {
            Content = details,
            ShowHorizontalScrollBar = false,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };
        Widgets.Add(scroll);
        SetProportionType(scroll, ProportionType.Fill);
        RefreshBuffs();
    }

    public void OpenBodyOverlay()
    {
        if (_bodyWindow?.IsPlaced == true)
        {
            return;
        }

        _bodyOverlay = new PawnBodyPanel(_gui, _pawn.Body, _pawn.Inventory)
        {
            Height = 740
        };
        _bodyWindow = new Window
        {
            Title = $"{_pawn.LabelShort} - Body",
            Content = _bodyOverlay
        };
        _bodyWindow.ShowModal(_gui.Desktop);
    }

    public void Update()
    {
        if (_bodyWindow is { IsPlaced: false })
        {
            _bodyOverlay = null;
            _bodyWindow = null;
        }

        _capabilities.Update();
        _skills.Update();
        RefreshBuffs();
        _loadout.Update();
        _bodyOverlay?.Update();
    }

    private void RefreshBuffs()
    {
        var signature = string.Join(",", _pawn.MealPlan.Items.Select(i => i?.Id ?? -1))
                        + "|"
                        + string.Join(",", _pawn.ActiveIncense.Select(a => a.Def?.Moniker ?? a.SourceMoniker));
        if (signature == _buffSignature)
        {
            return;
        }

        _buffSignature = signature;
        _buffs.SetEffects(PrepBuffList.FromPrep(_pawn));
    }
}
