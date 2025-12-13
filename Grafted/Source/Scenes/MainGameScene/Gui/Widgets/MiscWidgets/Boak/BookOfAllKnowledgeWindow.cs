namespace Grafted.Scenes.MainGameScene.Gui.Widgets.MiscWidgets.Boak;

internal class BookOfAllKnowledgeWindow : Window
{
    //private readonly BookOfAllKnowledge _boak;

    public BookOfAllKnowledgeWindow( /*BookOfAllKnowledge boak*/)
    {
        //_boak = boak;
        Width = Screen.Width;
        Height = Screen.Height;
        Title = "Book of all Knowledge";
        TitleFont = BaseContent.Fonts.Default.Large;
        TabPanel tabPanel = new()
        {
            ButtonStyle = BaseContent.Styles.Button.Large
        };
        Content = tabPanel;
        //tabPanel.AddTab("Zones",  new DefsPanel(DefRepository<ZoneDef>.Defs, Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.SmallFrame]));
        tabPanel.AddTab("Pawns", new BoakPawnPanel());
        tabPanel.AddTab("Items", new BoakItemsPanel(DefRepository<ItemDef>.Defs, DefRepository<WeaponManeuverDef>.Defs));
        tabPanel.AddTab("Stats", new BoakStatsPanel(DefRepository<StatDef>.Defs.OrderBy(d => d.Label).ToList()));
        tabPanel.AddTab("Chests", new BoakLootBoxPanel(DefRepository<LootBoxDef>.Defs));
        tabPanel.AddTab("Zones", new BoakBiomePanel(DefRepository<ZoneDef>.Defs));
        tabPanel.AddTab("Weather", new BoakWeatherPanel());
    }
}