namespace Grafted.Scenes.MainGameScene.Gui.Widgets;

public sealed class PlayerAchievementsWindow : Window
{
    public PlayerAchievementsWindow()
    {
        Title = "Achievements";
        Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrameBright];
        MinWidth = 500;
        MinHeight = 300;
        Padding = new Thickness(20);
        Content = new VerticalStackPanel
        {
            Widgets =
            {
                new Label { Text = "  - Tickled brain" },
                new Label { Text = "  - Tis' but a scratch!" },
                new Label { Text = "  - Just the tip" },
                new Label { Text = "  - Two! I don't need two!" },
                new Label { Text = "  - Noticed by a god" },
                new Label { Text = "  - The Spicer" },
                new Label { Text = "  - Blood Chugger" },
                new Label { Text = "  - Rushing River" },
                new Label { Text = "  - Vampire Wannabe" },
                new Label { Text = "  - Oh Wow! Your body is eating you" },
                new Label { Text = "  - Looter" },
                new Label { Text = "  - Fine Diner" },
                new Label { Text = "  - Head Banger" },
                new Label { Text = "  - One dies, the rest lives!" },


                new Label { Text = "  - Tarred Blood" , Margin = new Thickness(0,20,0,0)},
                new Label { Text = "  - Synthetic Arteries" },
                new Label { Text = "  - Random Bits" },
                new Label { Text = "  - Arterial Toughness" },
                new Label { Text = "  - Blood Bloated" },
                new Label { Text = "  - Carbon Weaved Ligaments" },
                new Label { Text = "  - Elven Grace" },
                new Label { Text = "  - God Touched" },
                new Label { Text = "  - Marked by a God" },
                new Label { Text = "  - Vampire Thirst" },
                new Label { Text = "  - Trinket Sniffer" },
            }
        };
    }
}