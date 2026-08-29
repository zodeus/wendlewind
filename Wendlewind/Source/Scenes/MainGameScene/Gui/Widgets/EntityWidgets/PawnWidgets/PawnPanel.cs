using Wendlewind.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets.PawnBodyPanelWidgets;
using Wendlewind.Scenes.MainGameScene.Gui.Widgets.PawnRenderer;

namespace Wendlewind.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets;

[UsedImplicitly]
public sealed class PawnPanel : EntityPanelBase
{
    private readonly PawnPortraitPanel _portrait;
    private readonly TabPanel _tabPanel;

    public PawnPanel(BaseGui gui, Pawn pawn, EntityPanelProperties? properties = null) : base(gui, pawn, properties)
    {
        var pane = new HorizontalStackPanel { Spacing = 40 };
        Widgets.Add(pane);
        MinWidth = 1000;

        _portrait = new PawnPortraitPanel(pawn) { Width = 300 };
        pane.Widgets.Add(_portrait);
        _tabPanel = new TabPanel();
        StackPanel.SetProportionType(_tabPanel, ProportionType.Fill);
        pane.Widgets.Add(_tabPanel);

        _tabPanel.AddTab("Body", new PawnBodyPanel(gui, pawn.Body));
        _tabPanel.AddTab("Equipment", new PawnEquipmentPanel(gui, pawn));
        _tabPanel.AddTab("Profile", new PawnProfilePanel(pawn));
    }

    public override void Update()
    {
        _portrait.Update();
        _tabPanel.Update();
    }
}

public sealed class PawnPortraitPanel : VerticalStackPanel, IDisposable
{
    private readonly Pawn _pawn;
    private readonly Label _attackSpeed;
    private readonly HorizontalProgressBar _bloodBar;
    private readonly PawnRenderWidget _renderWidget;
    private readonly PawnCapabilitiesPanel _capabilitiesPanel;
    private readonly PawnStatsPanel _statsPanel;

    public PawnPortraitPanel(Pawn pawn)
    {
        _pawn = pawn;
        Spacing = 5;

        _renderWidget = new PawnRenderWidget(pawn, 256)
        {
            Width = 256,
            Height = 256,
            ShowEditButton = true
        };

        Widgets.Add(_renderWidget);

        _bloodBar = new BloodBar(pawn) { Width = 256, Height = 30 };

        Widgets.Add(_bloodBar);
        _attackSpeed = new Label(BaseContent.Styles.Label.Normal) { Margin = new Thickness(0, 0, 0, 20) };
        Widgets.Add(_attackSpeed);
        //Widgets.Add(new Label(BaseContent.Styles.Label.Normal) { Text = $"Max Blood: {pawn.Body.MaxBlood}" });

        _capabilitiesPanel = new PawnCapabilitiesPanel(pawn.Body);
        Widgets.Add(_capabilitiesPanel);

        _statsPanel = new PawnStatsPanel(pawn);
        Widgets.Add(_statsPanel);
    }

    public void Update()
    {
        _attackSpeed.Text = $"Attack Speed: {_pawn.AttackSpeed:F2}";
        _bloodBar.Value = _pawn.Body.BloodPercent * 100;
        _capabilitiesPanel.Update();
        _statsPanel.Update();
    }

    public void Dispose()
    {
        _renderWidget.Dispose();
    }
}

public class BloodBar : HorizontalProgressBar
{
    public BloodBar(Pawn pawn)
    {
        var color = pawn.PawnDef.Body.BloodType?.Color ?? Color.Black;
        Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Bar.FrameSmall];
        Filler = new ColoredRegion(Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Bar.Neutral], color);
        Padding = new Thickness(3, 6, 3, 6);
    }
}

public class VerticalBloodBar : VerticalProgressBar
{
    public VerticalBloodBar(Pawn pawn)
    {
        var color = pawn.PawnDef.Body.BloodType?.Color ?? Color.Black;
        Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Bar.FrameSmall];
        Filler = new ColoredRegion(Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Bar.NeutralVertical], color);
        Padding = new Thickness(3, 3, 3, 3);
        Rotation = 180;
    }
}