using Grafted.Graphics.Textures;
using Grafted.Scenes.Components;
using Grafted.Scenes.MainGameScene;

namespace Grafted.Sim;

public class MainMenuScene : Scene
{
    private Desktop _desktop = null!;

    protected override void OnStart()
    {
        var grid = new Panel()
        {
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            Background = new TextureRegion(TextureUtils.PreMultiply(BaseContent.Textures.MainMenuBackground)), Width = 1200, Height = 1200
        };

        var buttonPanel = new VerticalStackPanel
        {
            Margin = new Thickness(80, 0, 0, 0),
            Spacing = 20, VerticalAlignment = VerticalAlignment.Center
        };
        var newGame = new Image
        {
            Background = new TextureRegion(TextureUtils.PreMultiply(BaseContent.Textures.MainMenuPlay)),
            OverBackground = new TextureRegion(TextureUtils.PreMultiply(BaseContent.Textures.MainMenuPlayOver)),
            Width = 256, Height = 80
        };
        var quit = new Image
        {
            Background = new TextureRegion(TextureUtils.PreMultiply(BaseContent.Textures.MainMenuQuit)), Width = 256, Height = 80
        };
        newGame.TouchDown += (_, _) => Core.ChangeScene<GameScene>();
        buttonPanel.Widgets.Add(newGame);
        buttonPanel.Widgets.Add(quit);
        grid.Widgets.Add(buttonPanel);

        if (DebugSettings.QuickPlay)
        {
            Core.ChangeScene<GameScene>();
        }

        /*if (File.Exists("save.xml")) {
            Sim.Load("save.xml");
            Scene.Load<GameScene>()
        }
        else {
            Scene.Load<MainMenuScene>();
        }*/

        _desktop = new Desktop
        {
            Root = grid
        };
    }

    public override void Draw(float deltaTime)
    {
        _desktop.Render();
    }
}