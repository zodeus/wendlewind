using Grafted.Scenes.MainGameScene.Gui.Widgets.DevConsole;
using Grafted.Scenes.MainGameScene.Gui.Widgets.MapWidgets;

namespace Grafted.Scenes.MainGameScene.Gui;

public class MapGui : BaseGui
{
    private readonly WorldTextHandler _worldTextHandler;
    private readonly GameHud _gameHud;
    private readonly MapPanel _mapPanel;
    public override WorldTextHandler WorldTextHandler => _worldTextHandler;

    public MapGui(GameContext context, WorldTextHandler worldTextHandler)
    {
        _worldTextHandler = worldTextHandler;
        
        // Create desktop first so NodeMapWidget can reference it
        Desktop = new Desktop { HasExternalTextInput = true };
        
        _gameHud = new GameHud(this, context) 
        { 
            HorizontalAlignment = HorizontalAlignment.Stretch, 
            Margin = new Thickness(0, 5, 0, 0) 
        };

        // Create the node-based zone map
        _mapPanel = new MapPanel(context.World)
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Margin = new Thickness(10, 50, 10, 10)
        };

        var rootPanel = new Panel();
        rootPanel.Widgets.Add(new VerticalStackPanel
        {
            Spacing = 8,
            Widgets =
            {
                _gameHud,
                _mapPanel
            }
        });
        Desktop.Root = rootPanel;

        Core.ConfigureDesktopScaling(Desktop);
        InitializeConsole();
    }

    public override void Update(float deltaTime)
    {
        _gameHud.Update();
        base.Update(deltaTime);
    }
}