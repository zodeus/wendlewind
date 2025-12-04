namespace Grafted.Scenes.MainGameScene.Gui.Widgets;

public sealed class PlayerKillsWindow : Window
{
    public PlayerKillsWindow(PlayerKillRecords deathRecords)
    {
        Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.Red];
        MinWidth = 500;
        MinHeight = 300;
        Padding = new Thickness(20);
        ScrollViewer scrollViewer = new();
        Grid container = new() { RowSpacing = 10, ColumnSpacing = 50, Margin = new Thickness(10), DefaultColumnProportion = Proportion.Auto, DefaultRowProportion = Proportion.Auto };
        scrollViewer.Content = container;
        Content = scrollViewer;
        var biomeLabel = new Label(BaseContent.Styles.Label.Medium) { Text = "Biome" };
        Grid.SetRow(biomeLabel, 0); Grid.SetColumn(biomeLabel, 0);
        container.Widgets.Add(biomeLabel);
        
        var creatureLabel = new Label(BaseContent.Styles.Label.Medium) { Text = "Creature" };
        Grid.SetRow(creatureLabel, 0); Grid.SetColumn(creatureLabel, 1);
        container.Widgets.Add(creatureLabel);
        
        var causeLabel = new Label(BaseContent.Styles.Label.Medium) { Text = "Cause of death" };
        Grid.SetRow(causeLabel, 0); Grid.SetColumn(causeLabel, 2);
        container.Widgets.Add(causeLabel);
        
        var damageLabel = new Label(BaseContent.Styles.Label.Medium) { Text = "Damage" };
        Grid.SetRow(damageLabel, 0); Grid.SetColumn(damageLabel, 3);
        container.Widgets.Add(damageLabel);
        
        var ticksLabel = new Label(BaseContent.Styles.Label.Medium) { Text = "Ticks" };
        Grid.SetRow(ticksLabel, 0); Grid.SetColumn(ticksLabel, 4);
        container.Widgets.Add(ticksLabel);
        
        var roundLabel = new Label(BaseContent.Styles.Label.Medium) { Text = "Round" };
        Grid.SetRow(roundLabel, 0); Grid.SetColumn(roundLabel, 5);
        container.Widgets.Add(roundLabel);
        int gridRow = 1;
        int totalTicks = 0;
        double totalDamage = 0;
        foreach (DeathRecord deathRecord in deathRecords)
        {
            totalTicks += deathRecord.Ticks;
            totalDamage += deathRecord.TotalDamageDealt;

            var ticks = new Label { Text = $"{deathRecord.Ticks}" };
            Grid.SetRow(ticks, gridRow); Grid.SetColumn(ticks, 4);
            var defaultColor = deathRecord.Ticks > 3000 ? Color.OrangeRed : ticks.TextColor;
            ticks.TextColor = defaultColor;

            var biome = new Label { Text = deathRecord.Biome.Label };
            Grid.SetRow(biome, gridRow); Grid.SetColumn(biome, 0);
            container.Widgets.Add(biome);
            
            var pawnName = new Label { Text = deathRecord.PawnName };
            Grid.SetRow(pawnName, gridRow); Grid.SetColumn(pawnName, 1);
            container.Widgets.Add(pawnName);
            
            var cause = new Label { Text = deathRecord.CauseOfDeath };
            Grid.SetRow(cause, gridRow); Grid.SetColumn(cause, 2);
            container.Widgets.Add(cause);
            
            var damage = new Label { Text = $"{deathRecord.TotalDamageDealt:N0}" };
            Grid.SetRow(damage, gridRow); Grid.SetColumn(damage, 3);
            container.Widgets.Add(damage);
            
            container.Widgets.Add(ticks);
            
            var round = new Label { TextColor = defaultColor, Text = deathRecord.Round.ToString() };
            Grid.SetRow(round, gridRow); Grid.SetColumn(round, 5);
            container.Widgets.Add(round);
            gridRow++;
        }


        var totalDamageLabel = new Label { Text = $"{totalDamage:N0}" };
        Grid.SetRow(totalDamageLabel, gridRow); Grid.SetColumn(totalDamageLabel, 3);
        container.Widgets.Add(totalDamageLabel);
        
        var totalTicksLabel = new Label { Text = $"{totalTicks:N0}" };
        Grid.SetRow(totalTicksLabel, gridRow); Grid.SetColumn(totalTicksLabel, 4);
        container.Widgets.Add(totalTicksLabel);
    }
}