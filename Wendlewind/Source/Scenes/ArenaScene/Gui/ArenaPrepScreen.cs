using Wendlewind.Scenes.MainGameScene.Gui;
using Wendlewind.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets;

namespace Wendlewind.Scenes.ArenaScene.Gui;

public sealed class ArenaPrepScreen : VerticalStackPanel
{
    private readonly PawnPreparationPanel _pawnPanel;
    private readonly ArenaHud _hud;

    public ArenaPrepScreen(BaseGui gui, GameContext context, Action onFight)
    {
        Spacing = 10;
        Padding = new Thickness(8);
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;

        _hud = new ArenaHud(context);
        _pawnPanel = new PawnPreparationPanel(gui, context.PlayerPawn, showGrimoire: false);

        var fight = new CursorButton(BaseContent.Styles.Button.LargeGold)
        {
            Content = new Label { Text = "Fight", HorizontalAlignment = HorizontalAlignment.Center }
        };
        fight.Click += (_, _) => onFight();

        var headerExtras = new HorizontalStackPanel
        {
            Spacing = 24,
            VerticalAlignment = VerticalAlignment.Center,
            Widgets = { fight, _hud }
        };
        _pawnPanel.SetControls(headerExtras);

        Widgets.Add(_pawnPanel);
        SetProportionType(_pawnPanel, ProportionType.Fill);
    }

    public void Update()
    {
        _hud.Refresh();
        _pawnPanel.Update();
    }
}
