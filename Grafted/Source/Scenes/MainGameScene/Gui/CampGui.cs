using Grafted.Scenes.MainGameScene.Gui.CombatGui;
using Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets;
using Myra;

namespace Grafted.Scenes.MainGameScene.Gui;

public class CampGui : BaseGui
{
    private readonly GameHud _gameHud;
    private readonly PawnBodyEffectsWindow _pawnBodyEffectsWindow;
    private readonly CampOverviewPanel _campOverview;

    public CampGui(World world)
    {
        _gameHud = new GameHud(world.Player) { HorizontalAlignment = HorizontalAlignment.Stretch, Margin = new Thickness(0, 5, 0, 0) };
        _campOverview = new CampOverviewPanel(this, world);
        Desktop = new Desktop
        {
            Scale = new Vector2(1, 1),
            Root = new VerticalStackPanel
            {
                Widgets =
                {
                    _gameHud,
                    new HorizontalSeparator() { Margin = new Thickness(0, 0, 0, 20) },
                    _campOverview
                }
            },
            HasExternalTextInput = true
        };

        _pawnBodyEffectsWindow = new PawnBodyEffectsWindow(world.PlayerPawn) { Width = 340, Height = 500 };
        _pawnBodyEffectsWindow.Show(Desktop, new Point(10, 115));
    }

    public override void Update(float deltaTime)
    {
        _gameHud.Update();
        _campOverview.Update();
        _pawnBodyEffectsWindow.Update();
        base.Update(deltaTime);
    }

    public override void Dispose()
    {
    }
}