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
            Scale = new Vector2(1, 1),
            Root = new VerticalStackPanel
            {
                Widgets =
                {
                    _gameHud,
                    new HorizontalSeparator { Margin = new Thickness(0, 0, 0, 20) },
                    _campOverview
                }
            },
            HasExternalTextInput = true
        };
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