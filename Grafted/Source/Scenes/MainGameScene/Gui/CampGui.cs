namespace Grafted.Scenes.MainGameScene.Gui;

public class CampGui : BaseGui
{
    private readonly WorldTextHandler _worldTextHandler;
    private readonly GameHud _gameHud;
    private readonly CampOverviewPanel _campOverview;

    public override WorldTextHandler WorldTextHandler => _worldTextHandler;
    
    public CampGui(GameContext context, WorldTextHandler worldTextHandler)
    {
        _worldTextHandler = worldTextHandler;
        _gameHud = new GameHud(this, context) { HorizontalAlignment = HorizontalAlignment.Stretch, Margin = new Thickness(0, 5, 0, 0) };
        _campOverview = new CampOverviewPanel(this, context);
        Desktop = new Desktop
        {
            Root = new VerticalStackPanel
            {
                Spacing = 8,
                Widgets =
                {
                    _gameHud,
                    _campOverview
                }
            },
            HasExternalTextInput = true
        };
        Core.ConfigureDesktopScaling(Desktop);
    }

    public override void Update(float deltaTime)
    {
        _gameHud.Update();
        _campOverview.Update();
        base.Update(deltaTime);
    }

    public override void Dispose()
    {
    }
}