using Milgreth.Definitions;
using Milgreth.Sim.Entities;
using Milgreth.Sim.Entities.AI;
using Milgreth.Sim.Entities.Buildings;
using Milgreth.Sim.Entities.Jobs;
using Milgreth.Sim.Entities.Pawns;
using Milgreth.Sim.Gui.DefWidgets;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.Styles;

namespace Milgreth.Sim.Gui;

internal class BookOfAllKnowledgeWindow : Window {
    private readonly BookOfAllKnowledge _boak;

    public BookOfAllKnowledgeWindow(BookOfAllKnowledge boak) {
        _boak = boak;
        Width = 1600;
        Height = 900;
        Title = "Book of all Knowledge";
        TitleFont = BaseContent.Fonts.Default.Large;
        TabPanel tabPanel = new() {
            ButtonStyle = BaseContent.Styles.Button.Large
        };
        Content = tabPanel;
        tabPanel.AddTab("Biomes", null, new DefsPanel(DefRepository<BiomeDef>.Defs, Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.SmallFrame]));
        tabPanel.AddTab("Citizens", null, new VerticalStackPanel {
            Widgets = {
                new DefsPanel(DefRepository<SkillDef>.Defs),
                new DefsPanel(DefRepository<StatDef>.Defs),
                new DefsPanel(DefRepository<NeedDef>.Defs),
                new DefsPanel(DefRepository<ProfessionDef>.Defs),
                new DefsPanel(DefRepository<HealthConditionDef>.Defs),
                new DefsPanel(DefRepository<DecisionPackageDef>.Defs),
                new DefsPanel(DefRepository<DecisionScopeDef>.Defs),
                new DefsPanel(DefRepository<DecisionDef>.Defs),
                new DefsPanel(DefRepository<ConsiderationDef>.Defs),
                new DefsPanel(DefRepository<JobDef>.Defs),
            }
        });
        tabPanel.AddTab("Resources", null, new DefsPanel(DefRepository<NaturalResourceDef>.Defs));
        tabPanel.AddTab("Creatures", null, new DefsPanel(DefRepository<RaceDef>.Defs));
        tabPanel.AddTab("Buildings", null, new DefsPanel(DefRepository<BuildingDef>.Defs, Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrame]));
        tabPanel.AddTab("Items", null, new DefsPanel(DefRepository<ItemDef>.Defs, Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrame]));
    }
}