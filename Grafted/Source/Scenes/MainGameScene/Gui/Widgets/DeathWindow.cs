namespace Grafted.Scenes.MainGameScene.Gui.Widgets;

public sealed class DeathWindow : Window
{
    public DeathWindow()
    {
        TitlePanel.Visible = false;
        Width = 600;
        Height = 400;
        Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.Red];
        var button = new TextButton(BaseContent.Styles.Button.Large)
        {
            HorizontalAlignment = HorizontalAlignment.Center, Text = "Restart"
        };
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
                new Label(BaseContent.Styles.Label.Huge)
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Text = "You dead..."
                },
                button
            }
        };
    }
}