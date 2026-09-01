using Wendlemire.Scenes.MainGameScene.Gui.Widgets.DefWidgets;

namespace Wendlemire.Scenes.MainGameScene.Gui.Widgets.MiscWidgets.Boak;

internal sealed class BoakPawnPanel : Panel
{
    public BoakPawnPanel()
    {
        TabPanel tabPanel = new() { ButtonStyle = BaseContent.Styles.Button.Normal };
        tabPanel.AddTab("Creatures", new BoakPawnsCreaturesPanel(DefRepository<PawnDef>.Defs));
        tabPanel.AddTab("Bloods", new BoakPawnsBloodsPanel(DefRepository<BloodDef>.Defs));
        tabPanel.AddTab("Bodies", new BoakPawnsBodiesPanel(DefRepository<BodyDef>.Defs));
        tabPanel.AddTab("Body Effects", new BoakPawnsBodyEffectsPanel(DefRepository<BodyEffectDef>.Defs));
        tabPanel.AddTab("Body Parts", new BoakPawnsBodyPartsPanel(DefRepository<BodyPartDef>.Defs, DefRepository<BodyPartSocketDef>.Defs));
        tabPanel.AddTab("Part Modifiers", new BoakPawnsPartModifiersPanel(DefRepository<BodyPartModifierDef>.Defs));
        tabPanel.AddTab("Traits", new DefsPanel(DefRepository<TraitDef>.Defs));
        tabPanel.AddTab("Skills", new DefsPanel(DefRepository<SkillDef>.Defs));

        Widgets.Add(tabPanel);
    }
}