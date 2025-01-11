using Grafted.Scenes.MainGameScene.Gui.Widgets.DefWidgets;
using Grafted.Sim.Entities;
using Grafted.Sim.LootBoxes;

namespace Grafted.Scenes.MainGameScene.Gui.Widgets.MiscWidgets;

internal class BookOfAllKnowledgeWindow : Window {
    //private readonly BookOfAllKnowledge _boak;

    public BookOfAllKnowledgeWindow( /*BookOfAllKnowledge boak*/) {
        //_boak = boak;
        Width = 2800;
        Height = 1400;
        Title = "Book of all Knowledge";
        TitleFont = BaseContent.Fonts.Default.Large;
        TabPanel tabPanel = new() {
            ButtonStyle = BaseContent.Styles.Button.Large
        };
        Content = tabPanel;
        //tabPanel.AddTab("Biomes",  new DefsPanel(DefRepository<BiomeDef>.Defs, Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.SmallFrame]));
        tabPanel.AddTab("Pawns", new VerticalStackPanel {
            Widgets = {
                new DefsPanel(DefRepository<BiomeDef>.Defs),
                new DefsPanel(DefRepository<LootBoxDef>.Defs),
                new DefsPanel(DefRepository<PawnDef>.Defs),
                new DefsPanel(DefRepository<RaceDef>.Defs),
                new DefsPanel(DefRepository<SkillDef>.Defs),
                new DefsPanel(DefRepository<StatDef>.Defs),
                //new DefsPanel(DefRepository<NeedDef>.Defs),
                //new DefsPanel(DefRepository<ProfessionDef>.Defs),
                new DefsPanel(DefRepository<BodyPartSocketDef>.Defs),
                new DefsPanel(DefRepository<BodyPartDef>.Defs),
                new DefsPanel(DefRepository<BodyEffectDef>.Defs),
                new DefsPanel(DefRepository<BodyPartModifierDef>.Defs),
                new DefsPanel(DefRepository<ToolManeuverDef>.Defs),
            }
        });
        tabPanel.AddTab("Creatures", new DefsPanel(DefRepository<RaceDef>.Defs));
        tabPanel.AddTab("Items", new DefsPanel(DefRepository<ItemDef>.Defs, Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrame]));
    }
}