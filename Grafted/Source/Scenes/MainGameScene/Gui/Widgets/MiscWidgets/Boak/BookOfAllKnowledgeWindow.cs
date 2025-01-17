using Grafted.Scenes.MainGameScene.Gui.Widgets.DefWidgets;
using Grafted.Sim.Entities;
using Grafted.Sim.Entities.Pawns.Modifiers;
using Grafted.Sim.LootBoxes;

namespace Grafted.Scenes.MainGameScene.Gui.Widgets.MiscWidgets.Boak;

internal class BookOfAllKnowledgeWindow : Window
{
    //private readonly BookOfAllKnowledge _boak;

    public BookOfAllKnowledgeWindow( /*BookOfAllKnowledge boak*/)
    {
        //_boak = boak;
        Width = 2800;
        Height = 1400;
        Title = "Book of all Knowledge";
        TitleFont = BaseContent.Fonts.Default.Large;
        TabPanel tabPanel = new()
        {
            ButtonStyle = BaseContent.Styles.Button.Large
        };
        Content = tabPanel;
        //tabPanel.AddTab("Biomes",  new DefsPanel(DefRepository<BiomeDef>.Defs, Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.SmallFrame]));
        tabPanel.AddTab("Pawns", new BoakPawnPanel());
        tabPanel.AddTab("Items", new BoakItemsPanel(DefRepository<ItemDef>.Defs, DefRepository<ToolManeuverDef>.Defs));
        tabPanel.AddTab("Stats", new BoakStatsPanel(DefRepository<StatDef>.Defs.OrderBy(d => d.Label).ToList()));
        tabPanel.AddTab("LootBoxes", new BoakLootBoxPanel(DefRepository<LootBoxDef>.Defs));
        tabPanel.AddTab("Biomes", new BoakBiomePanel(DefRepository<BiomeDef>.Defs));
    }
}