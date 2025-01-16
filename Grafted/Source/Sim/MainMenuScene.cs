using Grafted.Graphics.Textures;
using Grafted.Scenes.Components;
using Grafted.Scenes.MainGameScene;

namespace Grafted.Sim;

public class MainMenuScene : Scene {
    private Desktop _desktop = null!;

    protected override void OnStart() {
        var grid = new Grid {ShowGridLines = false};
        grid.DefaultRowProportion = Proportion.Auto;
        grid.ColumnsProportions.Add(Proportion.Auto);
        grid.ColumnsProportions.Add(Proportion.Fill);
        grid.Widgets.Add(new Image {
            Background = new TextureRegion(TextureUtils.PreMultiply(BaseContent.Textures.MilgrethTitle)), Width = 825, Height = 242, GridRow = 0 , GridColumnSpan = 2,
            HorizontalAlignment = HorizontalAlignment.Center
        });
        grid.Widgets.Add(new Image { Background = new TextureRegion(TextureUtils.PreMultiply(BaseContent.Textures.MilgrethImage)), Width = 900, Height = 900, GridRow = 1, GridColumn = 0 });

        var buttonPanel = new VerticalStackPanel { Spacing = 20, GridRow = 1, GridColumn = 1, HorizontalAlignment = HorizontalAlignment.Left,VerticalAlignment = VerticalAlignment.Top};
        var newGame = new Image {
            Background = new TextureRegion(TextureUtils.PreMultiply(BaseContent.Textures.MilgrethPlay)), 
            OverBackground = new TextureRegion(TextureUtils.PreMultiply(BaseContent.Textures.MilgrethPlayOver)), 
            Width = 256, Height = 128
        }; 
        var quit = new Image {
            Background = new TextureRegion(TextureUtils.PreMultiply(BaseContent.Textures.MilgrethQuit)), Width = 256, Height = 128
        };
        newGame.TouchDown += (_, _) => Core.ChangeScene<GameScene>();
        buttonPanel.Widgets.Add(newGame);
        buttonPanel.Widgets.Add(quit);
        grid.Widgets.Add(buttonPanel);

        if (DebugSettings.QuickPlay) {
            Core.ChangeScene<GameScene>();
        }

        /*if (File.Exists("save.xml")) {
            Sim.Load("save.xml");
            Scene.Load<GameScene>()
        }
        else {
            Scene.Load<MainMenuScene>();
        }*/

        _desktop = new Desktop {
            Root = grid
        };
    }

    public override void Draw(float deltaTime) {
        _desktop.Render();
    }
}