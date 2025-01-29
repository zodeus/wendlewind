using Myra.Graphics2D.Brushes;

namespace Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets;

[UsedImplicitly]
public sealed class PawnPanel : EntityPanelBase
{
    private readonly Pawn _pawn;

    private readonly PawnPortraitPanel _portrait;
    private readonly TabPanel _tabPanel;

    public PawnPanel(BaseGui gui, Pawn pawn, EntityPanelProperties? properties = null) : base(gui, pawn, properties)
    {
        _pawn = pawn;
        MinWidth = 1400;
        MinHeight = 1200;
        var pane = new HorizontalStackPanel { Spacing = 40 };
        pane.Proportions.Add(Proportion.Auto);
        pane.Proportions.Add(Proportion.Fill);
        Widgets.Add(pane);

        _portrait = new PawnPortraitPanel(pawn) { Width = 300 };
        pane.Widgets.Add(_portrait);
        _tabPanel = new TabPanel();
        pane.Widgets.Add(_tabPanel);

        _tabPanel.AddTab("Body", new PawnBodyPanel(gui, pawn.Body));
        _tabPanel.AddTab("Equipment", new PawnEquipmentPanel(gui, pawn));
        _tabPanel.AddTab("Skills", new PawnSkillsPanel(pawn.Skills));
        _tabPanel.AddTab("Stats", new PawnStatsPanel(pawn));
    }

    public override void Update()
    {
        _portrait.Update();
        _tabPanel.Update();
    }
}

public sealed class PawnPortraitPanel : VerticalStackPanel
{
    private readonly Pawn _pawn;
    private readonly Label _attackSpeed;
    private readonly HorizontalProgressBar _bloodBar;

    public PawnPortraitPanel(Pawn pawn)
    {
        _pawn = pawn;
        Spacing = 5;

        Widgets.Add(new Panel
        {
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.DeepGold],
            Padding = new Thickness(8),
            Width = 256, Height = 256,
            Widgets =
            {
                new Image
                {
                    Background = new TextureRegion(pawn.Icon),
                    HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch,
                    BorderThickness = new Thickness(1), Border = new SolidBrush(Color.Transparent)
                }
            }
        });

        _bloodBar = new BloodBar(pawn) { Width = 256, Height = 30 };

        Widgets.Add(_bloodBar);
        _attackSpeed = new Label(BaseContent.Styles.Label.Normal) { Wrap = true, TextColor = Color.LightYellow, Margin = new Thickness(0, 0, 0, 20) };
        Widgets.Add(_attackSpeed);
        Widgets.Add(new Label(BaseContent.Styles.Label.Normal) { Text = $"Species: {pawn.Species}" });
        Widgets.Add(new Label(BaseContent.Styles.Label.Normal) { Text = $"Race: {pawn.Race.Label}" });
        Widgets.Add(new Label(BaseContent.Styles.Label.Normal) { Text = $"Gender: {pawn.Gender}" });

        Widgets.Add(new Label(BaseContent.Styles.Label.Normal) { Text = "Capabilities", Margin = new Thickness(0, 15, 0, 0) });
        Widgets.Add(new Label(BaseContent.Styles.Label.Normal) { Text = $"• Sight: {pawn.Body.Capabilities.Sight}" });
        Widgets.Add(new Label(BaseContent.Styles.Label.Normal) { Text = $"• Breathing: {pawn.Body.Capabilities.Breathing}" });
        Widgets.Add(new Label(BaseContent.Styles.Label.Normal) { Text = "• Circulation: n/a" });
        Widgets.Add(new Label(BaseContent.Styles.Label.Normal) { Text = "• Digestion: n/a" });
        Widgets.Add(new Label(BaseContent.Styles.Label.Normal) { Text = $"• Mobility: {pawn.Body.Capabilities.Mobility}" });
        Widgets.Add(new Label(BaseContent.Styles.Label.Normal) { Text = $"• Max Blood: {pawn.Body.MaxBlood}" });

        Widgets.Add(new Label(BaseContent.Styles.Label.Normal) { Text = "Traits", Margin = new Thickness(0, 15, 0, 0) });
        foreach (var trait in pawn.Traits)
        {
            Widgets.Add(new Label(BaseContent.Styles.Label.Normal) { Text = $"• {trait.Label}" });
        }
    }

    public void Update()
    {
        _attackSpeed.Text = "AttackSpeed";
        _bloodBar.Value = _pawn.Body.BloodPercent * 100;
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