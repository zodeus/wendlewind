using Wendlewind.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets.PawnBodyPanelWidgets;
using Wendlewind.Scenes.MainGameScene.Gui.Widgets.PawnRenderer;

namespace Wendlewind.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets.PawnPreparationPanelWidgets;

public sealed class PawnSummaryCard : VerticalStackPanel, IUpdatable
{
    private readonly BaseGui _gui;
    private readonly Pawn _pawn;
    private readonly PawnRenderWidget _portrait;
    private readonly VerticalBloodBar _bloodBar;
    private readonly PawnCapabilitiesOverlay _capabilities;
    private readonly Label _hint;
    private Window? _bodyWindow;
    private PawnBodyPanel? _bodyOverlay;

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
            Height = 192
        };
        _portrait.Clicked += (_, _) => OpenBodyOverlay();

        _bloodBar = new VerticalBloodBar(pawn) { Width = 16, Height = 192 };

        var portraitRow = new HorizontalStackPanel { Spacing = 6 };
        portraitRow.Widgets.Add(_portrait);
        portraitRow.Widgets.Add(_bloodBar);
        Widgets.Add(portraitRow);

        _capabilities = new PawnCapabilitiesOverlay(pawn.Body);
        Widgets.Add(_capabilities);

        _hint = new Label(BaseContent.Styles.Label.Small)
        {
            Text = "Click portrait to inspect body",
            TextColor = new Color(150, 150, 150)
        };
        Widgets.Add(_hint);
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

        _bloodBar.Value = _pawn.Body.BloodPercent * 100;
        _capabilities.Update();
        _bodyOverlay?.Update();
    }
}
