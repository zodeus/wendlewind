using Wendlemire.NetCode;
using Wendlemire.Scenes.MainGameScene.Gui;
using Wendlemire.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets;

namespace Wendlemire.Scenes.ArenaScene.Gui;

public sealed class ArenaPrepScreen : VerticalStackPanel
{
    private readonly PawnPreparationPanel _pawnPanel;
    private readonly ArenaHud _hud;

    public ArenaPrepScreen(BaseGui gui, GameContext context, Action onFight, Action onShop, ArenaRankDisplay? rank = null)
    {
        Spacing = 10;
        Padding = new Thickness(8);
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;

        _hud = new ArenaHud(context, rank);
        _pawnPanel = new PawnPreparationPanel(gui, context.PlayerPawn, showGrimoire: false);

        var shop = new CursorButton(BaseContent.Styles.Button.Large)
        {
            Content = new Label { Text = "Shop", HorizontalAlignment = HorizontalAlignment.Center },
            VerticalAlignment = VerticalAlignment.Center
        };
        shop.Click += (_, _) => onShop();

        var fight = new CursorButton(BaseContent.Styles.Button.LargeGold)
        {
            Content = new Label { Text = "Fight", HorizontalAlignment = HorizontalAlignment.Center },
            VerticalAlignment = VerticalAlignment.Center
        };
        fight.Click += (_, _) => onFight();

        _pawnPanel.SetControls(shop);
        _pawnPanel.SetControls(fight);
        _pawnPanel.SetControls(_hud);

        Widgets.Add(_pawnPanel);
        SetProportionType(_pawnPanel, ProportionType.Fill);
    }

    public void Update()
    {
        _hud.Refresh();
        _pawnPanel.Update();
    }
}
