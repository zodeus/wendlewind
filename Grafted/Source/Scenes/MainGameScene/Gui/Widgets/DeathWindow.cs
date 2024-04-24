namespace Grafted.Scenes.MainGameScene.Gui.Widgets;

public class DeathWindow : Window
{
    public DeathWindow()
    {
        TitleGrid.Visible = false;
        Width = 600;
        Height = 400;
        Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.Red];
        var button = new TextButton(BaseContent.Styles.Button.Large) { Text = "Try again" };
        button.Click += (_, _) =>
        {
            Close();
            Core.Context.Restart();
        };
        Content = new VerticalStackPanel
        {
            Spacing = 15,
            Padding = new Thickness(50), HorizontalAlignment = HorizontalAlignment.Center,
            Widgets =
            {
                new Label(BaseContent.Styles.Label.Huge) { Text = "You dead..." },
                button
            }
        };
    }
}