using System.IO;
using Wendlewind.Graphics.Textures;
using Wendlewind.Scenes.Components;
using Wendlewind.Scenes.MainGameScene;

namespace Wendlewind.Sim;

public class MainMenuScene : Scene
{
    private Desktop _desktop = null!;

    protected override void OnStart()
    {
        if (File.Exists("save.xml"))
        {
            Core.ChangeScene<GameScene>();
            return;
        }

        var panel = new Panel()
        {
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrameBright],
            Width = 900,
            Height = 420,
            Padding = new Thickness(8)

        };

        panel.Widgets.Add(new Image() { Background = new TextureRegion(BaseContent.Textures.MainMenuBackground), Width = 900, Height = 420 });

        _desktop = new Desktop
        {
            Root = new VerticalStackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Spacing = 20,
                Widgets = {
                    panel,
                    new Label(BaseContent.Styles.Label.Normal) {Text = "Click anywhere to start", HorizontalAlignment = HorizontalAlignment.Center}
                }
            }
        };
        _desktop.Root.TouchDown += (_, _) => Core.ChangeScene<GameScene>();
        Core.ConfigureDesktopScaling(_desktop);
    }

    public override void Draw(float deltaTime)
    {
        _desktop.Render();
    }
}